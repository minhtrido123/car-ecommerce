# Car Catalog Seed + Detail Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Seed ~50 cars into the PostgreSQL database and build per-car detail pages (frontend) backed by dedicated backend endpoints (navigations, detail + similar-cars, authenticated favorites/cart).

**Architecture:** Backend gains Car navigation properties (Brand/Model/Category/Seller) with FK config + a migration, a new `CarService` in Services (detail/similar/favorites/cart), and replaces the generic `/api/cars` registration with dedicated endpoints. A new `Seed` console project inserts an idempotent catalog. Frontend adds `/cars/[id]` client page and wires the existing "View Details" button.

**Tech Stack:** .NET 10 / C# 13, ASP.NET Core minimal APIs, EF Core 10 + Npgsql + EF InMemory (tests), xUnit. Frontend: Next.js 16 app router, React 19, axios, Metro UI CSS, zustand.

## Global Constraints

- Target framework `net10.0` everywhere; `Nullable` + `ImplicitUsings` enabled.
- **Git repos:** `car-backend/` is a git repo (commit there). `car-app/` is NOT a git repo — frontend tasks have **no commit step**.
- Build/test per project from `car-backend/`:
  - `dotnet build API`, `dotnet test Tests`, `dotnet build Seed`
- Migration command (DesignTime factory exists):
  - `dotnet ef migrations add AddCarRelationships --project Infrastructure --startup-project API`
- No code comments in new C# files (project convention).
- API JSON is camelCase (ASP.NET Core default). Car entities serialize `CarImages`; `CarImage.Car` has `[JsonIgnore]` — never leak `User.PasswordHash` (detail endpoint returns DTOs, never entities).
- Existing generic endpoints for `Favorite`/`CartItem` (Admin commands) stay untouched.
- Frontend pages using hooks must be `"use client"`. Follow Metro UI conventions (existing `/cars`, `/login` pages).
- Seed data image URLs point at Next-served public assets: `/DesktopBackground/NN_....jpg`.

---

### Task 1: Car Navigation Properties + FK Configuration + Migration

**Files:**
- Modify: `car-backend/Models/Car.cs`
- Modify: `car-backend/Infrastructure/SorchaDbContext.cs`
- Create (generated): `car-backend/Infrastructure/Migrations/*AddCarRelationships*`

**Interfaces:**
- Consumes: `Brand`, `CarModel`, `Category`, `User` (Models project).
- Produces: `Car.Brand`/`Car.Model`/`Car.Category`/`Car.Seller` navigations; FK constraints `Cars.BrandId → Brands`, `Cars.ModelId → CarModels`, `Cars.CategoryId → Categories`, `Cars.SellerId → Users`; new migration `AddCarRelationships`.

- [ ] **Step 1: Add navigation properties**

In `car-backend/Models/Car.cs`, add to the `Car` class (after `CarImages`):

```csharp
public Brand? Brand { get; set; }
public CarModel? Model { get; set; }
public Category? Category { get; set; }
public User? Seller { get; set; }
```

- [ ] **Step 2: Configure relationships**

In `car-backend/Infrastructure/SorchaDbContext.cs`, the `Car` config appears in **two** blocks (lines ~73 and ~97). Merge them into one block after the `OrderItem` config, containing everything below (the two `entity.HasMany(c => c.CarImages)...` / `entity.Property(c => c.Price)...` lines already exist — keep them, add the rest):

```csharp
modelBuilder.Entity<Car>(entity =>
{
    entity.HasIndex(c => c.Status).HasDatabaseName("IX_cars_status");
    entity.Property(c => c.Price).HasColumnType("decimal(12,2)");
    entity.HasMany(c => c.CarImages)
        .WithOne(ci => ci.Car)
        .HasForeignKey(ci => ci.CarId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(c => c.Brand).WithMany().HasForeignKey(c => c.BrandId);
    entity.HasOne(c => c.Model).WithMany().HasForeignKey(c => c.ModelId);
    entity.HasOne(c => c.Category).WithMany().HasForeignKey(c => c.CategoryId);
    entity.HasOne(c => c.Seller).WithMany().HasForeignKey(c => c.SellerId);
});
```

Remove the two duplicate `modelBuilder.Entity<Car>(...)` blocks from `OnModelCreating`.

- [ ] **Step 3: Build**

Run (in `car-backend/`): `dotnet build API`
Expected: Build succeeded.

- [ ] **Step 4: Generate the migration**

Run: `dotnet ef migrations add AddCarRelationships --project Infrastructure --startup-project API`
Expected: New `*_AddCarRelationships.cs` under `car-backend/Infrastructure/Migrations/`. Open it — must add ForeignKeys `FK_Cars_Brands_BrandId`, `FK_Cars_CarModels_ModelId`, `FK_Cars_Categories_CategoryId`, `FK_Cars_Users_SellerId` (plus `IX_Cars_*` indexes). Existing NULL-FK car rows do not violate nullable constraints.

- [ ] **Step 5: Apply the migration**

Run: `dotnet ef database update --project Infrastructure --startup-project API`
Expected: Applies cleanly against `localhost:5432`. Verify: `psql` — `\d "Cars"` shows the 4 FK constraints.

- [ ] **Step 6: Run tests**

Run: `dotnet test Tests`
Expected: All existing tests pass.

- [ ] **Step 7: Commit**

```bash
git add Models/Car.cs Infrastructure/SorchaDbContext.cs Infrastructure/Migrations/
git commit -m "feat: add car brand/model/category/seller relationships"
```

---

### Task 2: CarService + Unit Tests

**Files:**
- Create: `car-backend/Services/CarService.cs` (contains `ICarService`, `CarService`)
- Create: `car-backend/Tests/CarServiceTests.cs`

