using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure;

public class CacheWorker : BackgroundService, ICacheWorker
{
    private readonly RedisCacheService _cache;
    private readonly IConsumer<Null, string> _consumer;
    private readonly ILogger<CacheWorker> _logger;

    public CacheWorker(RedisCacheService cache, string brokers, string topic, string groupId, ILogger<CacheWorker> logger)
    {
        _cache = cache;
        _logger = logger;
        var config = new ConsumerConfig
        {
            BootstrapServers = brokers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };
        _consumer = new ConsumerBuilder<Null, string>(config).Build();
        _consumer.Subscribe(topic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result.Message.Value is not null)
                {
                    await HandleInvalidationAsync(result.Message.Value, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CacheWorker error: {ex.Message}");
            }
        }
    }

    private async Task HandleInvalidationAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var evt = System.Text.Json.JsonSerializer.Deserialize<CacheInvalidationEvent>(message);
            if (evt is null) return;

            _logger.LogInformation("Kafka consume: entity={EntityType} id={EntityId} action={Action}", evt.EntityType, evt.EntityId, evt.Action);

            if (evt.EntityType == "Product")
            {
                await _cache.InvalidateEntityAsync("Product", evt.EntityId, cancellationToken);
                await _cache.InvalidateEntityAsync("Car", evt.EntityId, cancellationToken);
                await _cache.InvalidateEntityAsync("Part", evt.EntityId, cancellationToken);
                await _cache.DeleteAsync($"product:{evt.EntityId}", cancellationToken);
                await _cache.DeleteByPatternAsync("products:list:*", cancellationToken);
            }
            else
            {
                await _cache.InvalidateEntityAsync(evt.EntityType, evt.EntityId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to handle invalidation: {ex.Message}");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer.Close();
        _consumer.Dispose();
        await base.StopAsync(cancellationToken);
    }
}