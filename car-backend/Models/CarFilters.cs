namespace Models;

public record CarFilterBrand(Guid Id, string Name);

public record CarFilters(
    List<CarFilterBrand> Brands,
    List<string> Colors,
    List<string> Statuses,
    decimal MinPrice,
    decimal MaxPrice,
    int MinMileage,
    int MaxMileage,
    int MinYear,
    int MaxYear);
