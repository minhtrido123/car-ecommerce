namespace Services;

public class InsufficientStockException : Exception
{
    public Guid ProductId { get; }

    public InsufficientStockException(Guid productId)
        : base($"Insufficient stock or inactive product: {productId}")
    {
        ProductId = productId;
    }
}
