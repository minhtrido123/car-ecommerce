# Cars Page — React Pagination/Search/Sort Design

**Date:** 2026-08-13

## Problem

The cars listing page (`car-app/app/(main)/cars/page.tsx`) renders through the
Metro UI `data-role="list"` plugin, which loads all cars in one request and
paginates/sorts client-side. It cannot paginate, search, or sort by calling the
API per interaction, and it depends on the global `Metro` plugin state.

## Goal

Reimplement the cars page so pagination, search, and sort each trigger a server
call to `GET /api/cars`.

## Backend Changes (`car-backend`)

### `CarService.SearchCarsAsync`

New method on `ICarService`/`CarService`:

```csharp
Task<PagedResult<Car>> SearchCarsAsync(
    string? search, string? sortBy, string? sortDir,
    int pageNumber, int pageSize, CancellationToken cancellationToken = default);
```

Behavior:
- Query: `_db.Cars.AsNoTracking()` with `Include(ProductImages)`, `Include(Brand)`, `Include(Model)`.
- `search` (optional): case-insensitive `Contains` over `Brand.Name`, `Model.Name`, `Description`.
- `sortBy` (optional): `price` | `year` | `mileage` | `createdAt`. Default `createdAt`.
- `sortDir` (optional): `asc` | `desc`. Default `desc` (newest first).
- Returns `PagedResult<Car>` (count + paged items).

### `MapCarEndpoints` GET `/`

Replace the generic `IService<Car>.GetPagedAsync` handler with `ICarService.SearchCarsAsync`.
Query params: `search`, `sortBy`, `sortDir`, `pageNumber`, `pageSize`, `includes`
(`includes` accepted for backward compatibility; images always included).

### Tests

`Tests/CarServiceTests.cs` additions:
- `SearchCarsAsync_FiltersByBrandOrModelOrDescription`
- `SearchCarsAsync_SortsByPriceAscAndDesc`
- `SearchCarsAsync_SortsByYearDefaultNewestFirst`
- `SearchCarsAsync_PaginatesWithTotalCount`

## Frontend Changes (`car-app`)

### `cars/page.tsx`

Remove:
- `data-role="list"` attribute + Metro list options on the `<ul>`
- `Metro.getPlugin("#cars", "list")` calls and `.list-top`/`.list-bottom` DOM removal
- `sortList()` Metro helper
- The `useEffect` that wires the list plugin

Add React state: `search`, `sortBy` (`createdAt` default), `sortDir` (`desc` default),
`pageNumber`, `pageSize` (20), `cars`, `totalCount`, `loading`.

`fetchCars` calls `GET /api/cars` with `pageNumber`, `pageSize`, `search`, `sortBy`,
`sortDir`, `includes=ProductImages`. Each interaction:
- Search input: debounced 400ms, resets to page 1.
- Sort buttons (Price / Year / Mileage / Newest): toggle asc/desc, reset to page 1.
- Pagination: sets `pageNumber`.

UI:
- Result count line (`N cars found`).
- Skeleton loading while fetching; "No cars found" empty state.
- Keep existing Metro card markup for each car.

### `Pagination.tsx`

Add `onPageChange(page: number)` prop; wire Prev/Next/number links to it. Show
page numbers per current page. Stop using dead `<a href="#">` links.

## Out of Scope

- `/api/products`, `ProductService`, part pages, detail page.
- Visual restyle of cards.

## Addendum: Filter Panel (2026-08-13)

### Backend

`CarService.SearchCarsAsync` gains optional filter params:
`minPrice`, `maxPrice`, `minMileage`, `maxMileage`, `minYear`, `maxYear`,
`color` (exact), `status` (exact), `brandIds` (comma-separated list).

New `CarService.GetFiltersAsync()` returns a `CarFilters` record
(`Models/CarFilters.cs`):
`Brands` (id/name, distinct), `Colors`, `Statuses`, and the DB min/max for
price, mileage, and year — computed via distinct queries.

New endpoint `GET /api/cars/filters` returns the `CarFilters` record.

### Frontend

Collapsible filter panel above the grid (default open), populated from
`/api/cars/filters`:

- Price range — dual `input[type=range]` (native, no Metro plugin), $ labels
- Mileage range — dual slider, km labels
- Year range — dual slider
- Color dropdown — from filters endpoint
- Status dropdown — from filters endpoint
- Brand checkboxes — from filters endpoint
- Reset button clears all filters

Behavior: slider drags debounced 500ms then refetch; dropdowns/checkboxes
refetch on change; any filter change resets to page 1. Filters compose with
existing search/sort/pagination in a single request.

### Tests

`CarServiceTests`: price/mileage/year range filters, color/status/brand
filters, `GetFiltersAsync` returns distinct values and correct bounds.

## Verification

- `dotnet test Tests` green (backend).
- `bunx tsc --noEmit` clean (frontend).
- Manual: search debounces and refetches; sort buttons refetch; pagination moves
  pages; result count correct; cards render with images; filters compose with
  each other and with search/sort/pagination.
