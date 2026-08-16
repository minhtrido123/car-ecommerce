using Models;

namespace Services;

public static class ProductMapping
{
    public static string DetailName(Product product) => product switch
    {
        Car car => string.Join(" ", new[] { car.Year.ToString(), car.Brand?.Name, car.Model?.Name }
            .Where(s => !string.IsNullOrWhiteSpace(s))),
        Part part => part.Name,
        _ => string.Empty
    };

    public static string? PrimaryImageUrl(Product product) =>
        product.ProductImages.FirstOrDefault(i => i.IsPrimary)?.Url
        ?? product.ProductImages.FirstOrDefault()?.Url;

    public static ProductResponse ToResponse(Product product) => new(
        product.Id, product.ProductType, product.CategoryId, product.Category?.Name,
        product.Price, product.Quantity, product.Status,
        PrimaryImageUrl(product), DetailName(product), product.CreatedAt);

    public static ProductDetailResponse ToDetail(Product product, Car? car, Part? part) => new(
        product.Id, product.ProductType, product.CategoryId, product.Category?.Name, product.SellerId,
        product.Price, product.Quantity, product.Status, product.Description, product.Specs,
        car?.Year, car?.Brand?.Name, car?.Model?.Name, car?.Mileage, car?.Color,
        part?.Name, part?.Brand, part?.Sku,
        product.CreatedAt,
        product.ProductImages
            .OrderByDescending(i => i.IsPrimary)
            .Select(i => new ProductImageResponse(i.Id, i.ProductId, i.Url, i.IsPrimary, i.CreatedAt))
            .ToList());

    public static PartResponse ToPartResponse(Part part) => new(
        part.Id, part.Name, part.Brand, part.Sku, part.CategoryId, part.Category?.Name,
        part.Price, part.Quantity, part.Status, part.Description, part.Specs,
        PrimaryImageUrl(part), part.CreatedAt);
}
