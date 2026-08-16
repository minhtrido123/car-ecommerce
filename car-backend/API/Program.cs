using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults();
builder.Services.AddDbContext<SorchaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddCaching(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddJwtAuthentication(builder.Configuration["Jwt:Issuer"] ?? "", builder.Configuration["Jwt:Audience"] ?? "", builder.Configuration["Jwt:Secret"] ?? "");
builder.Services.AddScoped(typeof(ICachedRepository<>), typeof(CachedRepository<>));
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod(); ;
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("FrontEnd");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

MapEntityEndpoints<User>(app, "users");
MapEntityEndpoints<Brand>(app, "brands");
MapEntityEndpoints<CarModel>(app, "car-models");
MapEntityEndpoints<Category>(app, "categories");
MapCarEndpoints(app);
MapEntityEndpoints<ProductImage>(app, "product-images");
MapImageEndpoints(app);
MapMenuEndpoints(app);
MapEntityEndpoints<Review>(app, "reviews");
MapEntityEndpoints<Order>(app, "orders");
MapEntityEndpoints<OrderItem>(app, "order-items");
MapEntityEndpoints<Favorite>(app, "favorites");
MapEntityEndpoints<CartItem>(app, "cart-items");

MapAuthEndpoints(app);
MapAdminEndpoints(app);
MapCustomerActionEndpoints(app);
MapProductEndpoints(app);
MapOrderEndpoints(app);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SorchaDbContext>();
    if (!db.MenuItems.Any())
    {
        db.MenuItems.AddRange(
            new MenuItem { Label = "Cars", Url = "/cars", Order = 0 },
            new MenuItem { Label = "Car Parts", Url = "/windows", Order = 1 },
            new MenuItem { Label = "Blogs", Url = "/windows", Order = 2 });
        await db.SaveChangesAsync();
    }
}

app.Run();

static void MapEntityEndpoints<T>(WebApplication app, string routePrefix, string? queryRole = null, string commandRole = "Admin") where T : EntityBase
{
    var group = queryRole != null ? app.MapGroup($"/api/{routePrefix}")
        .RequireAuthorization(new AuthorizeAttribute { Roles = queryRole }) :
        app.MapGroup($"/api/{routePrefix}");

    var postGroup = app.MapGroup($"/api/{routePrefix}")
        .RequireAuthorization(new AuthorizeAttribute { Roles = commandRole });

    group.MapGet("/", async ([FromServices] IService<T> service,
        CancellationToken cancel,
        int pageNumber = 1, int pageSize = 20, string? includes = null
        ) =>
    {
        string[] includeArray = includes != null && includes.Any()
        ? includes.Split(",") : [];
        var result = await service.GetPagedAsync(pageNumber, pageSize, includeArray, cancel);
        return Results.Ok(result);
    }).WithName($"GetAll_{typeof(T).Name}");

    group.MapGet("/{id:guid}", async ([FromServices] IService<T> service, Guid id) =>
    {
        var entity = await service.GetByIdAsync(id);
        return entity is not null ? Results.Ok(entity) : Results.NotFound();
    }).WithName($"GetById_{typeof(T).Name}");

    postGroup.MapPost("/", async ([FromServices] IService<T> service, CreateRequest<T> request) =>
    {
        var entity = await service.CreateAsync(request);
        return Results.CreatedAtRoute($"GetById_{typeof(T).Name}", new { id = entity.Id }, entity);
    }).WithName($"Create_{typeof(T).Name}");

    postGroup.MapPut("/{id:guid}", async ([FromServices] IService<T> service, Guid id, UpdateRequest<T> request) =>
    {
        if (id != request.Id) return Results.BadRequest();
        var entity = await service.UpdateAsync(request);
        return Results.Ok(entity);
    }).WithName($"Update_{typeof(T).Name}");

    postGroup.MapDelete("/{id:guid}", async ([FromServices] IService<T> service, Guid id) =>
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }).WithName($"Delete_{typeof(T).Name}");
}

