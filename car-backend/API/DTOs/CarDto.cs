namespace API.DTOs;

public record CarResponse(Guid Id, Guid BrandId, Guid ModelId, Guid CategoryId, Guid SellerId, int Year, decimal Price, int? Mileage, string? Color, string? Description, string Status, DateTime CreatedAt);

public record CreateCarRequest(Guid BrandId, Guid ModelId, Guid CategoryId, Guid SellerId, int Year, decimal Price, int? Mileage, string? Color, string? Description, string Status);

public record UpdateCarRequest(Guid Id, Guid BrandId, Guid ModelId, Guid CategoryId, Guid SellerId, int Year, decimal Price, int? Mileage, string? Color, string? Description, string Status);

public record CarDetailResponse(
    Guid Id, Guid? ModelId, int Year, decimal Price, int? Mileage, string? Color,
    string? Description, string Status, DateTime CreatedAt,
    string? BrandName, string? ModelName, string? CategoryName,
    SellerResponse? Seller, List<ProductImageResponse> Images);

public record SellerResponse(Guid Id, string Name, string Email);

public record CarCardResponse(
    Guid Id, string? BrandName, string? ModelName, int Year, decimal Price,
    int? Mileage, string? Color, string? PrimaryImageUrl);