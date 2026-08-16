using Infrastructure;
using Models;
using Services;

namespace Tests;

public class CarServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly CarService _carService;

    public CarServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _carService = new CarService(_db);
    }

    private async Task<(Guid brandId, Guid modelId, Guid categoryId, Guid sellerId)> SeedCatalogAsync()
    {
        var brand = new Brand { Name = "Toyota", Country = "Japan" };
        await _db.Brands.AddAsync(brand);
        await _db.SaveChangesAsync();
        var model = new CarModel { BrandId = brand.Id, Name = "Camry" };
        await _db.CarModels.AddAsync(model);
        await _db.SaveChangesAsync();
        var category = new Category { Name = "Sedan", Slug = "sedan" };
        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();
        var seller = new User { Email = "seller@test.com", PasswordHash = "hash", Role = "Seller", Name = "Dealer" };
        await _db.Users.AddAsync(seller);
        await _db.SaveChangesAsync();
        return (brand.Id, model.Id, category.Id, seller.Id);
    }

    private Car MakeCar(Guid brandId, Guid modelId, Guid categoryId, Guid sellerId, int year, decimal price, string status = "Active") =>
        new()
        {
            BrandId = brandId, ModelId = modelId, CategoryId = categoryId, SellerId = sellerId,
            Year = year, Price = price, Mileage = 1000, Color = "Red", Status = status
        };

    [Fact]
    public async Task GetDetailAsync_ReturnsCar_WithNavigationsLoaded()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        await _db.ProductImages.AddAsync(new ProductImage { ProductId = car.Id, Url = "/img.jpg", IsPrimary = true });
        await _db.SaveChangesAsync();

        var result = await _carService.GetDetailAsync(car.Id);

        Assert.NotNull(result);
        Assert.Equal("Toyota", result!.Brand!.Name);
        Assert.Equal("Camry", result.Model!.Name);
        Assert.Equal("Sedan", result.Category!.Name);
        Assert.Equal("Dealer", result.Seller!.Name);
        Assert.Single(result.ProductImages);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNull_WhenMissing()
    {
        var result = await _carService.GetDetailAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSimilarAsync_ReturnsSameModelExcludingSelf_OrderedByYearDesc()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var c1 = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 20000);
        var c2 = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 25000);
        var c3 = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 22000);
        await _db.Cars.AddRangeAsync(c1, c2, c3);
        await _db.SaveChangesAsync();

        var result = await _carService.GetSimilarAsync(modelId, c1.Id, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(c2.Id, result[0].Id);
        Assert.Equal(c3.Id, result[1].Id);
    }

    [Fact]
    public async Task GetSimilarAsync_ExcludesNonActiveCars()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var active = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 25000);
        var sold = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000, "Sold");
        await _db.Cars.AddRangeAsync(active, sold);
        await _db.SaveChangesAsync();

        var result = await _carService.GetSimilarAsync(modelId, null, 10);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    [Fact]
    public async Task SearchCarsAsync_FiltersByBrandModelOrDescription()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var byBrand = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000);
        byBrand.Description = "Reliable family sedan";
        var byModel = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 25000);
        byModel.Description = "Sport package";
        var byDescription = MakeCar(brandId, modelId, categoryId, sellerId, 2023, 30000);
        byDescription.Description = "Sunroof and leather seats";
        var other = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 15000);
        other.Description = "Base trim";
        await _db.Cars.AddRangeAsync(byBrand, byModel, byDescription, other);
        await _db.SaveChangesAsync();

        var byToyota = await _carService.SearchCarsAsync("toyota", null, null, 1, 20);
        var byCamry = await _carService.SearchCarsAsync("camry", null, null, 1, 20);
        var bySunroof = await _carService.SearchCarsAsync("sunroof", null, null, 1, 20);
        var noMatch = await _carService.SearchCarsAsync("nonexistent", null, null, 1, 20);

        Assert.Equal(4, byToyota.TotalCount);
        Assert.Equal(4, byCamry.TotalCount);
        Assert.Single(bySunroof.Items);
        Assert.Equal(byDescription.Id, bySunroof.Items[0].Id);
        Assert.Empty(noMatch.Items);
    }

    [Fact]
    public async Task SearchCarsAsync_SortsByPriceAscAndDesc()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var cheap = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 10000);
        var mid = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000);
        var expensive = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 30000);
        await _db.Cars.AddRangeAsync(cheap, mid, expensive);
        await _db.SaveChangesAsync();

        var asc = await _carService.SearchCarsAsync(null, "price", "asc", 1, 20);
        var desc = await _carService.SearchCarsAsync(null, "price", "desc", 1, 20);

        Assert.Equal(cheap.Id, asc.Items[0].Id);
        Assert.Equal(expensive.Id, asc.Items[2].Id);
        Assert.Equal(expensive.Id, desc.Items[0].Id);
        Assert.Equal(cheap.Id, desc.Items[2].Id);
    }

    [Fact]
    public async Task SearchCarsAsync_DefaultSortsByCreatedAtDesc()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var older = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 10000);
        older.CreatedAt = new DateTime(2024, 1, 1);
        var newer = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 30000);
        newer.CreatedAt = new DateTime(2025, 1, 1);
        await _db.Cars.AddRangeAsync(older, newer);
        await _db.SaveChangesAsync();

        var result = await _carService.SearchCarsAsync(null, null, null, 1, 20);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task SearchCarsAsync_PaginatesWithTotalCount()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        for (var i = 0; i < 5; i++)
        {
            await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2020 + i, 10000 + i * 1000));
        }
        await _db.SaveChangesAsync();

        var page1 = await _carService.SearchCarsAsync(null, "price", "asc", 1, 2);
        var page2 = await _carService.SearchCarsAsync(null, "price", "asc", 2, 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(1, page1.PageNumber);
        Assert.Equal(2, page1.PageSize);
        Assert.True(page1.Items[0].Price < page1.Items[1].Price);
    }

    [Fact]
    public async Task SearchCarsAsync_FiltersByPriceRange()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2020, 10000));
        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000));
        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2022, 30000));
        await _db.SaveChangesAsync();

        var result = await _carService.SearchCarsAsync(null, null, null, 1, 20, minPrice: 15000, maxPrice: 25000);

        Assert.Single(result.Items);
        Assert.Equal(20000, result.Items[0].Price);
    }

    [Fact]
    public async Task SearchCarsAsync_FiltersByMileageRange()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var low = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 10000);
        low.Mileage = 10000;
        var mid = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000);
        mid.Mileage = 50000;
        var high = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 30000);
        high.Mileage = 90000;
        await _db.Cars.AddRangeAsync(low, mid, high);
        await _db.SaveChangesAsync();

        var result = await _carService.SearchCarsAsync(null, null, null, 1, 20, minMileage: 20000, maxMileage: 60000);

        Assert.Single(result.Items);
        Assert.Equal(mid.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task SearchCarsAsync_FiltersByYearRange()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2018, 10000));
        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2020, 20000));
        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2022, 30000));
        await _db.SaveChangesAsync();

        var result = await _carService.SearchCarsAsync(null, null, null, 1, 20, minYear: 2019, maxYear: 2021);

        Assert.Single(result.Items);
        Assert.Equal(2020, result.Items[0].Year);
    }

    [Fact]
    public async Task SearchCarsAsync_FiltersByColorAndStatus()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var redActive = MakeCar(brandId, modelId, categoryId, sellerId, 2020, 10000);
        var redSold = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 20000, "Sold");
        var blueActive = MakeCar(brandId, modelId, categoryId, sellerId, 2022, 30000);
        blueActive.Color = "Blue";
        await _db.Cars.AddRangeAsync(redActive, redSold, blueActive);
        await _db.SaveChangesAsync();

        var red = await _carService.SearchCarsAsync(null, null, null, 1, 20, color: "red");
        var active = await _carService.SearchCarsAsync(null, null, null, 1, 20, status: "Active");
        var redActiveResult = await _carService.SearchCarsAsync(null, null, null, 1, 20, color: "red", status: "Active");

        Assert.Equal(2, red.TotalCount);
        Assert.Equal(2, active.TotalCount);
        Assert.Single(redActiveResult.Items);
        Assert.Equal(redActive.Id, redActiveResult.Items[0].Id);
    }

    [Fact]
    public async Task SearchCarsAsync_FiltersByBrandIds()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var fordBrand = new Brand { Name = "Ford", Country = "USA" };
        await _db.Brands.AddAsync(fordBrand);
        await _db.SaveChangesAsync();
        var fordModel = new CarModel { BrandId = fordBrand.Id, Name = "Focus" };
        await _db.CarModels.AddAsync(fordModel);
        await _db.SaveChangesAsync();

        await _db.Cars.AddAsync(MakeCar(brandId, modelId, categoryId, sellerId, 2020, 10000));
        await _db.Cars.AddAsync(MakeCar(fordBrand.Id, fordModel.Id, categoryId, sellerId, 2021, 20000));
        await _db.SaveChangesAsync();

        var result = await _carService.SearchCarsAsync(null, null, null, 1, 20, brandIds: [fordBrand.Id.ToString()]);

        Assert.Single(result.Items);
        Assert.Equal(fordBrand.Id, result.Items[0].BrandId);
    }

    [Fact]
    public async Task SearchCarsAsync_ReturnsProductImagesOrderedByPosition()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var car = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 25000);
        await _db.Cars.AddAsync(car);
        await _db.SaveChangesAsync();
        await _db.ProductImages.AddRangeAsync(
            new ProductImage { ProductId = car.Id, Url = "/c.jpg", Position = 2 },
            new ProductImage { ProductId = car.Id, Url = "/a.jpg", Position = 0 },
            new ProductImage { ProductId = car.Id, Url = "/b.jpg", Position = 1 });
        await _db.SaveChangesAsync();

        var result = await _carService.SearchCarsAsync(null, null, null, 1, 20);

        Assert.Single(result.Items);
        var urls = result.Items[0].ProductImages.Select(i => i.Url).ToArray();
        Assert.Equal(new[] { "/a.jpg", "/b.jpg", "/c.jpg" }, urls);
    }

    [Fact]
    public async Task GetFiltersAsync_ReturnsDistinctValuesAndBounds()
    {
        var (brandId, modelId, categoryId, sellerId) = await SeedCatalogAsync();
        var fordBrand = new Brand { Name = "Ford", Country = "USA" };
        await _db.Brands.AddAsync(fordBrand);
        await _db.SaveChangesAsync();
        var fordModel = new CarModel { BrandId = fordBrand.Id, Name = "F150" };
        await _db.CarModels.AddAsync(fordModel);
        await _db.SaveChangesAsync();

        var a = MakeCar(brandId, modelId, categoryId, sellerId, 2019, 5000);
        a.Mileage = 10000;
        a.Color = "Red";
        var b = MakeCar(brandId, modelId, categoryId, sellerId, 2021, 30000);
        b.Mileage = 50000;
        b.Color = "Blue";
        var c = MakeCar(fordBrand.Id, fordModel.Id, categoryId, sellerId, 2023, 40000);
        c.Mileage = 70000;
        c.Color = "Red";
        await _db.Cars.AddRangeAsync(a, b, c);
        await _db.SaveChangesAsync();

        var filters = await _carService.GetFiltersAsync();

        Assert.Equal(new[] { "Ford", "Toyota" }, filters.Brands.Select(x => x.Name).ToArray());
        Assert.Equal(new[] { "Blue", "Red" }, filters.Colors.ToArray());
        Assert.Equal(new[] { "Active" }, filters.Statuses.ToArray());
        Assert.Equal(5000, filters.MinPrice);
        Assert.Equal(40000, filters.MaxPrice);
        Assert.Equal(10000, filters.MinMileage);
        Assert.Equal(70000, filters.MaxMileage);
        Assert.Equal(2019, filters.MinYear);
        Assert.Equal(2023, filters.MaxYear);
    }
}