static void MapImageEndpoints(WebApplication app)
{
    var admin = app.MapGroup("/api")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    admin.MapPost("/images/upload", async ([FromServices] IService<ProductImage> service,
        IFormFile file, CancellationToken cancel) =>
    {
        if (file.Length == 0) return Results.BadRequest(new { error = "Empty file" });
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) return Results.BadRequest(new { error = "File must have an extension" });

        var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var dir = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        await using (var stream = File.Create(path))
        {
            await file.CopyToAsync(stream, cancel);
        }
        return Results.Ok(new { url = $"/uploads/{name}" });
    }).WithName("Upload_Image").DisableAntiforgery();

    admin.MapPut("/product-images/reorder", async ([FromServices] IService<ProductImage> service,
        ReorderRequest request, CancellationToken cancel) =>
    {
        for (var i = 0; i < request.Ids.Count; i++)
        {
            var image = await service.GetByIdAsync(request.Ids[i], cancel);
            if (image is null) continue;
            image.Position = i;
            await service.UpdateAsync(new UpdateRequest<ProductImage>(image.Id, image), cancel);
        }
        return Results.NoContent();
    }).WithName("Reorder_ProductImages");

    admin.MapGet("/product-images/by-product/{productId:guid}", async ([FromServices] IService<ProductImage> service,
        Guid productId, CancellationToken cancel) =>
    {
        var images = await service.GetFilteredAsync(pi => pi.ProductId == productId, cancel);
        return Results.Ok(images.OrderBy(pi => pi.Position).ToList());
    }).WithName("GetProductImagesByProduct");
}

static void MapMenuEndpoints(WebApplication app)
{
    app.MapGroup("/api/menu").MapGet("/", async ([FromServices] IService<MenuItem> service, CancellationToken cancel) =>
    {
        var items = await service.GetFilteredAsync(m => m.IsActive, cancel);
        return Results.Ok(items.OrderBy(m => m.Order).ToList());
    }).WithName("Get_Menu");

    var admin = app.MapGroup("/api/menu")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    admin.MapGet("/all", async ([FromServices] IService<MenuItem> service, CancellationToken cancel) =>
    {
        var items = await service.GetFilteredAsync(_ => true, cancel);
        return Results.Ok(items.OrderBy(m => m.Order).ToList());
    }).WithName("GetAll_MenuItem");

    admin.MapGet("/{id:guid}", async ([FromServices] IService<MenuItem> service, Guid id, CancellationToken cancel) =>
    {
        var item = await service.GetByIdAsync(id, cancel);
        return item is not null ? Results.Ok(item) : Results.NotFound();
    }).WithName("GetById_MenuItem");

    admin.MapPost("/", async ([FromServices] IService<MenuItem> service,
        CreateRequest<MenuItem> request, CancellationToken cancel) =>
    {
        var existing = await service.GetFilteredAsync(_ => true, cancel);
        request.Data.Order = existing.Count == 0 ? 0 : existing.Max(m => m.Order) + 1;
        var entity = await service.CreateAsync(request, cancel);
        return Results.CreatedAtRoute("GetById_MenuItem", new { id = entity.Id }, entity);
    }).WithName("Create_MenuItem");

    admin.MapPut("/{id:guid}", async ([FromServices] IService<MenuItem> service,
        Guid id, UpdateRequest<MenuItem> request, CancellationToken cancel) =>
    {
        if (id != request.Id) return Results.BadRequest();
        var entity = await service.UpdateAsync(request, cancel);
        return Results.Ok(entity);
    }).WithName("Update_MenuItem");

    admin.MapDelete("/{id:guid}", async ([FromServices] IService<MenuItem> service,
        Guid id, CancellationToken cancel) =>
    {
        await service.DeleteAsync(id, cancel);
        return Results.NoContent();
    }).WithName("Delete_MenuItem");
}

static void MapAuthEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/auth");

    group.MapPost("/register", async ([FromServices] IAuthService authService, RegisterRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await authService.RegisterAsync(request.Email, request.Password, request.Name, request.RememberMe, cancellationToken);
            return Results.Json(ToAuthResponse(result), statusCode: StatusCodes.Status201Created);
        }
        catch (EmailAlreadyExistsException)
        {
            return Results.Conflict(new { error = "Email already registered" });
        }
    });

    group.MapPost("/login", async ([FromServices] IAuthService authService, LoginRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await authService.LoginAsync(request.Email, request.Password, request.RememberMe, cancellationToken);
            return Results.Ok(ToAuthResponse(result));
        }
        catch (InvalidCredentialsException)
        {
            return Results.Unauthorized();
        }
    });

    group.MapPost("/refresh", async ([FromServices] IAuthService authService, RefreshRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
            return Results.Ok(ToAuthResponse(result));
        }
        catch (InvalidRefreshTokenException)
        {
            return Results.Unauthorized();
        }
    });
}

