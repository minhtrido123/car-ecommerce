# Car Catalog Seed + Detail Pages — Design

Date: 2026-08-11

## Purpose

1. Seed the PostgreSQL database with a realistic car catalog: brands, models, categories, cars, and car images.
2. Build per-car detail pages in the frontend (`/cars/[id]`) with gallery, spec table, seller info, similar cars, and add-to-favorites/cart.
3. Add the backend support needed: Car relationship navigations, a dedicated car-detail endpoint, a similar-cars endpoint, and customer-facing favorites/cart endpoints.

## Current State

- `Cars` table has 2 orphaned rows (NULL brand/model/category/seller). No FK constraints exist between `Cars` and `Brands`, `CarModels`, `Categories`, or `Users`.
- Brands, models, categories tables are empty.
- `Car` entity has only a `CarImages` navigation.
- Generic entity endpoints exist for everything; command endpoints require `Admin` role, so customers cannot POST favorites/cart-items through them.
- Frontend `/cars` page renders a Metro UI list; its "View Details" button does nothing.

## 1. Database Seed (~50 cars)

New console project `car-backend/Seed/` referencing `Infrastructure` and `Models`, reusing `SorchaDbContext` (reads `Database` connection string). Runs idempotently: skips any brand/model/category whose name already exists.

Data to insert:

- **Brands** (~10, with country): Toyota (Japan), Honda (Japan), Ford (USA), Chevrolet (USA), BMW (Germany), Mercedes-Benz (Germany), Audi (Germany), Hyundai (South Korea), Kia (South Korea), Tesla (USA).
- **Models** (3–4 per brand, with year ranges): e.g. Toyota Camry/Corolla/RAV4/Highlander, Honda Civic/Accord/CR-V, Ford F-150/Mustang/Escape, Chevrolet Silverado/Tahoe/Camaro, BMW 3 Series/X5/5 Series, Mercedes C-Class/GLE/E-Class, Audi A4/Q5/A6, Hyundai Elantra/Tucson/Sonata, Kia Sportage/Telluride/Optima, Tesla Model 3/Model Y/Model S.
- **Categories** (6): Sedan, SUV, Truck, Coupe, Hatchback, Electric.
- **Cars** (~50): spread across models, varied year (2012–2025), price ($8k–$120k), mileage, colors, status `Active`. All assigned to one existing seller user (looked up by email; skip if no users exist). 1–3 `CarImage` rows per car with real image URLs (Unsplash or similar), one `IsPrimary = true`.

Existing 2 cars remain untouched (nullable FKs preserved).

## 2. Backend — Car Relationships + Dedicated Endpoints

### 2.1 Navigation properties

`Models/Car.cs` gains:

```csharp
public Brand? Brand { get; set; }
public CarModel? Model { get; set; }
public Category? Category { get; set; }
public User? Seller { get; set; }
```

### 2.2 FK configuration + migration

`Infrastructure/SorchaDbContext.cs` configures relationships:

- `BrandId` → `Brands.Id`
- `ModelId` → `CarModels.Id`
- `CategoryId` → `Categories.Id`
- `SellerId` → `Users.Id`

All optional (nullable FK). New EF migration `AddCarRelationships` created via `dotnet ef migrations add`.

### 2.3 New endpoints

Registered in `API/Program.cs`. New DTOs in `API/DTOs/CarDto.cs`:

The generic `MapEntityEndpoints<Car>(app, "cars")` call is replaced by a dedicated `MapCarEndpoints(app)` that keeps the existing paged list GET (`/api/cars`, with `includes`) plus the new detail and similar-cars endpoints — avoiding a duplicate `GET /api/cars/{id:guid}` route.

```csharp
public record CarDetailResponse(
    Guid Id, int Year, decimal Price, int? Mileage, string? Color,
    string? Description, string Status, DateTime CreatedAt,
    string? BrandName, string? ModelName, string? CategoryName,
    SellerResponse? Seller, List<CarImageResponse> Images);

public record SellerResponse(Guid Id, string Name, string Email);
```

Reuses existing `CarImageResponse` from `API/DTOs/CarImageDto.cs` (Id, CarId, Url, IsPrimary, CreatedAt).

- `GET /api/cars/{id}` — **replaces** the generic cars GetById. Returns 404 if car not found. Includes `Brand`, `Model`, `Category`, `Seller`, `CarImages`.
- `GET /api/cars/by-model/{modelId:guid}?limit=4` — similar cars (same `ModelId`, excluding a given car via `?exclude=`), ordered by year desc. Returns list of `CarCardResponse` (id, brand name, model name, year, price, mileage, color, primary image URL).
- `POST /api/favorites` (auth) — body `{ "carId": "..." }`, `UserId` taken from JWT `sub` claim. 409 if already favorited.
- `DELETE /api/favorites/{carId}` (auth) — removes favorite.
- `POST /api/cart-items` (auth) — body `{ "carId": "...", "quantity": 1 }`, `UserId` from JWT. Quantity default 1.
- `DELETE /api/cart-items/{carId}` (auth) — removes cart item.

The existing generic `MapEntityEndpoints<Favorite>` / `<CartItem>` admin command endpoints stay as-is (admin management); the new auth endpoints serve customers.

## 3. Frontend — Car Detail Page

### 3.1 Route

New client page `app/cars/[id]/page.tsx`:

- Fetches `GET /cars/{id}` via existing `api` client.
- **Photo gallery**: primary image large + thumbnail strip; clicking a thumbnail swaps the main image. Metro UI lightbox styling consistent with existing theme.
- **Spec table**: brand, model, category, year, price (formatted), mileage, color, status (badge).
- **Seller info**: name + email card.
- **Similar cars**: 4 cards from `GET /cars/by-model/{modelId}?exclude={id}`; clicking navigates to that car's detail.
- **Add to favorites / cart** buttons: visible only when logged in (`useAuthStore.accessToken`); otherwise redirect to `/login`. POST to the new auth endpoints; optimistic UI feedback (button state change) and alert on 409.
- **Back button** → `/cars`.
- **Not-found state**: styled "Car not found" message with link back to `/cars` when API returns 404.

### 3.2 `/cars` page

- "View Details" button becomes a `<Link href={"/cars/" + car.id}>`.

## 4. Error Handling & Testing

- Endpoints use `Results.*`; 404 for missing car/favorite; 409 for duplicate favorite.
- Backend: unit tests for detail/similar endpoints (via existing test project patterns); seed idempotency check (running twice yields same counts).
- Frontend: `npm run lint`, `npm run build`.
- Backend: `dotnet build`, `dotnet test`.

## Open Questions

- None. Scope confirmed with user.

## Out of Scope

- Car listing filters on `/cars` (already commented out in page).
- Review, order, payment flows.
- Admin CRUD UI.
