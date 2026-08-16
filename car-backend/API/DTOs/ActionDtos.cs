namespace API.DTOs;

public record AddFavoriteRequest(Guid ProductId);

public record AddCartItemRequest(Guid ProductId, int Quantity);
