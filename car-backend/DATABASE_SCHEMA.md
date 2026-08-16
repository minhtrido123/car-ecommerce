# Car Ecommerce — Database Schema

## Full Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **API** | ASP.NET Core 10 (C#) | REST API |
| **ORM** | EF Core 10 + Npgsql | PostgreSQL queries |
| **Cache** | Redis | Session cache, product catalog cache, rate limiting, cart cache |
| **Events** | Kafka | Order events, inventory updates, notifications, scraper pipeline |
| **Scraper** | Playwright (.NET) | Headless browser scraping of car listing sites → Kafka |
| **Testing** | (TBD) | E2E / integration tests |

## Audit Fields (all entities)

| Column | Type | Description |
|--------|------|-------------|
| `created_at` | `timestamptz` | Record creation timestamp |
| `created_by` | `uuid` | User who created |
| `updated_at` | `timestamptz` | Last modification timestamp |
| `updated_by` | `uuid` | User who last modified |

## Polymorphic Product Model (Table-per-Type)

The catalog uses EF Core table-per-type (TPT) inheritance. `products` is the base
table holding shared commerce fields; `cars` and `parts` are detail tables whose
primary key is also a foreign key to `products.id` (one-to-one). All commerce
relations (reviews, favorites, cart items, order items) point at `products.id`,
so a single model serves both cars and parts.

```
products (base)
  ├── cars (detail, TPT)
  └── parts (detail, TPT)
```

## Entities

### 1. users

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | Primary key |
| `email` | `varchar(255)` | UNIQUE, NOT NULL | Email address |
| `password_hash` | `varchar(512)` | NOT NULL | Hashed password |
| `role` | `varchar(20)` | NOT NULL | `Customer`, `Seller`, `Admin` |
| `name` | `varchar(255)` | NOT NULL | Full name |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 2. brands

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `name` | `varchar(100)` | UNIQUE, NOT NULL | e.g. Toyota, Honda, Ford |
| `country` | `varchar(100)` | | Country of origin |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 3. models

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `brand_id` | `uuid` | FK → `brands(id)`, NOT NULL | |
| `name` | `varchar(100)` | NOT NULL | e.g. Camry, Civic, F-150 |
| `year_start` | `int` | | First model year |
| `year_end` | `int` | | Last model year (null = current) |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 4. categories

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `name` | `varchar(50)` | UNIQUE, NOT NULL | e.g. Sedan, SUV, Battery |
| `slug` | `varchar(50)` | UNIQUE, NOT NULL | URL-friendly identifier |
| `type` | `varchar(10)` | NOT NULL, INDEX, DEFAULT `'Car'` | `Car` or `Part` |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 5. products (base)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | Primary key (shared by `cars`/`parts`) |
| `product_type` | `varchar(10)` | NOT NULL, INDEX | `Car` or `Part` |
| `seller_id` | `uuid` | FK → `users(id)`, INDEX | Listing owner |
| `category_id` | `uuid` | FK → `categories(id)`, INDEX | |
| `price` | `decimal(12,2)` | NOT NULL | Listing price |
| `quantity` | `int` | NOT NULL, DEFAULT 1 | Stock count |
| `status` | `varchar(20)` | NOT NULL, INDEX, DEFAULT `'Active'` | `Active`, `Sold`, `Inactive` |
| `description` | `text` | | Detailed description |
| `specs` | `jsonb` | | Structured spec data |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 6. cars (detail, TPT)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK, FK → `products(id)` (cascade) | Shared PK |
| `brand_id` | `uuid` | FK → `brands(id)` | |
| `model_id` | `uuid` | FK → `models(id)` | |
| `year` | `int` | NOT NULL, INDEX | Model year |
| `mileage` | `int` | | Odometer reading |
| `color` | `varchar(50)` | | Exterior color |

> Commerce fields (`price`, `quantity`, `status`, `description`, `specs`, `seller_id`, `category_id`) live on `products`.

### 7. parts (detail, TPT)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK, FK → `products(id)` (cascade) | Shared PK |
| `name` | `varchar(200)` | NOT NULL | Part name |
| `brand` | `varchar(100)` | | Part brand |
| `sku` | `varchar(100)` | UNIQUE | Stock keeping unit |

> `product_type = 'Part'`; commerce fields shared via `products`.

