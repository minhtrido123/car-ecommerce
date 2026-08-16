using Infrastructure;
using Models;
using System.Linq.Expressions;

namespace Repositories;

public class CachedRepository<T> : ICachedRepository<T> where T : EntityBase
{
    private readonly IRepository<T> _inner;
    private readonly RedisCacheService _cache;
    private readonly IKafkaProducer _kafkaProducer;

    public CachedRepository(IRepository<T> inner, RedisCacheService cache, IKafkaProducer kafkaProducer)
    {
        _inner = inner;
        _cache = cache;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"entity:{typeof(T).Name}:{id}";
        var cached = await _cache.GetAsync<T>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var entity = await _inner.GetByIdAsync(id, cancellationToken);
        if (entity is not null)
        {
            await _cache.SetAsync(cacheKey, entity, cancellationToken: cancellationToken);
        }
        return entity;
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"entity:{typeof(T).Name}:all";
        var cached = await _cache.GetAsync<List<T>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var entities = await _inner.GetAllAsync(cancellationToken);
        await _cache.SetListAsync(cacheKey, entities, cancellationToken: cancellationToken);
        return entities;
    }

    public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default)
    {
        var includesKey = includes != null && includes.Any()
            ? ":" + string.Join(",", includes.OrderBy(x => x))
            : "";
        var cacheKey = $"entity:{typeof(T).Name}:page:{pageNumber}:{pageSize}{includesKey}";
        var cached = await _cache.GetAsync<PagedResult<T>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var result = await _inner.GetPagedAsync(pageNumber, pageSize, includes, cancellationToken);
        await _cache.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
        return result;
    }

    public async Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _inner.GetFilteredAsync(predicate, cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddAsync(entity, cancellationToken);
        await _kafkaProducer.PublishInvalidationAsync(typeof(T).Name, result.Id, "created", cancellationToken);
        return result;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(entity, cancellationToken);
        await _kafkaProducer.PublishInvalidationAsync(typeof(T).Name, result.Id, "updated", cancellationToken);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(id, cancellationToken);
        await _kafkaProducer.PublishInvalidationAsync(typeof(T).Name, id, "deleted", cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _inner.ExistsAsync(id, cancellationToken);
    }
}
