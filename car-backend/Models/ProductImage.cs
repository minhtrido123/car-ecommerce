using System.Text.Json.Serialization;

namespace Models;

public class ProductImage : EntityBase
{
    public Guid ProductId { get; set; }
    public required string Url { get; set; }
    public bool IsPrimary { get; set; }
    public int Position { get; set; }
    [JsonIgnore]
    public Product Product { get; set; } = null!;
}
