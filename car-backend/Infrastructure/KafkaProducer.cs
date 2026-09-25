using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Infrastructure;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _invalidationTopic;
    private readonly string _inventoryTopic;
    private readonly ILogger<KafkaProducer> _logger;


    public KafkaProducer(string brokers, string invalidationTopic, string inventoryTopic, ILogger<KafkaProducer> logger)
    {
        _invalidationTopic = invalidationTopic;
        _inventoryTopic = inventoryTopic;
        _logger = logger;
        var config = new ProducerConfig { BootstrapServers = brokers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishInvalidationAsync(string entityType, Guid entityId, string action, CancellationToken cancellationToken = default)
    {
        var message = new CacheInvalidationEvent(entityType, entityId, action, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(message);
        _logger.LogInformation("Kafka publish {Topic}: entity={EntityType} id={EntityId} action={Action}", _invalidationTopic, entityType, entityId, action);
        await _producer.ProduceAsync(_invalidationTopic, new Message<Null, string> { Value = json }, cancellationToken);
    }

    public async Task PublishInventoryAsync(Guid productId, int quantity, string action, CancellationToken cancellationToken = default)
    {
        var message = new InventoryEvent(productId, quantity, action, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(message);
        _logger.LogInformation("Kafka publish {Topic}: product={ProductId} qty={Quantity} action={Action}", _inventoryTopic, productId, quantity, action);
        await _producer.ProduceAsync(_inventoryTopic, new Message<Null, string> { Value = json }, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}

public record CacheInvalidationEvent(string EntityType, Guid EntityId, string Action, DateTime Timestamp);

public record InventoryEvent(Guid ProductId, int Quantity, string Action, DateTime Timestamp);
