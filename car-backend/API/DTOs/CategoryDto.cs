namespace API.DTOs;

public record CategoryResponse(Guid Id, string Name, string Slug, DateTime CreatedAt);

public record CreateCategoryRequest(string Name, string Slug);

public record UpdateCategoryRequest(Guid Id, string Name, string Slug);