static AuthResponse ToAuthResponse(AuthResult result) =>
    new(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAt,
        new UserResponse(result.UserId, result.Email, result.Role, result.Name, result.CreatedAt));

static UserResponse ToUserResponse(User user) =>
    new(user.Id, user.Email, user.Role, user.Name, user.CreatedAt);

static void MapCarEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/cars");
    var admin = app.MapGroup("/api/cars")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    group.MapGet("/", async ([FromServices] ICarService carService,
        CancellationToken cancel, int pageNumber = 1, int pageSize = 20,
        string? search = null, string? sortBy = null, string? sortDir = null, string? includes = null,
        decimal? minPrice = null, decimal? maxPrice = null,
        int? minMileage = null, int? maxMileage = null,
        int? minYear = null, int? maxYear = null,
        string? color = null, string? status = null, string? brandIds = null) =>
    {
        _ = includes;
        var result = await carService.SearchCarsAsync(
            search, sortBy, sortDir, pageNumber, pageSize,
            minPrice, maxPrice, minMileage, maxMileage, minYear, maxYear,
            color, status, brandIds?.Split(",", StringSplitOptions.RemoveEmptyEntries),
            cancel);
        return Results.Ok(result);
    }).WithName("GetAll_Car");

    group.MapGet("/filters", async ([FromServices] ICarService carService, CancellationToken cancel) =>
        Results.Ok(await carService.GetFiltersAsync(cancel))).WithName("GetCarFilters");

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

    admin.MapPost("/", async ([FromServices] IProductService service, [FromServices] ICarService carService,
        CreateRequest<Car> request, CancellationToken cancel) =>
    {
        var entity = await service.CreateCarAsync(request.Data, cancel);
        var detail = await carService.GetDetailAsync(entity.Id, cancel);
        return Results.CreatedAtRoute("GetById_Car", new { id = entity.Id }, detail is null ? CarMapping.ToDetail(entity) : CarMapping.ToDetail(detail));
    }).WithName("Create_Car");

    admin.MapPut("/{id:guid}", async ([FromServices] IProductService service, [FromServices] ICarService carService,
        Guid id, UpdateRequest<Car> request, CancellationToken cancel) =>
    {
        if (id != request.Id) return Results.BadRequest();
        request.Data.Id = id;
        var entity = await service.UpdateCarAsync(request.Data, cancel);
        var detail = await carService.GetDetailAsync(entity.Id, cancel);
        return Results.Ok(detail is null ? CarMapping.ToDetail(entity) : CarMapping.ToDetail(detail));
    }).WithName("Update_Car");

    admin.MapDelete("/{id:guid}", async ([FromServices] IProductService service, Guid id, CancellationToken cancel) =>
    {
        await service.DeleteAsync(id, cancel);
        return Results.NoContent();
    }).WithName("Delete_Car");
}

static void MapCustomerActionEndpoints(WebApplication app)
{
    var favorites = app.MapGroup("/api/me/favorites").RequireAuthorization();
    var cart = app.MapGroup("/api/me/cart-items").RequireAuthorization();

    favorites.MapPost("/", async ([FromServices] IProductService productService,
        ClaimsPrincipal user, AddFavoriteRequest request, CancellationToken cancel) =>
    {
        var added = await productService.AddFavoriteAsync(GetUserId(user), request.ProductId, cancel);
        return added ? Results.NoContent() : Results.Conflict(new { error = "Already favorited" });
    });

    favorites.MapDelete("/{productId:guid}", async ([FromServices] IProductService productService,
        ClaimsPrincipal user, Guid productId, CancellationToken cancel) =>
    {
        await productService.RemoveFavoriteAsync(GetUserId(user), productId, cancel);
        return Results.NoContent();
    });

    cart.MapPost("/", async ([FromServices] IProductService productService,
        ClaimsPrincipal user, AddCartItemRequest request, CancellationToken cancel) =>
    {
        try
        {
            var added = await productService.AddCartItemAsync(GetUserId(user), request.ProductId, request.Quantity, cancel);
            return added ? Results.NoContent() : Results.Conflict(new { error = "Already in cart" });
        }
        catch (InsufficientStockException)
        {
            return Results.Conflict(new { error = "Insufficient stock" });
        }
    });

    cart.MapDelete("/{productId:guid}", async ([FromServices] IProductService productService,
        ClaimsPrincipal user, Guid productId, CancellationToken cancel) =>
    {
        await productService.RemoveCartItemAsync(GetUserId(user), productId, cancel);
        return Results.NoContent();
    });
}

