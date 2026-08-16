# Car Parts — Design

Date: 2026-08-12

## Context

The database currently models cars only (brands, models, categories, cars, images, reviews, orders, cart). The business also sells car parts (engines, batteries, etc.) which need the full treatment: browse, filter, images, reviews, cart, and orders.

## Decisions

| Topic | Decision |
|-------|----------|
| Scope | Full catalog + buying for parts |
| Car compatibility | Parts are generic; no links to specific cars |
| Part specs | JSON column for type-specific attributes |
| Categories | One `categories` table, add `type` discriminator (`Car` \| `Part`) |
| Cart/orders | One polymorphic flow via a base `products` table |
| Stock | Parts tracked in quantity; checked and deducted on order |
| Activity log | No `activities` table. Kafka topics carry events; `ILogger` structured logging per event. Revisit if a queryable history is needed |

## Data Model

Table-per-type (TPT) with EF Core. Shared primary key between `products` and the detail tables (`cars`, `parts`).

### products (new base table)

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | `uuid` | PK | shared with detail row |
| `product_type` | `varchar(10)` | NOT NULL | `Car` \| `Part` |
| `seller_id` | `uuid` | FK → users, NOT NULL | |
| `category_id` | `uuid` | FK → categories | |
| `price` | `decimal(12,2)` | NOT NULL | |
| `quantity` | `int` | NOT NULL, default 1 | stock; cars stay 1 |
| `status` | `varchar(20)` | NOT NULL | `Active`, `Sold`, `Inactive` |
| `description` | `text` | | moved from cars, shared |
| `specs` | `jsonb` | | parts-only; null for cars |
| audit fields | | | |

### cars (becomes detail table)

| Column | Notes |
|--------|-------|
| `id` | PK = products.id |
| `brand_id` | FK → brands |
| `model_id` | FK → models |
| `year` | |
| `mileage` | |
| `color` | |

`price`, `status`, `description` move out to `products`.

### parts (new detail table)

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | `uuid` | PK = products.id | |
| `name` | `varchar(200)` | NOT NULL | e.g. "12V 75Ah Battery" |
| `brand` | `varchar(100)` | | manufacturer, e.g. Bosch |
| `sku` | `varchar(100)` | UNIQUE | part number |

### categories

Add `type` column: `varchar` `Car` \| `Part`. Existing rows backfilled to `Car`. New part categories seeded (Engine, Battery, Brakes, Filters, ...).

### Refactored — `CarId` → `ProductId`

- `car_images` → `product_images`
- `reviews`
- `favorites` — PK becomes `(user_id, product_id)`
- `cart_items` — PK becomes `(user_id, product_id)`
- `order_items`

## API & Services

Generic CRUD (`MapEntityEndpoints<T>`) cannot create TPT entities: POSTing a car must write both `products` and `cars` rows. New specialized service replaces generic for products.

### ProductService (Services/)

Backed by `ICachedRepository<Product>`. Methods:
- `CreateCarAsync` / `UpdateCarAsync` — product + car rows in a transaction
- `CreatePartAsync` / `UpdatePartAsync` — product + part rows in a transaction
- `GetProductAsync(id)` — product joined with car or part detail
- `GetCatalogAsync(type, categoryId, minPrice, maxPrice, page, pageSize)` — filtered, paged, cached

### Endpoints (minimal APIs)

| Route | Auth | Notes |
|-------|------|-------|
| `GET /api/products?type=car\|part&categoryId=&minPrice=&maxPrice=` | public | catalog, paged |
| `GET /api/products/{id}` | public | product + detail + images |
| `GET /api/parts` | public | part list, same filters |
| `POST/PUT/DELETE /api/parts` | Admin | part CRUD |
| `POST/PUT/DELETE /api/cars` | Admin | switched to ProductService |
| `GET /api/cars`, `GET /api/cars/{id}` | public | kept, backed by ProductService |

Refactored endpoints: `car-images` → `product-images` (GET public, write Admin), `reviews`, `favorites`, `cart-items`, `order-items` — same routes, ProductId payloads.

### DTOs (API/DTOs/)

- `ProductDto` — type, id, price, quantity, status, category, seller, thumbnail
- `CarDto` — updated, adds product fields
- `PartDto` — id, name, brand, sku, categoryId, price, quantity, specs, status, description, sellerId, images
- `CartItemDto`, `OrderItemDto`, `ReviewDto`, `FavoriteDto` — CarId → ProductId

Existing `Service<T>` / repositories stay untouched for `User`, `Brand`, `CarModel`, `Category`, `Order`, `Review`, `Favorite`, `CartItem`, `OrderItem` — only FK target changes.

## Buying Flow

### Cart (`cart-items`)

- Add: validate product `Active`, `quantity > 0`, requested qty ≤ stock → 400/409 otherwise
- Redis key `cart:{user_id}` unchanged; entries carry productId

### Order (`orders` + `order-items`)

- Create order: single transaction — lock product rows (`SELECT ... FOR UPDATE`), re-check stock, deduct `quantity`, compute `total_amount`
- Cars: qty fixed 1; product status → `Sold`
- Parts: decrement stock; status → `Inactive` at qty 0
- Insufficient stock → 409, no partial order

## Caching (Redis)

| Key | TTL | Invalidated by |
|-----|-----|----------------|
| `product:{id}` | 10 min | product update |
| `products:list:{type}:{filters}` | 5 min | any product CRUD |
| `cart:{user_id}` | 1 h | cart add/remove |
| `user:{id}`, `session:{token}` | unchanged | |

## Kafka

| Topic | Producer | Consumer | Purpose |
|-------|----------|----------|---------|
| `product.listed` | API | API (cache invalidation) | renamed from `car.listed` |
| `product.updated` | API | API (cache invalidation) | renamed from `car.updated` |
| `inventory.updated` | API | Order service | stock deduction / restock |
| `order.created`, `order.paid`, `order.shipped` | API | unchanged | |
| `scraper.data` | Scraper | API | unchanged |

### Event logging

Every event produced/consumed is logged via `ILogger` with structured data: event type, productId/orderId, correlationId, timestamp. CorrelationId propagates API request → Kafka event → consumer log, linking the full flow. No `activities` table for now.

## Migration (EF Core)

1. Create `products`
2. Add `type` to `categories`, backfill existing → `Car`
3. Backfill `products` from existing `cars`
4. Create `parts`, `product_images` (migrate `car_images`, CarId → ProductId)
5. Refactor `reviews`, `favorites`, `cart_items`, `order_items`
6. Drop `price`/`status`/`description` from `cars`
7. Seed part categories (Engine, Battery, Brakes, Filters, ...)
8. Seed sample parts

## Testing

- `ProductServiceTests` — car/part create + update write both rows; transaction rollback on failure
- `StockTests` — cart stock validation; order deducts stock; insufficient stock → no partial order
- `PartCrudTests` — part CRUD via ProductService
- `GenericRepositoryTests` / `GenericServiceTests` — no regression from FK refactor
- Migration backfill test — car count matches product count, data intact