**Interfaces:**
- Consumes: `SorchaDbContext` (Infrastructure), `Models` entities. `SorchaDbContext` is available in Services via transitive P2P references (Repositories → Infrastructure).
- Produces:
  - `Services.ICarService`:
    - `Task<Car?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)` — Car with Brand/Model/Category/Seller/CarImages loaded, or null.
    - `Task<List<Car>> GetSimilarAsync(Guid modelId, Guid? excludeId, int limit, CancellationToken cancellationToken = default)` — same ModelId, excludes `excludeId`, ordered Year desc, top `limit`. Navigations: Brand, Model, CarImages.
    - `Task<bool> AddFavoriteAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default)` — false if already favorited.
    - `Task RemoveFavoriteAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default)` — idempotent.
    - `Task<bool> AddCartItemAsync(Guid userId, Guid carId, int quantity, CancellationToken cancellationToken = default)` — false if already in cart.
    - `Task RemoveCartItemAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default)` — idempotent.

- [ ] **Step 1: Write the failing tests**

Create `car-backend/Tests/CarServiceTests.cs`:

```csharp
using Infrastructure;
using Models;
using Services;

namespace Tests;

public class CarServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly CarService _carService;

    public CarServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _carService = new CarService(_db);
    }

    private async Task<(Guid brandId, Guid modelId, Guid categoryId, Guid sellerId)> SeedCatalogAsync()
    {
        var brand = new Brand { Name = "Toyota", Country = "Japan" };
        await _db.Brands.AddAsync(brand);
        await _db.SaveChangesAsync();
        var model = new CarModel { BrandId = brand.Id, Name = "Camry" };
        await _db.CarModels.AddAsync(model);
        await _db.SaveChangesAsync();
        var category = new Category { Name = "Sedan", Slug = "sedan" };
        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();
        var seller = new User { Email = "seller@test.com", PasswordHash = "hash", Role = "Seller", Name = "Dealer" };
        await _db.Users.AddAsync(seller);
        await _db.SaveChangesAsync();
        return (brand.Id, model.Id, category.Id, seller.Id);
    }

    private Car MakeCar(Guid brandId, Guid modelId, Guid categoryId, Guid sellerId, int year, decimal price, string status = "Active") =>
        new()
        {
            BrandId = brandId, ModelId = modelId, CategoryId = categoryId, SellerId = sellerId,
            Year = year, Price = price, Mileage = 1000, Color = "Red", Status = status
        };

    [Fact]
    public async Task GetDetailAsync_ReturnsCar_WithNavigationsLoaded()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        await _db.CarImages.AddAsync(new CarImage { CarId = car.Id, Url = "/img.jpg", IsPrimary = true });
        await _db.SaveChangesAsync();

        var result = await _carService.GetDetailAsync(car.Id);

        Assert.NotNull(result);
        Assert.Equal("Toyota", result!.Brand!.Name);
        Assert.Equal("Camry", result.Model!.Name);
        Assert.Equal("Sedan", result.Category!.Name);
        Assert.Equal("Dealer", result.Seller!.Name);
        Assert.Single(result.CarImages);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNull_WhenMissing()
    {
        var result = await _carService.GetDetailAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSimilarAsync_ReturnsSameModelExcludingSelf_OrderedByYearDesc()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var c1 = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 20000);
        var c2 = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 25000);
        var c3 = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 22000);
        await _db.Cars.AddRangeAsync(c1, c2, c3);
        await _db.SaveChangesAsync();

        var result = await _carService.GetSimilarAsync(modelId, c1.Id, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(c2.Id, result[0].Id);
        Assert.Equal(c3.Id, result[1].Id);
    }

    [Fact]
    public async Task GetSimilarAsync_ExcludesNonActiveCars()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var active = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 25000);
        var sold = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000, "Sold");
        await _db.Cars.AddRangeAsync(active, sold);
        await _db.SaveChangesAsync();

        var result = await _carService.GetSimilarAsync(modelId, null, 10);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    [Fact]
    public async Task AddFavoriteAsync_Adds_WhenNew_AndFalse_WhenDuplicate()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        var userId = Guid.NewGuid();

        var added = await _carService.AddFavoriteAsync(userId, car.Id);
        var again = await _carService.AddFavoriteAsync(userId, car.Id);

        Assert.True(added);
        Assert.False(again);
        Assert.Single(await _db.Favorites.ToListAsync());
    }

    [Fact]
    public async Task RemoveFavoriteAsync_Removes_WhenExists()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        var userId = Guid.NewGuid();
        await _carService.AddFavoriteAsync(userId, car.Id);

        await _carService.RemoveFavoriteAsync(userId, car.Id);

        Assert.Empty(await _db.Favorites.ToListAsync());
    }

    [Fact]
    public async Task AddCartItemAsync_Adds_WhenNew_AndFalse_WhenDuplicate()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        var userId = Guid.NewGuid();

        var added = await _carService.AddCartItemAsync(userId, car.Id, 1);
        var again = await _carService.AddCartItemAsync(userId, car.Id, 1);

        Assert.True(added);
        Assert.False(again);
        Assert.Single(await _db.CartItems.ToListAsync());
    }

    [Fact]
    public async Task RemoveCartItemAsync_Removes_WhenExists()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        var userId = Guid.NewGuid();
        await _carService.AddCartItemAsync(userId, car.Id, 1);

        await _carService.RemoveCartItemAsync(userId, car.Id);

        Assert.Empty(await _db.CartItems.ToListAsync());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run (in `car-backend/`): `dotnet test Tests`
Expected: Compilation fails — `CarService`, `ICarService` don't exist.

- [ ] **Step 3: Implement CarService**

Create `car-backend/Services/CarService.cs`:

```csharp
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services;

public interface ICarService
{
    Task<Car?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Car>> GetSimilarAsync(Guid modelId, Guid? excludeId, int limit, CancellationToken cancellationToken = default);
    Task<bool> AddFavoriteAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default);
    Task RemoveFavoriteAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default);
    Task<bool> AddCartItemAsync(Guid userId, Guid carId, int quantity, CancellationToken cancellationToken = default);
    Task RemoveCartItemAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default);
}

public class CarService : ICarService
{
    private readonly SorchaDbContext _db;

