namespace Models;

public record ProductImageResponse(Guid Id, Guid ProductId, string Url, bool IsPrimary, DateTime CreatedAt);

public record ProductResponse(
    Guid Id, string ProductType, Guid? CategoryId, string? CategoryName,
    decimal Price, int Quantity, string Status, string? PrimaryImageUrl, string? DetailName, DateTime CreatedAt);

public record ProductDetailResponse(
    Guid Id, string ProductType, Guid? CategoryId, string? CategoryName, Guid? SellerId,
    decimal Price, int Quantity, string Status, string? Description, string? Specs,
    int? Year, string? BrandName, string? ModelName, int? Mileage, string? Color,
    string? Name, string? PartBrand, string? Sku,
    DateTime CreatedAt, List<ProductImageResponse> Images);

public record PartResponse(
    Guid Id, string Name, string? Brand, string? Sku, Guid? CategoryId, string? CategoryName,
    decimal Price, int Quantity, string Status, string? Description, string? Specs,
    string? PrimaryImageUrl, DateTime CreatedAt);
