# Caching Strategy

## Overview

This backend uses a two-tier caching strategy:

1. **Redis** — distributed cache for frequently read data
2. **Kafka** — event-driven cache invalidation across workers

## Cache Pattern

### Cache-Aside (Lazy Loading)

Reads follow this path:

```
Request → CachedRepository → Redis
         (miss)              ↓
         (miss)         PostgreSQL
         (hit)           ↓
                      Return to caller
                      Set Redis key
```

### Invalidation Strategy

**Drop-all per entity type on write.** When any create/update/delete operation occurs for an entity type, all cached keys for that entity type are dropped.

Rationale:
- Writes are infrequent relative to reads in this domain
- Simpler than version-based or targeted invalidation
- Short TTL (5-10 min) acts as a safety net for missed invalidations

## Cache Key Convention

| Key Pattern | Example | TTL |
|-------------|---------|-----|
| Single entity | `entity:User:{id}` | 10 min |
| Paged list | `entity:Car:page:1:20` | 5 min |
| All entity type | `entity:Car:*` (for invalidation) | — |

## Architecture

```
┌─────────┐     ┌──────────────────┐     ┌──────────┐
│  API     │────▶│ CachedRepository │────▶│ Redis    │
│          │     │                  │     │          │
│          │     │  (cache-aside)   │     └──────────┘
│          │     └────────┬─────────┘
│          │              │ miss
│          │              ▼
│          │     ┌──────────────────┐     ┌──────────┐
│          │     │   EfRepository   │────▶│PostgreSQL│
│          │     └────────┬─────────┘     └──────────┘
│          │              │ write
│          │              ▼
│          │     ┌──────────────────┐
│          │     │  KafkaProducer   │────▶ Kafka topic
│          │     └──────────────────┘
│          │              │
│          │              ▼
│          │     ┌──────────────────┐
│          │     │   CacheWorker    │────▶ Redis (invalidate)
│          │     └──────────────────┘
└─────────┘
```

## Components

### RedisCacheService

Manages Redis connection and key operations. Provides:
- `GetAsync<T>(key)` — deserialize from Redis
- `SetAsync(key, value, ttl)` — serialize and store
- `DeleteByPatternAsync(pattern)` — delete keys matching pattern
- `ExistsAsync(key)` — check if key exists

### CachedRepository<T>

Wraps `EfRepository<T>` with Redis caching:
- `GetByIdAsync` — check Redis, miss → DB → cache
- `GetAllAsync` — check Redis, miss → DB → cache
- `GetPagedAsync` — check Redis, miss → DB → cache
- `AddAsync` — write DB → publish Kafka event → return
- `UpdateAsync` — write DB → publish Kafka event → return
- `DeleteAsync` — write DB → publish Kafka event → return

### KafkaProducer

Publishes cache invalidation events to Kafka:
- Topic: `cache.invalidation`
- Event payload: `{ "entityType": "Car", "entityId": "uuid", "action": "updated" }`

### CacheWorker

Background service that:
1. Consumes messages from `cache.invalidation` topic
2. Drops all Redis keys matching `entity:{entityType}:*`
3. Logs invalidation events for debugging

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `REDIS_CONNECTION_STRING` | `localhost:6379` | Redis connection |
| `KAFKA_BROKERS` | `localhost:9092` | Kafka bootstrap servers |
| `KAFKA_INVALIDATION_TOPIC` | `cache.invalidation` | Invalidation topic |

### appsettings.json

```json
{
  "Cache": {
    "RedisConnectionString": "localhost:6379",
    "DefaultTtlSeconds": 600,
    "ListTtlSeconds": 300
  },
  "Kafka": {
    "Brokers": "localhost:9092",
    "InvalidationTopic": "cache.invalidation",
    "GroupId": "cache-worker"
  }
}
```

## Adding Cache to a New Entity

1. Entity extends `EntityBase` in Models project
2. `CachedRepository<T>` works automatically for any `EntityBase` subtype
3. Register `ICachedRepository<T>` in DI (see `ServiceDefaultsExtensions`)
4. No additional configuration needed

## Testing

Tests use `Microsoft.EntityFrameworkCore.InMemory` for the database layer.
Redis and Kafka are not required for unit tests — the caching layer is tested
separately with integration tests.