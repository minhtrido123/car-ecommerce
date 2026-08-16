namespace Models;

public record OrderItemInput(Guid ProductId, int Quantity);

public record CreateOrderRequest(List<OrderItemInput> Items);
