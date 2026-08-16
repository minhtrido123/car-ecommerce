namespace API.DTOs;

public record BrandResponse(Guid Id, string Name, string? Country, DateTime CreatedAt);

public record CreateBrandRequest(string Name, string? Country);

public record UpdateBrandRequest(Guid Id, string Name, string? Country);