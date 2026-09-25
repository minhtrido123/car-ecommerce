using System.Text;
using System.Text.Json;

namespace Infrastructure;

public interface IKafkaConsumer
{
    Task ConsumeInvalidationAsync(CancellationToken cancellationToken = default);
    Task ConsumeInventoryAsync(CancellationToken cancellationToken = default);
}
