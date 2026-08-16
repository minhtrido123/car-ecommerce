namespace API.DTOs;

public record FavoriteResponse(Guid UserId, Guid ProductId, DateTime CreatedAt);

public record CreateFavoriteRequest(Guid UserId, Guid ProductId);
