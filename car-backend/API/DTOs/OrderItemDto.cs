namespace API.DTOs;

public record OrderItemResponse(Guid Id, Guid OrderId, Guid ProductId, int Quantity, decimal UnitPrice, DateTime CreatedAt);

public record CreateOrderItemRequest(Guid OrderId, Guid ProductId, int Quantity, decimal UnitPrice);

public record UpdateOrderItemRequest(Guid Id, Guid OrderId, Guid ProductId, int Quantity, decimal UnitPrice);
