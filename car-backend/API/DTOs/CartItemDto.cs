namespace API.DTOs;

public record CartItemResponse(Guid UserId, Guid ProductId, int Quantity, DateTime CreatedAt);

public record CreateCartItemRequest(Guid UserId, Guid ProductId, int Quantity);

public record UpdateCartItemRequest(Guid UserId, Guid ProductId, int Quantity);
