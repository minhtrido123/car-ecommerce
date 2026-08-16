namespace API.DTOs;

public record OrderResponse(Guid Id, Guid BuyerId, string Status, decimal TotalAmount, DateTime CreatedAt);

public record CreateOrderRequest(Guid BuyerId, string Status, decimal TotalAmount);

public record UpdateOrderRequest(Guid Id, string Status, decimal TotalAmount);