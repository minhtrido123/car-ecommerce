using Infrastructure;
using Models;
using System.Linq.Expressions;

namespace Repositories;

public class CachedRepository<T> : ICachedRepository<T> where T : EntityBase
{
    private readonly IRepository<T> _inner;
    private readonly Lazy<RedisCacheService> _cache;
    private readonly Lazy<IKafkaProducer> _kafkaProducer;

    public CachedRepository(IRepository<T> inner, Lazy<RedisCacheService> cache, Lazy<IKafkaProducer> kafkaProducer)
    {
        _inner = inner;
        _cache = cache;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"entity:{typeof(T).Name}:{id}";
        if (_cache.Value is not null)
        {

            var cached = await _cache.Value.GetAsync<T>(cacheKey, cancellationToken);
            if (cached is not null) return cached;
        }
        var entity = await _inner.GetByIdAsync(id, cancellationToken);
        if (entity is not null && _cache.Value is not null)
        {
            await _cache.Value.SetAsync(cacheKey, entity, cancellationToken: cancellationToken);
        }
        return entity;
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"entity:{typeof(T).Name}:all";

        try
        {
            var cached = await _cache.Value.GetAsync<List<T>>(cacheKey, cancellationToken);
            if (cached is not null) return cached;
        }
        catch { }
        var entities = await _inner.GetAllAsync(cancellationToken);
        try
        {
            await _cache.Value.SetListAsync(cacheKey, entities, cancellationToken: cancellationToken);
        }
        catch { }
        return entities;
    }

    public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default)
    {
        var includesKey = includes != null && includes.Any()
            ? ":" + string.Join(",", includes.OrderBy(x => x))
            : "";
        var cacheKey = $"entity:{typeof(T).Name}:page:{pageNumber}:{pageSize}{includesKey}";
        try
        {
            var cached = await _cache.Value.GetAsync<PagedResult<T>>(cacheKey, cancellationToken);
            if (cached is not null) return cached;
        }
        catch { }
        var result = await _inner.GetPagedAsync(pageNumber, pageSize, includes, cancellationToken);
        try
        {
            await _cache.Value.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
        }
        catch { }
        return result;
    }

    public async Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _inner.GetFilteredAsync(predicate, cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddAsync(entity, cancellationToken);
        try
        {
            await _kafkaProducer.Value.PublishInvalidationAsync(typeof(T).Name, result.Id, "created", cancellationToken);
        }
        catch { }
        return result;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(entity, cancellationToken);
        try
        {
            await _kafkaProducer.Value.PublishInvalidationAsync(typeof(T).Name, result.Id, "updated", cancellationToken);
        }
        catch { }
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(id, cancellationToken);
        try
        {
            await _kafkaProducer.Value.PublishInvalidationAsync(typeof(T).Name, id, "deleted", cancellationToken);
        }
        catch { }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _inner.ExistsAsync(id, cancellationToken);
    }
}
