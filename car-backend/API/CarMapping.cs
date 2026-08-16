using API.DTOs;
using Models;

namespace API;

public static class CarMapping
{
    public static CarDetailResponse ToDetail(Car car) => new(
        car.Id, car.ModelId, car.Year, car.Price, car.Mileage, car.Color, car.Description,
        car.Status, car.CreatedAt,
        car.Brand?.Name, car.Model?.Name, car.Category?.Name,
        car.Seller is null ? null : new SellerResponse(car.Seller.Id, car.Seller.Name, car.Seller.Email),
        car.ProductImages
            .OrderByDescending(i => i.IsPrimary)
            .Select(i => new ProductImageResponse(i.Id, i.ProductId, i.Url, i.IsPrimary, i.CreatedAt))
            .ToList());

    public static CarCardResponse ToCard(Car car) => new(
        car.Id, car.Brand?.Name, car.Model?.Name, car.Year, car.Price, car.Mileage, car.Color,
        car.ProductImages.FirstOrDefault(i => i.IsPrimary)?.Url ?? car.ProductImages.FirstOrDefault()?.Url);
}
