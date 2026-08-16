using Infrastructure;
using Models;
using Services;

namespace Tests;

public class OrderServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly OrderService _orderService;
    private readonly ProductService _productService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _orderService = new OrderService(_db, new StubKafkaProducer());
        _productService = new ProductService(_db, new StubCacheService(), new StubKafkaProducer());
    }

    private async Task<(Guid categoryId, Guid sellerId, Part part)> SeedPartAsync(int quantity)
    {
        var category = new Category { Name = "Battery", Slug = "battery", Type = "Part" };
        await _db.Categories.AddAsync(category);
        await _db.SaveChangesAsync();
        var seller = new User { Email = "seller@test.com", PasswordHash = "hash", Role = "Seller", Name = "Dealer" };
        await _db.Users.AddAsync(seller);
        await _db.SaveChangesAsync();
        var part = await _productService.CreatePartAsync(new Part
        {
            Name = "12V 75Ah Battery", Brand = "Bosch", Sku = "SKU-1", CategoryId = category.Id,
            SellerId = seller.Id, Price = 129.99m, Quantity = quantity, Status = "Active"
        });
        return (category.Id, seller.Id, part);
    }

    [Fact]
    public async Task CreateAsync_DeductsStock_AndCreatesOrder()
    {
        var (_, _, part) = await SeedPartAsync(10);
        var buyer = Guid.NewGuid();

        var order = await _orderService.CreateAsync(buyer, new CreateOrderRequest(new List<OrderItemInput>
        {
            new(part.Id, 3)
        }));

        Assert.Equal("Pending", order.Status);
        Assert.Equal(129.99m * 3, order.TotalAmount);
        var updated = await _db.Products.FindAsync(part.Id);
        Assert.Equal(7, updated!.Quantity);
    }

    [Fact]
    public async Task CreateAsync_SetsCarSold()
    {
        var brand = new Brand { Name = "Toyota", Country = "Japan" };
        await _db.Brands.AddAsync(brand);
        await _db.SaveChangesAsync();
        var model = new CarModel { BrandId = brand.Id, Name = "Camry" };
        await _db.CarModels.AddAsync(model);
        await _db.SaveChangesAsync();
        var car = await _productService.CreateCarAsync(new Car
        {
            BrandId = brand.Id, ModelId = model.Id, Year = 2021, Price = 25000m, Status = "Active"
        });

        await _orderService.CreateAsync(Guid.NewGuid(), new CreateOrderRequest(new List<OrderItemInput>
        {
            new(car.Id, 1)
        }));

        var updated = await _db.Products.FindAsync(car.Id);
        Assert.Equal("Sold", updated!.Status);
    }

    [Fact]
    public async Task CreateAsync_SetsPartInactive_AtZeroStock()
    {
        var (_, _, part) = await SeedPartAsync(2);

        await _orderService.CreateAsync(Guid.NewGuid(), new CreateOrderRequest(new List<OrderItemInput>
        {
            new(part.Id, 2)
        }));

        var updated = await _db.Products.FindAsync(part.Id);
        Assert.Equal(0, updated!.Quantity);
        Assert.Equal("Inactive", updated.Status);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenStockInsufficient()
    {
        var (_, _, part) = await SeedPartAsync(2);

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _orderService.CreateAsync(Guid.NewGuid(), new CreateOrderRequest(new List<OrderItemInput>
            {
                new(part.Id, 5)
            })));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenProductInactive()
    {
        var (_, _, part) = await SeedPartAsync(2);
        part.Status = "Inactive";
        await _productService.UpdatePartAsync(part);

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            _orderService.CreateAsync(Guid.NewGuid(), new CreateOrderRequest(new List<OrderItemInput>
            {
                new(part.Id, 1)
            })));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenItemsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderService.CreateAsync(Guid.NewGuid(), new CreateOrderRequest(new List<OrderItemInput>())));
    }
}
