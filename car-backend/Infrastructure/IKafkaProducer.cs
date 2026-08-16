using System.Text;
using System.Text.Json;

namespace Infrastructure;

public interface IKafkaProducer
{
    Task PublishInvalidationAsync(string entityType, Guid entityId, string action, CancellationToken cancellationToken = default);
    Task PublishInventoryAsync(Guid productId, int quantity, string action, CancellationToken cancellationToken = default);
}