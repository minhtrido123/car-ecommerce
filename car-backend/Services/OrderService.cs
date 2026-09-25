using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services;

public interface IOrderService
{
    Task<Order> CreateAsync(Guid buyerId, CreateOrderRequest request, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly SorchaDbContext _db;
    private readonly Lazy<IKafkaProducer> _kafkaProducer;

    public OrderService(SorchaDbContext db, Lazy<IKafkaProducer> kafkaProducer)
    {
        _db = db;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<Order> CreateAsync(Guid buyerId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("Order must contain at least one item");

        var order = new Order { Id = Guid.NewGuid(), BuyerId = buyerId, Status = "Pending" };
        decimal total = 0;
        var items = new List<OrderItem>();

        foreach (var input in request.Items)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == input.ProductId, cancellationToken)
                ?? throw new InsufficientStockException(input.ProductId);

            if (input.Quantity <= 0)
                throw new ArgumentException("Quantity must be at least 1");

            if (product.Status != "Active" || product.Quantity < input.Quantity)
                throw new InsufficientStockException(input.ProductId);

            product.Quantity -= input.Quantity;
            if (product.ProductType == "Car")
                product.Status = "Sold";
            else if (product.Quantity == 0)
                product.Status = "Inactive";

            var unitPrice = product.Price;
            total += unitPrice * input.Quantity;
            items.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, Quantity = input.Quantity, UnitPrice = unitPrice });
        }

        order.TotalAmount = total;
        _db.Orders.Add(order);
        _db.OrderItems.AddRange(items);
        await _db.SaveChangesAsync(cancellationToken);

        if (_kafkaProducer.Value is null) return order;
        try
        {
            foreach (var item in items)
            {
                await _kafkaProducer.Value.PublishInvalidationAsync("Product", item.ProductId, "updated", cancellationToken);
                await _kafkaProducer.Value.PublishInventoryAsync(item.ProductId, item.Quantity, "sold", cancellationToken);
            }
            await _kafkaProducer.Value.PublishInvalidationAsync("Order", order.Id, "created", cancellationToken);
        }
        catch { }

        return order;
    }
}
