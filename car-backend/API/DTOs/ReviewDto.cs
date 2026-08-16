namespace API.DTOs;

public record ReviewResponse(Guid Id, Guid ProductId, Guid UserId, int Rating, string? Comment, DateTime CreatedAt);

public record CreateReviewRequest(Guid ProductId, Guid UserId, int Rating, string? Comment);

public record UpdateReviewRequest(Guid Id, Guid ProductId, Guid UserId, int Rating, string? Comment);
