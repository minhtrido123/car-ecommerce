using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public class KafkaConsumer : IKafkaConsumer
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly Lazy<ICacheService> _cache;
    private readonly ILogger<KafkaConsumer> _logger;
    public KafkaConsumer(IConfiguration configuration, Lazy<ICacheService> cache, ILogger<KafkaConsumer> logger)
    {
        _cache = cache;
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<Null, string>(config)
            .Build();

        _consumer.Subscribe(new[]
        {
            configuration["Kafka:InvalidationTopic"],
            configuration["Kafka:InventoryTopic"]
        });
        _logger = logger;
    }

    public async Task ConsumeInvalidationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var message = _consumer.Consume(cancellationToken);

            var invalidationEvent = JsonSerializer.Deserialize<CacheInvalidationEvent>(message.Message.Value);
            var entityType = invalidationEvent!.EntityType;
            var entityId = invalidationEvent.EntityId;

            await _cache.Value.DeleteAsync($"entity:{entityType}:{entityId}", cancellationToken);
            await _cache.Value.DeleteByPatternAsync($"entity:{entityType}:*", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache invalidation failed, check Kafka and Redis is running");
        }
        finally
        {
            _consumer.Commit();
            _consumer.Close();
        }
    }
    public async Task ConsumeInventoryAsync(
        CancellationToken cancellationToken = default)
    {
        // business logic
        await Task.CompletedTask;
    }
}
