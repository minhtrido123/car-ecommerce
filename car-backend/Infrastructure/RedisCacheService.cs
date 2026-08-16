using System.Text.Json;
using StackExchange.Redis;

namespace Infrastructure;

public class RedisCacheService : ICacheService, IDisposable
{
    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _db;
    private readonly TimeSpan _defaultTtl;
    private readonly TimeSpan _listTtl;

    public RedisCacheService(string connectionString, TimeSpan? defaultTtl = null, TimeSpan? listTtl = null)
    {
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _db = _connection.GetDatabase();
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(10);
        _listTtl = listTtl ?? TimeSpan.FromMinutes(5);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Get From Cache: " + key);
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl ?? _defaultTtl);
    }

    public async Task SetListAsync<T>(string key, List<T> value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl ?? _listTtl);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task DeleteByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var server = _connection.GetServer(_connection.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern);
        if (keys.Any())
        {
            await _db.KeyDeleteAsync(keys.ToArray());
        }
    }

    public async Task InvalidateEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"entity:{entityType}:{entityId}", cancellationToken);
        await DeleteByPatternAsync($"entity:{entityType}:page:*", cancellationToken);
    }

    public async Task InvalidateAllAsync(string entityType, CancellationToken cancellationToken = default)
    {
        await DeleteByPatternAsync($"entity:{entityType}:*", cancellationToken);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