static void MapProductEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/products");
    var parts = app.MapGroup("/api/parts");
    var adminParts = app.MapGroup("/api/parts")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    group.MapGet("/", async ([FromServices] IProductService service, CancellationToken cancel,
        string? type = null, Guid? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null,
        int pageNumber = 1, int pageSize = 20) =>
    {
        var result = await service.GetCatalogAsync(type, categoryId, minPrice, maxPrice, pageNumber, pageSize, cancel);
        return Results.Ok(result);
    }).WithName("GetAll_Product");

    group.MapGet("/{id:guid}", async ([FromServices] IProductService service, Guid id, CancellationToken cancel) =>
    {
        var product = await service.GetDetailAsync(id, cancel);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }).WithName("GetById_Product");

    parts.MapGet("/", async ([FromServices] IProductService service, CancellationToken cancel,
        Guid? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null,
        int pageNumber = 1, int pageSize = 20) =>
    {
        var result = await service.GetPartsAsync(categoryId, minPrice, maxPrice, pageNumber, pageSize, cancel);
        return Results.Ok(result);
    }).WithName("GetAll_Part");

    parts.MapGet("/{id:guid}", async ([FromServices] IProductService service, Guid id, CancellationToken cancel) =>
    {
        var part = await service.GetPartAsync(id, cancel);
        return part is null ? Results.NotFound() : Results.Ok(part);
    }).WithName("GetById_Part");

    adminParts.MapPost("/", async ([FromServices] IProductService service, CreateRequest<Part> request, CancellationToken cancel) =>
    {
        var entity = await service.CreatePartAsync(request.Data, cancel);
        var part = await service.GetPartAsync(entity.Id, cancel);
        return Results.CreatedAtRoute("GetById_Part", new { id = entity.Id }, part);
    }).WithName("Create_Part");

    adminParts.MapPut("/{id:guid}", async ([FromServices] IProductService service,
        Guid id, UpdateRequest<Part> request, CancellationToken cancel) =>
    {
        if (id != request.Id) return Results.BadRequest();
        request.Data.Id = id;
        var entity = await service.UpdatePartAsync(request.Data, cancel);
        var part = await service.GetPartAsync(entity.Id, cancel);
        return Results.Ok(part);
    }).WithName("Update_Part");

    adminParts.MapDelete("/{id:guid}", async ([FromServices] IProductService service, Guid id, CancellationToken cancel) =>
    {
        await service.DeleteAsync(id, cancel);
        return Results.NoContent();
    }).WithName("Delete_Part");
}

static Guid GetUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException();
}

static void MapOrderEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/me/orders").RequireAuthorization();

    group.MapPost("/", async ([FromServices] IOrderService orderService, ClaimsPrincipal user,
        Models.CreateOrderRequest request, CancellationToken cancel) =>
    {
        try
        {
            var order = await orderService.CreateAsync(GetUserId(user), request, cancel);
            return Results.Created($"/api/me/orders/{order.Id}", order);
        }
        catch (InsufficientStockException)
        {
            return Results.Conflict(new { error = "Insufficient stock for one or more items" });
        }
    }).WithName("CreateMyOrder");
}

static void MapAdminEndpoints(WebApplication app)
{
    var admin = app.MapGroup("/api/admins")
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

    admin.MapPost("/", async ([FromServices] IAdminService adminService, CreateAdminRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var user = await adminService.CreateAdminAsync(request.Email, request.Password, request.Name, request.Role, cancellationToken);
            return Results.Json(ToUserResponse(user), statusCode: StatusCodes.Status201Created);
        }
        catch (EmailAlreadyExistsException)
        {
            return Results.Conflict(new { error = "Email already registered" });
        }
    }).WithName("Create_Admin");

    admin.MapPut("/{id:guid}", async ([FromServices] IAdminService adminService, Guid id, UpdateAdminRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var user = await adminService.UpdateAdminAsync(id, request.Email, request.Name, request.Role, request.Password, cancellationToken);
            return Results.Ok(ToUserResponse(user));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (EmailAlreadyExistsException)
        {
            return Results.Conflict(new { error = "Email already registered" });
        }
    }).WithName("Update_Admin");
}