### 8. product_images

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `product_id` | `uuid` | FK → `products(id)` (cascade), INDEX, NOT NULL | |
| `url` | `varchar(1024)` | NOT NULL | Image URL |
| `is_primary` | `bool` | NOT NULL, DEFAULT `false` | Main listing image |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 9. reviews

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `product_id` | `uuid` | FK → `products(id)` | (nullable in DB; app always sets) |
| `user_id` | `uuid` | FK → `users(id)`, NOT NULL | Reviewer |
| `rating` | `int` | NOT NULL, CHECK (1-5) | Star rating |
| `comment` | `text` | | Review text |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 10. orders

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `buyer_id` | `uuid` | FK → `users(id)`, NOT NULL | |
| `status` | `varchar(20)` | NOT NULL, DEFAULT `'Pending'` | `Pending`, `Paid`, `Shipped`, `Delivered`, `Cancelled` |
| `total_amount` | `decimal(12,2)` | NOT NULL | Sum of order items |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 11. order_items

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | `uuid` | PK | |
| `order_id` | `uuid` | FK → `orders(id)`, NOT NULL | |
| `product_id` | `uuid` | FK → `products(id)` | (nullable in DB; app always sets) |
| `quantity` | `int` | NOT NULL, DEFAULT 1 | |
| `unit_price` | `decimal(12,2)` | NOT NULL | Price at time of purchase |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 12. favorites

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `user_id` | `uuid` | FK → `users(id)`, PK | |
| `product_id` | `uuid` | FK → `products(id)`, PK | |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

### 13. cart_items

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `user_id` | `uuid` | FK → `users(id)`, PK | |
| `product_id` | `uuid` | FK → `products(id)`, PK | |
| `quantity` | `int` | NOT NULL, DEFAULT 1 | |
| `created_at` | `timestamptz` | NOT NULL | |
| `created_by` | `uuid` | | |
| `updated_at` | `timestamptz` | | |
| `updated_by` | `uuid` | | |

> **Note**: `cart_items` is also cached in Redis with key `cart:{user_id}` (TTL: 1 hour).

## Relationships Diagram

```
users ──< products (seller_id)
users ──< reviews
users ──< orders (buyer_id)
users ──< favorites
users ──< cart_items

brands ──< models
brands ──< cars

models ──< cars

categories ──< products
categories ──< cars
categories ──< parts

products ──< product_images
products ──< reviews
products ──< order_items
products ──< favorites
products ──< cart_items

products ──> cars (1:1, TPT, shared PK)
products ──> parts (1:1, TPT, shared PK)

orders ──< order_items
```

## Kafka Topics

| Topic | Producer | Consumer | Purpose |
|-------|----------|----------|---------|
| `product.listed` | API | API (cache invalidation) | New product listing event |
| `product.updated` | API | API (cache invalidation) | Product change event |
| `inventory.updated` | API | Inventory service | Stock-level change (sale/deduct) event |
| `order.created` | API | Order service, Notification service | New order event |
| `order.paid` | Payment service | Order service, Notification service | Payment confirmation |
| `order.shipped` | Order service | Notification service | Shipping update |
| `scraper.data` | Scraper | API (ingestion) | Scraped car data pipeline |

> `product.listed` / `product.updated` cover both cars and parts (`ProductType` discriminates).

## Scraper Architecture (Playwright)

```
Playwright Scraper (.NET)
  ├── Navigates to external car listing sites (headless Chromium)
  ├── Renders JavaScript-heavy pages
  ├── Extracts: brand, model, year, price, mileage, images, description
  ├── Publishes scraped data → Kafka topic `scraper.data`
  └── API consumer reads `scraper.data` → persists to PostgreSQL → invalidates Redis cache
```

## Cache Strategy (Redis)

| Cache Key Pattern | TTL | Invalidated By |
|-------------------|-----|----------------|
| `product:{id}` | 10 min | `product.updated` / `product.listed` Kafka event |
| `products:list:{filters}` | 10 min | Any product CRUD |
| `user:{id}` | 15 min | User update |
| `session:{token}` | 30 min | Login/logout |
| `cart:{user_id}` | 1 hour | Cart add/remove |

## Suggested File Layout

```
Models/
  User.cs
  Brand.cs
  CarModel.cs
  Category.cs
  Product.cs       (base)
  Car.cs           (Product detail)
  Part.cs          (Product detail)
  ProductImage.cs
  Review.cs
  Order.cs
  OrderItem.cs
  Favorite.cs
  CartItem.cs
  CatalogDtos.cs
  OrderDtos.cs
Services/
  ProductService.cs
  OrderService.cs
  AuthService.cs
  CarService.cs
API/                (Program.cs, DTOs, middleware)
  - Redis caching middleware
  - Kafka producer/consumer integration
Scraper/            (Playwright-based BackgroundService → Kafka producer)
Infrastructure/     (Redis config, Kafka config, EF migrations)
```

## Notes

- `Guid` PKs throughout (matches frontend UUID pattern)
- Scraper uses Playwright's headless Chromium for JS rendering, publishes to Kafka
- API consumes Kafka `scraper.data` topic to persist listings
- Redis sits in front of PostgreSQL for read-heavy product queries
- Audit fields (`created_at`, `created_by`, `updated_at`, `updated_by`) on every entity
- `reviews`/`favorites`/`cart_items`/`order_items`.`product_id` columns are nullable in the DB (the app always sets them) to keep the data-backfill migration simple
