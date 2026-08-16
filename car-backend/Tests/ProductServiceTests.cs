using Infrastructure;
using Models;
using Repositories;
using Services;

namespace Tests;

public class ProductServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _service = new ProductService(_db, new StubCacheService(), new StubKafkaProducer());
    }

    private async Task<(Guid categoryId, Guid sellerId)> SeedBaseAsync()
    {
        var category = new Category { Name = "Battery", Slug = "battery", Type = "Part" };
        var carCategory = new Category { Name = "Sedan", Slug = "sedan", Type = "Car" };
        await _db.Categories.AddRangeAsync(category, carCategory);
        await _db.SaveChangesAsync();
        var seller = new User { Email = "seller@test.com", PasswordHash = "hash", Role = "Seller", Name = "Dealer" };
        await _db.Users.AddAsync(seller);
        await _db.SaveChangesAsync();
        return (category.Id, seller.Id);
    }

    private Part MakePart(Guid categoryId, Guid sellerId, string sku, decimal price, int quantity, string status = "Active") => new()
    {
        Name = "12V 75Ah Battery", Brand = "Bosch", Sku = sku, CategoryId = categoryId,
        SellerId = sellerId, Price = price, Quantity = quantity, Status = status,
        Specs = """{"voltage":12,"capacityAh":75}"""
    };

    [Fact]
    public async Task CreatePartAsync_WritesPartRow()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var part = MakePart(categoryId, sellerId, "SKU-1", 129.99m, 25);

        var result = await _service.CreatePartAsync(part);

        Assert.Equal("Part", result.ProductType);
        Assert.NotNull(await _db.Parts.FindAsync(result.Id));
        Assert.NotNull(await _db.Products.FindAsync(result.Id));
    }

    [Fact]
    public async Task GetCatalogAsync_FiltersByType()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 25));
        await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-2", 99.99m, 30));

        var result = await _service.GetCatalogAsync("Part", null, null, null, 1, 20);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("Part", i.ProductType));
    }

    [Fact]
    public async Task GetCatalogAsync_FiltersByMinPrice()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 25));
        await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-2", 99.99m, 30));

        var result = await _service.GetCatalogAsync("Part", null, 120, null, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(129.99m, result.Items[0].Price);
    }

    [Fact]
    public async Task GetPartsAsync_ReturnsFullPartFields()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 25));

        var result = await _service.GetPartsAsync(null, null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal("12V 75Ah Battery", item.Name);
        Assert.Equal("Bosch", item.Brand);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal("""{"voltage":12,"capacityAh":75}""", item.Specs);
    }

    [Fact]
    public async Task UpdateCarAsync_PreservesSpecs()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var car = new Car
        {
            BrandId = null, ModelId = null, Year = 2020, Mileage = 50000, Color = "Black",
            SellerId = sellerId, CategoryId = categoryId, Price = 25000m, Quantity = 1,
            Status = "Active", Description = "desc", Specs = """{"transmission":"auto"}"""
        };
        var created = await _service.CreateCarAsync(car);

        created.Specs = """{"transmission":"manual"}""";
        created.Price = 24000m;
        var result = await _service.UpdateCarAsync(created);

        Assert.Equal("""{"transmission":"manual"}""", result.Specs);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNull_WhenMissing()
    {
        var result = await _service.GetDetailAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePartAsync_ModifiesPart()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var part = await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 25));

        part.Price = 119.99m;
        var result = await _service.UpdatePartAsync(part);

        Assert.Equal(119.99m, result.Price);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductRow()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var part = await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 25));

        await _service.DeleteAsync(part.Id);

        Assert.Null(await _db.Products.FindAsync(part.Id));
        Assert.Null(await _db.Parts.FindAsync(part.Id));
    }

    [Fact]
    public async Task AddCartItemAsync_Throws_WhenStockInsufficient()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var part = await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 2));

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _service.AddCartItemAsync(Guid.NewGuid(), part.Id, 5));
    }

    [Fact]
    public async Task AddCartItemAsync_Adds_WhenStockAvailable()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var part = await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 10));
        var userId = Guid.NewGuid();

        var added = await _service.AddCartItemAsync(userId, part.Id, 3);

        Assert.True(added);
        var item = Assert.Single(await _db.CartItems.ToListAsync());
        Assert.Equal(part.Id, item.ProductId);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task AddFavoriteAsync_Adds_WhenNew_AndFalse_WhenDuplicate()
    {
        var (categoryId, sellerId) = await SeedBaseAsync();
        var part = await _service.CreatePartAsync(MakePart(categoryId, sellerId, "SKU-1", 129.99m, 10));
        var userId = Guid.NewGuid();

        var added = await _service.AddFavoriteAsync(userId, part.Id);
        var again = await _service.AddFavoriteAsync(userId, part.Id);

        Assert.True(added);
        Assert.False(again);
        Assert.Single(await _db.Favorites.ToListAsync());
    }
}

internal sealed class StubCacheService : ICacheService
{
    private readonly Dictionary<string, object> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(key, out var value);
        return Task.FromResult((T?)value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        _store[key] = value!;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task DeleteByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var prefix = pattern.Replace("*", "");
        foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix)).ToList())
            _store.Remove(key);
        return Task.CompletedTask;
    }
}

internal sealed class StubKafkaProducer : IKafkaProducer
{
    public Task PublishInvalidationAsync(string entityType, Guid entityId, string action, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishInventoryAsync(Guid productId, int quantity, string action, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