    public CarService(SorchaDbContext db)
    {
        _db = db;
    }

    public async Task<Car?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Cars
            .Include(c => c.Brand)
            .Include(c => c.Model)
            .Include(c => c.Category)
            .Include(c => c.Seller)
            .Include(c => c.CarImages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Car>> GetSimilarAsync(Guid modelId, Guid? excludeId, int limit, CancellationToken cancellationToken = default)
    {
        return await _db.Cars
            .Where(c => c.ModelId == modelId && c.Status == "Active")
            .Where(c => excludeId == null || c.Id != excludeId)
            .Include(c => c.Brand)
            .Include(c => c.Model)
            .Include(c => c.CarImages)
            .OrderByDescending(c => c.Year)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AddFavoriteAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default)
    {
        if (await _db.Favorites.AnyAsync(f => f.UserId == userId && f.CarId == carId, cancellationToken))
            return false;

        _db.Favorites.Add(new Favorite { UserId = userId, CarId = carId });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default)
    {
        var favorite = await _db.Favorites.FirstOrDefaultAsync(
            f => f.UserId == userId && f.CarId == carId, cancellationToken);
        if (favorite is not null)
        {
            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> AddCartItemAsync(Guid userId, Guid carId, int quantity, CancellationToken cancellationToken = default)
    {
        if (await _db.CartItems.AnyAsync(c => c.UserId == userId && c.CarId == carId, cancellationToken))
            return false;

        _db.CartItems.Add(new CartItem { UserId = userId, CarId = carId, Quantity = quantity });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RemoveCartItemAsync(Guid userId, Guid carId, CancellationToken cancellationToken = default)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(
            c => c.UserId == userId && c.CarId == carId, cancellationToken);
        if (item is not null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests`
Expected: All tests pass (existing + new).

- [ ] **Step 5: Commit**

```bash
git add Services/CarService.cs Tests/CarServiceTests.cs
git commit -m "feat: add car service for detail, similar, favorites, and cart"
```

---

### Task 3: DTOs, Mapping, Endpoints, DI

**Files:**
- Modify: `car-backend/API/DTOs/CarDto.cs`
- Create: `car-backend/API/DTOs/ActionDtos.cs`
- Create: `car-backend/API/CarMapping.cs`
- Modify: `car-backend/API/GlobalUsings.cs`
- Modify: `car-backend/API/Program.cs`

**Interfaces:**
- Consumes: `Services.ICarService` (Task 2), `Models` entities, existing `API.DTOs.CarImageResponse` (from `CarImageDto.cs`).
- Produces: `API.DTOs.CarDetailResponse`, `SellerResponse`, `CarCardResponse`, `AddFavoriteRequest`, `AddCartItemRequest`; static `API.CarMapping.ToDetail(Car)`, `API.CarMapping.ToCard(Car)`; endpoints `GET /api/cars`, `GET /api/cars/{id}`, `GET /api/cars/by-model/{modelId}`, Admin `POST/PUT/DELETE /api/cars`, auth `POST/DELETE /api/me/favorites`, auth `POST/DELETE /api/me/cart-items`.
- **Deviation from spec (approved):** customer favorites/cart live under `/api/me/...` instead of `/api/favorites` / `/api/cart-items` because those templates are already registered by the generic Admin-role `MapEntityEndpoints<Favorite>` / `<CartItem>` and a second identical route would fail at startup.

- [ ] **Step 1: Add car DTOs**

Append to `car-backend/API/DTOs/CarDto.cs`:

```csharp
public record CarDetailResponse(
    Guid Id, Guid? ModelId, int Year, decimal Price, int? Mileage, string? Color,
    string? Description, string Status, DateTime CreatedAt,
    string? BrandName, string? ModelName, string? CategoryName,
    SellerResponse? Seller, List<CarImageResponse> Images);

public record SellerResponse(Guid Id, string Name, string Email);

public record CarCardResponse(
    Guid Id, string? BrandName, string? ModelName, int Year, decimal Price,
    int? Mileage, string? Color, string? PrimaryImageUrl);
```

- [ ] **Step 2: Add action DTOs**

Create `car-backend/API/DTOs/ActionDtos.cs`:

```csharp
namespace API.DTOs;

public record AddFavoriteRequest(Guid CarId);

public record AddCartItemRequest(Guid CarId, int Quantity);
```

- [ ] **Step 3: Create the mapping helper**

Create `car-backend/API/CarMapping.cs`:

```csharp
using API.DTOs;
using Models;

namespace API;

public static class CarMapping
{
    public static CarDetailResponse ToDetail(Car car) => new(
        car.Id, car.ModelId, car.Year, car.Price, car.Mileage, car.Color, car.Description,
        car.Status, car.CreatedAt,
        car.Brand?.Name, car.Model?.Name, car.Category?.Name,
        car.Seller is null ? null : new SellerResponse(car.Seller.Id, car.Seller.Name, car.Seller.Email),
        car.CarImages
            .OrderByDescending(i => i.IsPrimary)
            .Select(i => new CarImageResponse(i.Id, i.CarId, i.Url, i.IsPrimary, i.CreatedAt))
            .ToList());

    public static CarCardResponse ToCard(Car car) => new(
        car.Id, car.Brand?.Name, car.Model?.Name, car.Year, car.Price, car.Mileage, car.Color,
        car.CarImages.FirstOrDefault(i => i.IsPrimary)?.Url ?? car.CarImages.FirstOrDefault()?.Url);
}
```

- [ ] **Step 4: Extend GlobalUsings**

In `car-backend/API/GlobalUsings.cs`, append:

```csharp
global using System.Security.Claims;
global using System.IdentityModel.Tokens.Jwt;
```

- [ ] **Step 5: Update Program.cs — DI + endpoints**

In `car-backend/API/Program.cs`:

(a) After `builder.Services.AddScoped<IAuthService, AuthService>();` add:

```csharp
builder.Services.AddScoped<ICarService, CarService>();
```

(b) Replace the line `MapEntityEndpoints<Car>(app, "cars");` with:

```csharp
MapCarEndpoints(app);
```

(c) After `MapAuthEndpoints(app);` add:

```csharp
MapCustomerActionEndpoints(app);
```

(d) After the `MapAuthEndpoints` static method (end of file), add:

```csharp
static void MapCarEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/cars");
    var admin = app.MapGroup("/api/cars")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    group.MapGet("/", async ([FromServices] IService<Car> service,
        CancellationToken cancel, int pageNumber = 1, int pageSize = 20, string? includes = null) =>
    {
        string[] includeArray = includes != null && includes.Any() ? includes.Split(",") : [];
        var result = await service.GetPagedAsync(pageNumber, pageSize, includeArray, cancel);
        return Results.Ok(result);
    }).WithName("GetAll_Car");

    group.MapGet("/{id:guid}", async ([FromServices] ICarService carService, Guid id, CancellationToken cancel) =>
    {
        var car = await carService.GetDetailAsync(id, cancel);
        return car is null ? Results.NotFound() : Results.Ok(CarMapping.ToDetail(car));
    }).WithName("GetById_Car");

    group.MapGet("/by-model/{modelId:guid}", async ([FromServices] ICarService carService,
        Guid modelId, CancellationToken cancel, int limit = 4, Guid? exclude = null) =>
    {
        var cars = await carService.GetSimilarAsync(modelId, exclude, limit, cancel);
        return Results.Ok(cars.Select(CarMapping.ToCard).ToList());
    }).WithName("GetCarsByModel");

    admin.MapPost("/", async ([FromServices] IService<Car> service,
        CreateRequest<Car> request, CancellationToken cancel) =>
    {
        var entity = await service.CreateAsync(request, cancel);
        return Results.CreatedAtRoute("GetById_Car", new { id = entity.Id }, CarMapping.ToDetail(entity));
    }).WithName("Create_Car");

    admin.MapPut("/{id:guid}", async ([FromServices] IService<Car> service,
        Guid id, UpdateRequest<Car> request, CancellationToken cancel) =>
    {
        if (id != request.Id) return Results.BadRequest();
        var entity = await service.UpdateAsync(request, cancel);
        return Results.Ok(entity);
    }).WithName("Update_Car");

    admin.MapDelete("/{id:guid}", async ([FromServices] IService<Car> service,
        Guid id, CancellationToken cancel) =>
    {
        await service.DeleteAsync(id, cancel);
        return Results.NoContent();
    }).WithName("Delete_Car");
}

static void MapCustomerActionEndpoints(WebApplication app)
{
    var favorites = app.MapGroup("/api/me/favorites").RequireAuthorization();
    var cart = app.MapGroup("/api/me/cart-items").RequireAuthorization();

    favorites.MapPost("/", async ([FromServices] ICarService carService,
        ClaimsPrincipal user, AddFavoriteRequest request, CancellationToken cancel) =>
    {
        var added = await carService.AddFavoriteAsync(GetUserId(user), request.CarId, cancel);
        return added ? Results.NoContent() : Results.Conflict(new { error = "Already favorited" });
    });

    favorites.MapDelete("/{carId:guid}", async ([FromServices] ICarService carService,
        ClaimsPrincipal user, Guid carId, CancellationToken cancel) =>
    {
        await carService.RemoveFavoriteAsync(GetUserId(user), carId, cancel);
        return Results.NoContent();
    });

    cart.MapPost("/", async ([FromServices] ICarService carService,
        ClaimsPrincipal user, AddCartItemRequest request, CancellationToken cancel) =>
    {
        var added = await carService.AddCartItemAsync(GetUserId(user), request.CarId, request.Quantity, cancel);
        return added ? Results.NoContent() : Results.Conflict(new { error = "Already in cart" });
    });

    cart.MapDelete("/{carId:guid}", async ([FromServices] ICarService carService,
        ClaimsPrincipal user, Guid carId, CancellationToken cancel) =>
    {
        await carService.RemoveCartItemAsync(GetUserId(user), carId, cancel);
        return Results.NoContent();
    });
}

static Guid GetUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException();
}
```

Note: the existing `MapEntityEndpoints<Favorite>(app, "favorites")` and `MapEntityEndpoints<CartItem>(app, "cart-items")` lines **remain** — they provide Admin management + open GETs.

- [ ] **Step 6: Build**

Run: `dotnet build API`
Expected: Build succeeded.

- [ ] **Step 7: Run tests**

Run: `dotnet test Tests`
Expected: All tests pass.

- [ ] **Step 8: Smoke test**

Start API (Postgres + Redis reachable; run from `car-backend/`):
`dotnet run --project API`

In another shell:

```bash
curl -s http://localhost:5251/api/cars | jq '.totalCount'
CAR_ID=$(curl -s http://localhost:5251/api/cars | jq -r '.items[0].id')
curl -s http://localhost:5251/api/cars/$CAR_ID | jq '{id, brandName, modelName, categoryName, seller, images}'
```

Expected: detail response includes brand/model/category names, seller, images. `GET /api/cars/by-model/<modelId>` returns up to 4 cards.

- [ ] **Step 9: Commit**

```bash
git add API/DTOs/CarDto.cs API/DTOs/ActionDtos.cs API/CarMapping.cs API/GlobalUsings.cs API/Program.cs
git commit -m "feat: add car detail, similar, favorites, and cart endpoints"
```

---

### Task 4: Seed Console Project + Run

**Files:**
- Create: `car-backend/Seed/Seed.csproj`
- Create: `car-backend/Seed/Program.cs`
- Create: `car-backend/Seed/SeedData.cs`
- Modify: `car-backend/CarEcommerce.slnx` (add Seed project)

**Interfaces:**
- Consumes: `SorchaDbContext`, `Models` (transitive via Infrastructure).
- Produces: `Seed.SeedData.Run(SorchaDbContext db)` returning `Seed.SeedResult` (counts of inserted brands/models/categories/cars/images). Console app `dotnet run --project Seed` with optional connection-string arg.

- [ ] **Step 1: Create the project file**

Create `car-backend/Seed/Seed.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
    <ProjectReference Include="..\Models\Models.csproj" />
    <ProjectReference Include="..\Sorcha.ServiceDefaults\Sorcha.ServiceDefaults.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create Program.cs**

Create `car-backend/Seed/Program.cs`:

```csharp
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Seed;

public static class Program
{
    public static void Main(string[] args)
    {
        var connection = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("CONNECTION_STRING")
              ?? "Host=localhost;Port=5432;Database=car ecommerce;Username=admin;Password=123456";

        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseNpgsql(connection)
            .Options;

        using var db = new SorchaDbContext(options);
        db.Database.Migrate();

        var result = SeedData.Run(db);
        Console.WriteLine($"Seeded: {result.Brands} brands, {result.Models} models, {result.Categories} categories, {result.Cars} cars, {result.Images} images");
    }
}
```

- [ ] **Step 3: Create the seed data**

Create `car-backend/Seed/SeedData.cs`:

```csharp
using Infrastructure;
using Models;

namespace Seed;

public sealed record SeedResult(int Brands, int Models, int Categories, int Cars, int Images);

public static class SeedData
{
    public static SeedResult Run(SorchaDbContext db)
    {
        var brandCount = 0;
        var modelCount = 0;
        var categoryCount = 0;
        var carCount = 0;
        var imageCount = 0;

        var brandRows = new (string Name, string Country)[]
        {
            ("Toyota", "Japan"), ("Honda", "Japan"), ("Ford", "USA"),
            ("Chevrolet", "USA"), ("BMW", "Germany"), ("Mercedes-Benz", "Germany"),
            ("Audi", "Germany"), ("Hyundai", "South Korea"), ("Kia", "South Korea"),
            ("Tesla", "USA")
        };

        var brandIds = new Dictionary<string, Guid>();
        foreach (var (name, country) in brandRows)
        {
            var existing = db.Brands.FirstOrDefault(b => b.Name == name);
            if (existing is null)
            {
                existing = new Brand { Name = name, Country = country };
                db.Brands.Add(existing);
                db.SaveChanges();
                brandCount++;
            }
            brandIds[name] = existing.Id;
        }

        var modelRows = new (string Brand, string Name, int? YearStart)[]
        {
            ("Toyota", "Camry", 1982), ("Toyota", "Corolla", 1966), ("Toyota", "RAV4", 1994), ("Toyota", "Highlander", 2000),
            ("Honda", "Civic", 1972), ("Honda", "Accord", 1976), ("Honda", "CR-V", 1995),
            ("Ford", "F-150", 1975), ("Ford", "Mustang", 1964), ("Ford", "Escape", 2000),
            ("Chevrolet", "Silverado", 1999), ("Chevrolet", "Tahoe", 1995), ("Chevrolet", "Camaro", 1966),
            ("BMW", "3 Series", 1975), ("BMW", "5 Series", 1972), ("BMW", "X5", 1999),
            ("Mercedes-Benz", "C-Class", 1993), ("Mercedes-Benz", "E-Class", 1993), ("Mercedes-Benz", "GLE", 2015),
            ("Audi", "A4", 1994), ("Audi", "A6", 1994), ("Audi", "Q5", 2008),
            ("Hyundai", "Elantra", 1990), ("Hyundai", "Sonata", 1985), ("Hyundai", "Tucson", 2004),
            ("Kia", "Sportage", 1993), ("Kia", "Telluride", 2019), ("Kia", "Optima", 2000),
            ("Tesla", "Model 3", 2017), ("Tesla", "Model Y", 2020), ("Tesla", "Model S", 2012)
        };

        var modelIds = new Dictionary<string, Guid>();
        foreach (var (brand, name, yearStart) in modelRows)
        {
            var key = $"{brand}|{name}";
            var existing = db.CarModels.FirstOrDefault(m => m.Name == name && m.BrandId == brandIds[brand]);
            if (existing is null)
            {
                existing = new CarModel { BrandId = brandIds[brand], Name = name, YearStart = yearStart };
                db.CarModels.Add(existing);
                db.SaveChanges();
                modelCount++;
            }
            modelIds[key] = existing.Id;
        }

        var categoryRows = new (string Name, string Slug)[]
        {
            ("Sedan", "sedan"), ("SUV", "suv"), ("Truck", "truck"),
            ("Coupe", "coupe"), ("Hatchback", "hatchback"), ("Electric", "electric")
        };

        var categoryIds = new Dictionary<string, Guid>();
        foreach (var (name, slug) in categoryRows)
        {
            var existing = db.Categories.FirstOrDefault(c => c.Name == name);
            if (existing is null)
            {
                existing = new Category { Name = name, Slug = slug };
                db.Categories.Add(existing);
                db.SaveChanges();
                categoryCount++;
            }
            categoryIds[name] = existing.Id;
        }

        var seller = db.Users.OrderBy(u => u.CreatedAt).FirstOrDefault();

        var images = new[]
        {
            "/DesktopBackground/1_classicsportscars_porsche911.jpg",
            "/DesktopBackground/2_classicsportscars_ferrarienzo.jpg",
            "/DesktopBackground/3_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/4_classicsportscars_jaguare-type.jpg",
            "/DesktopBackground/5_classicsportscars_mercedes-benz300sl.jpg",
            "/DesktopBackground/6_classicsportscars_lamborghinimiura.jpg",
            "/DesktopBackground/7_classicsportscars_astonmartinvolante.jpg",
            "/DesktopBackground/8_classicsportscars_mazdamiata.jpg",
            "/DesktopBackground/9_classicsportscars_mclarenf1.jpg",
            "/DesktopBackground/10_classicsportscars_ferrari330.jpg",
            "/DesktopBackground/11_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/12_classicsportscars_porsche911.jpg",
            "/DesktopBackground/13_classicsportscars_mercedes-benz300sl.jpg",
            "/DesktopBackground/14_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/15_classicsportscars_astonmartindb2-4.jpg",
            "/DesktopBackground/16_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/17_classicsportscars_lamborghinis.jpg",
            "/DesktopBackground/18_classicsportscars_ferraritestarossa.jpg",
            "/DesktopBackground/19_classicsportscars_astonmartindb4.jpg"
        };

        var carRows = new (string Brand, string Model, string Category, int Year, decimal Price, int Mileage, string Color, int ImageIndex)[]
        {
            ("Toyota", "Camry", "Sedan", 2019, 22500, 45000, "Silver", 0),
            ("Toyota", "Camry", "Sedan", 2021, 26900, 22000, "White", 1),
            ("Toyota", "Camry", "Sedan", 2016, 16800, 78000, "Black", 2),
            ("Toyota", "Corolla", "Sedan", 2020, 18900, 38000, "Blue", 3),
            ("Toyota", "Corolla", "Sedan", 2018, 14500, 60000, "Red", 4),
            ("Toyota", "Corolla", "Sedan", 2022, 21400, 15000, "Gray", 5),
            ("Toyota", "RAV4", "SUV", 2021, 31200, 28000, "Green", 6),
            ("Toyota", "RAV4", "SUV", 2019, 27500, 40000, "Silver", 7),
            ("Toyota", "RAV4", "SUV", 2023, 35800, 9000, "White", 8),
            ("Toyota", "Highlander", "SUV", 2020, 38900, 33000, "Black", 9),
            ("Honda", "Civic", "Hatchback", 2020, 19800, 35000, "White", 10),
            ("Honda", "Civic", "Hatchback", 2022, 24900, 12000, "Red", 11),
            ("Honda", "Civic", "Hatchback", 2018, 15200, 70000, "Black", 12),
            ("Honda", "Accord", "Sedan", 2021, 27400, 20000, "Blue", 13),
            ("Honda", "Accord", "Sedan", 2019, 23100, 45000, "Silver", 14),
            ("Honda", "CR-V", "SUV", 2020, 29500, 32000, "Gray", 15),
            ("Honda", "CR-V", "SUV", 2022, 33200, 18000, "White", 16),
            ("Ford", "F-150", "Truck", 2021, 41500, 25000, "Red", 17),
            ("Ford", "F-150", "Truck", 2019, 33800, 52000, "Black", 18),
            ("Ford", "Mustang", "Coupe", 2020, 36900, 18000, "Yellow", 0),
            ("Ford", "Mustang", "Coupe", 2022, 44500, 6000, "Blue", 1),
            ("Ford", "Escape", "SUV", 2021, 26700, 21000, "White", 2),
            ("Chevrolet", "Silverado", "Truck", 2022, 48200, 14000, "Black", 3),
            ("Chevrolet", "Silverado", "Truck", 2020, 39900, 38000, "Silver", 4),
            ("Chevrolet", "Tahoe", "SUV", 2021, 58300, 16000, "White", 5),
            ("Chevrolet", "Camaro", "Coupe", 2020, 34200, 22000, "Red", 6),
            ("BMW", "3 Series", "Sedan", 2021, 41800, 19000, "Blue", 7),
            ("BMW", "3 Series", "Sedan", 2019, 32500, 44000, "White", 8),
            ("BMW", "5 Series", "Sedan", 2020, 48900, 30000, "Black", 9),
            ("BMW", "X5", "SUV", 2021, 62400, 17000, "Gray", 10),
            ("BMW", "X5", "SUV", 2019, 48700, 41000, "White", 11),
            ("Mercedes-Benz", "C-Class", "Sedan", 2020, 38400, 26000, "Silver", 12),
            ("Mercedes-Benz", "C-Class", "Sedan", 2022, 46900, 8000, "Black", 13),
            ("Mercedes-Benz", "E-Class", "Sedan", 2021, 58200, 15000, "White", 14),
            ("Mercedes-Benz", "GLE", "SUV", 2020, 63800, 23000, "Blue", 15),
            ("Audi", "A4", "Sedan", 2021, 39700, 18000, "Gray", 16),
            ("Audi", "A4", "Sedan", 2019, 29800, 46000, "White", 17),
            ("Audi", "A6", "Sedan", 2020, 47500, 27000, "Black", 18),
            ("Audi", "Q5", "SUV", 2021, 43900, 20000, "Blue", 0),
            ("Hyundai", "Elantra", "Sedan", 2021, 19600, 24000, "White", 1),
            ("Hyundai", "Elantra", "Sedan", 2019, 14900, 58000, "Gray", 2),
            ("Hyundai", "Sonata", "Sedan", 2020, 22800, 31000, "Silver", 3),
            ("Hyundai", "Tucson", "SUV", 2022, 29400, 12000, "Red", 4),
            ("Kia", "Sportage", "SUV", 2021, 24900, 26000, "White", 5),
            ("Kia", "Telluride", "SUV", 2022, 44600, 15000, "Black", 6),
            ("Kia", "Optima", "Sedan", 2020, 20300, 33000, "Blue", 7),
            ("Tesla", "Model 3", "Electric", 2021, 37900, 29000, "White", 8),
            ("Tesla", "Model 3", "Electric", 2023, 44700, 5000, "Black", 9),
            ("Tesla", "Model Y", "Electric", 2022, 52900, 11000, "Blue", 10),
            ("Tesla", "Model S", "Electric", 2021, 72400, 19000, "Red", 11)
        };

        if (seller is not null)
        {
            foreach (var (brand, model, category, year, price, mileage, color, imageIndex) in carRows)
            {
                var brandId = brandIds[brand];
                var modelId = modelIds[$"{brand}|{model}"];
                var categoryId = categoryIds[category];
                var exists = db.Cars.Any(c =>
                    c.BrandId == brandId && c.ModelId == modelId &&
                    c.Year == year && c.Price == price && c.Color == color);
                if (exists) continue;

                var car = new Car
                {
                    BrandId = brandId, ModelId = modelId, CategoryId = categoryId,
                    SellerId = seller.Id, Year = year, Price = price, Mileage = mileage,
                    Color = color, Status = "Active"
                };
                db.Cars.Add(car);
                db.SaveChanges();
                carCount++;

                db.CarImages.Add(new CarImage { CarId = car.Id, Url = images[imageIndex], IsPrimary = true });
                db.CarImages.Add(new CarImage { CarId = car.Id, Url = images[(imageIndex + 7) % images.Length], IsPrimary = false });
                db.SaveChanges();
                imageCount += 2;
            }
        }

        return new SeedResult(brandCount, modelCount, categoryCount, carCount, imageCount);
    }
}
```

- [ ] **Step 4: Add Seed to the solution**

Modify `car-backend/CarEcommerce.slnx` — add after the Services entry:

```xml
  <Project Path="Seed/Seed.csproj" />
```

- [ ] **Step 5: Build**

Run (in `car-backend/`): `dotnet build Seed`
Expected: Build succeeded.

- [ ] **Step 6: Run the seed (twice to prove idempotency)**

Run: `dotnet run --project Seed`
Expected (first run): `Seeded: 10 brands, 31 models, 6 categories, 50 cars, 100 images`

Run again: `dotnet run --project Seed`
Expected (second run): `Seeded: 0 brands, 0 models, 0 categories, 0 cars, 0 images`

Verify in DB:

```bash
PGPASSWORD=123456 psql -h localhost -U admin -d "car ecommerce" -c "SELECT count(*) FROM \"Cars\"; SELECT count(*) FROM \"Brands\"; SELECT count(*) FROM \"CarModels\"; SELECT count(*) FROM \"Categories\"; SELECT count(*) FROM \"CarImages\";"
```

Expected: cars=52 (50 seeded + 2 pre-existing), brands=10, models=31, categories=6, images=102 (100 + 2 pre-existing).

- [ ] **Step 7: Commit**

```bash
git add Seed/ CarEcommerce.slnx
git commit -m "feat: add seed console project for car catalog"
```

---

### Task 5: Frontend Car Detail Page

**Files:**
- Create: `car-app/app/cars/[id]/page.tsx`

**Interfaces:**
- Consumes: `GET /cars/{id}` → `CarDetailResponse` (camelCase: id, year, price, mileage, color, description, status, createdAt, brandName, modelName, categoryName, seller{id,name,email}, images[{id,carId,url,isPrimary}]); `GET /cars/by-model/{modelId}?limit=4&exclude={id}` → `CarCardResponse[]` (camelCase); `POST /me/favorites {carId}`; `POST /me/cart-items {carId, quantity}`. Frontend `api` client (`app/lib/api.ts`), `useAuthStore` (`app/stores/auth-store.ts`).

- [ ] **Step 1: Write the page**

Create `car-app/app/cars/[id]/page.tsx`:

```tsx
"use client";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import api from "../../lib/api";
import { useAuthStore } from "../../stores/auth-store";

type CarImage = { id: string; carId: string; url: string; isPrimary: boolean };
type Seller = { id: string; name: string; email: string };
type CarDetail = {
  id: string;
  modelId: string | null;
  year: number;
  price: number;
  mileage: number | null;
  color: string | null;
  description: string | null;
  status: string;
  createdAt: string;
  brandName: string | null;
  modelName: string | null;
  categoryName: string | null;
  seller: Seller | null;
  images: CarImage[];
};

type SimilarCar = {
  id: string;
  brandName: string | null;
  modelName: string | null;
  year: number;
  price: number;
  mileage: number | null;
  color: string | null;
  primaryImageUrl: string | null;
};

export default function CarDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const accessToken = useAuthStore((state) => state.accessToken);

  const [car, setCar] = useState<CarDetail | null>(null);
  const [similar, setSimilar] = useState<SimilarCar[]>([]);
  const [selectedImage, setSelectedImage] = useState(0);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    async function load() {
      try {
        const { data } = await api.get(`/cars/${id}`, { showLoading: false });
        setCar(data);
        setSelectedImage(0);
        if (data.modelId) {
          const sim = await api.get(`/cars/by-model/${data.modelId}?limit=4&exclude=${id}`, { showLoading: false });
          setSimilar(sim.data);
        }
      } catch (err: any) {
        if (err?.response?.status === 404) setNotFound(true);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [id]);

  async function addFavorite() {
    if (!accessToken) {
      router.push("/login");
      return;
    }
    try {
      await api.post("/me/favorites", { carId: id }, { showLoading: false });
      alert("Added to favorites");
    } catch (err: any) {
      if (err?.response?.status === 409) alert("Already in favorites");
    }
  }

  async function addToCart() {
    if (!accessToken) {
      router.push("/login");
      return;
    }
    try {
      await api.post("/me/cart-items", { carId: id, quantity: 1 }, { showLoading: false });
      alert("Added to cart");
    } catch (err: any) {
      if (err?.response?.status === 409) alert("Already in cart");
    }
  }

  if (loading) {
    return (
      <div className="px-5!">
        <h1 className="h1 text-center my-6">Loading…</h1>
        <div className="card image-header animate-pulse">
          <div className="card-header" style={{ background: "gray", height: "300px" }} />
        </div>
      </div>
    );
  }

  if (notFound || !car) {
    return (
      <div className="px-5! text-center my-10">
        <h1 className="h1">Car not found</h1>
        <p className="h3">The listing you are looking for does not exist.</p>
        <Link href="/cars" className="button">Back to Cars</Link>
      </div>
    );
  }

  const mainImage = car.images.length > 0 ? car.images[selectedImage].url : "/ford.png";

  return (
    <div className="px-5! max-w-[1100px]! mx-auto!">
      <div className="my-4">
        <Link href="/cars" className="button secondary">← Back to Cars</Link>
      </div>

      <h1 className="h1 my-4">
        {car.year} {car.brandName ?? ""} {car.modelName ?? ""}
      </h1>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <div className="card">
            <div className="card-header p-0">
              <img
                src={mainImage}
                alt={`${car.brandName ?? ""} ${car.modelName ?? ""}`}
                className="w-full! h-[320px]! object-cover!"
              />
            </div>
            {car.images.length > 1 && (
              <div className="card-content d-flex flex-wrap p-2">
                {car.images.map((img, i) => (
                  <img
                    key={img.id}
                    src={img.url}
                    alt=""
                    onClick={() => setSelectedImage(i)}
                    className={`w-[80px]! h-[60px]! object-cover! m-1! border! cursor-pointer ${
                      i === selectedImage ? "border-blue-600" : "border-gray-300"
                    }`}
                  />
                ))}
              </div>
            )}
          </div>

          <div className="card mt-4">
            <div className="card-content p-4">
              <h2 className="h3">Specifications</h2>
              <table className="table table-striped">
                <tbody>
                  <tr><td className="w-[40%]!">Brand</td><td>{car.brandName ?? "—"}</td></tr>
                  <tr><td>Model</td><td>{car.modelName ?? "—"}</td></tr>
                  <tr><td>Category</td><td>{car.categoryName ?? "—"}</td></tr>
                  <tr><td>Year</td><td>{car.year}</td></tr>
                  <tr><td>Price</td><td className="text-2xl! font-bold!">${car.price.toLocaleString()}</td></tr>
                  <tr><td>Mileage</td><td>{car.mileage != null ? `${car.mileage.toLocaleString()} km` : "—"}</td></tr>
                  <tr><td>Color</td><td>{car.color ?? "—"}</td></tr>
                  <tr><td>Status</td><td>{car.status}</td></tr>
                </tbody>
              </table>

              {car.description && (
                <div className="mt-4">
                  <h3 className="h4">Description</h3>
                  <p className="text-gray-600">{car.description}</p>
                </div>
              )}

              <div className="d-flex flex-wrap mt-4">
                <button className="button success m-1" onClick={addFavorite}>Add to Favorites</button>
                <button className="button info m-1" onClick={addToCart}>Add to Cart</button>
              </div>
            </div>
          </div>
        </div>

        <div>
          {car.seller && (
            <div className="card">
              <div className="card-header">Seller</div>
              <div className="card-content p-4">
                <p className="text-lg! font-semibold">{car.seller.name}</p>
                <p className="text-gray-600">{car.seller.email}</p>
              </div>
            </div>
          )}

          {similar.length > 0 && (
            <div className="card mt-4">
              <div className="card-header">Similar Cars</div>
              <div className="card-content p-4">
                {similar.map((s) => (
                  <Link key={s.id} href={`/cars/${s.id}`} className="d-flex items-center! p-2 border! mb-2! no-hover">
                    <img
                      src={s.primaryImageUrl ?? "/ford.png"}
                      alt=""
                      className="w-[100px]! h-[70px]! object-cover!"
                    />
                    <div className="ml-3">
                      <p className="font-semibold">{s.year} {s.brandName ?? ""} {s.modelName ?? ""}</p>
                      <p className="text-gray-600">${s.price.toLocaleString()} · {s.mileage?.toLocaleString()} km</p>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Typecheck**

Run (in `car-app/`): `npx tsc --noEmit`
Expected: No type errors.

- [ ] **Step 3: Lint**

Run: `npm run lint`
Expected: No errors.

- [ ] **Step 4: Build**

Run: `npm run build`
Expected: Build succeeds.

- [ ] **Step 5: Manual smoke test**

Start API (`dotnet run --project API` from `car-backend/`) and frontend (`npm run dev` from `car-app/`). Open `http://localhost:3000/cars`, click "View Details" on a card, verify: gallery swaps on thumbnail click, spec table, seller card, similar cars link correctly, favorites/cart redirect to `/login` when logged out. Log in and confirm the buttons POST 204.

(No commit step — `car-app` is not a git repo.)

---

### Task 6: Wire "View Details" Button on /cars

**Files:**
- Modify: `car-app/app/cars/page.tsx` (card-footer around line 289-291)

**Interfaces:**
- Consumes: existing `/cars` list rendering; car ids available on each card.
- Produces: each card's "View Details" button navigates to `/cars/{id}`.

- [ ] **Step 1: Replace the button with a Link**

In `car-app/app/cars/page.tsx`, inside the card-footer div, replace:

```tsx
<div className="card-footer">
    <button className="button secondary">View Details</button>
</div>
```

with:

```tsx
<div className="card-footer">
    <Link href={`/cars/${car.id}`} className="button secondary">View Details</Link>
</div>
```

and ensure `import Link from "next/link";` is present at the top of the file (add it if missing).

- [ ] **Step 2: Lint + build**

Run (in `car-app/`): `npm run lint` then `npm run build`
Expected: Both succeed.

- [ ] **Step 3: Manual verification**

With API + frontend running, open `http://localhost:3000/cars` and click a card's "View Details" — browser navigates to `/cars/{id}` and the detail page renders.

(No commit step — `car-app` is not a git repo.)

---

### Task 7: Final Verification

- [ ] **Step 1: Backend build + tests**

Run (in `car-backend/`): `dotnet build API` then `dotnet test Tests`
Expected: Build succeeded; all tests green.

- [ ] **Step 2: Frontend lint + build**

Run (in `car-app/`): `npm run lint` && `npm run build`
Expected: Both succeed.

- [ ] **Step 3: End-to-end smoke**

With API + Redis + Postgres + frontend running:
- `/cars` lists seeded cars with working "View Details".
- `/cars/{id}` shows gallery, specs, seller, similar cars.
- Logged-out favorite/cart → redirects to `/login`; logged-in → 204 and UI feedback.

- [ ] **Step 4: Commit backend**

```bash
git -C car-backend status
git -C car-backend add -A
git -C car-backend commit -m "chore: verify car catalog detail pages end to end"
```

(Only if there are uncommitted backend changes; skip otherwise.)
