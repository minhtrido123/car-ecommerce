using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetCatalogAsync(string? type, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<PartResponse>> GetPartsAsync(Guid? categoryId, decimal? minPrice, decimal? maxPrice, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailResponse?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PartResponse?> GetPartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Part> CreatePartAsync(Part part, CancellationToken cancellationToken = default);
    Task<Part> UpdatePartAsync(Part part, CancellationToken cancellationToken = default);
    Task<Car> CreateCarAsync(Car car, CancellationToken cancellationToken = default);
    Task<Car> UpdateCarAsync(Car car, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AddFavoriteAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task RemoveFavoriteAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> AddCartItemAsync(Guid userId, Guid productId, int quantity, CancellationToken cancellationToken = default);
    Task RemoveCartItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
}

public class ProductService : IProductService
{
    private readonly SorchaDbContext _db;
    private readonly Lazy<ICacheService> _cache;
    private readonly Lazy<IKafkaProducer> _kafkaProducer;

    public ProductService(SorchaDbContext db, Lazy<ICacheService> cache, Lazy<IKafkaProducer> kafkaProducer)
    {
        _db = db;
        _cache = cache;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<PagedResult<ProductResponse>> GetCatalogAsync(string? type, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products:list:{type}:{categoryId}:{minPrice}:{maxPrice}:{pageNumber}:{pageSize}";
        if (_cache.Value is not null)
        {
            var cached = await _cache.Value.GetAsync<PagedResult<ProductResponse>>(cacheKey, cancellationToken);
            if (cached is not null) return cached;
        }
        IQueryable<Product> query = (type?.ToUpperInvariant()) switch
        {
            "CAR" => _db.Cars.AsNoTracking()
                .Include(c => c.Brand).Include(c => c.Model)
                .Include(c => c.Category).Include(c => c.ProductImages),
            "PART" => _db.Parts.AsNoTracking()
                .Include(p => p.Category).Include(p => p.ProductImages),
            _ => _db.Products.AsNoTracking()
                .Include(p => p.Category).Include(p => p.ProductImages)
        };

        query = query
            .Where(p => categoryId == null || p.CategoryId == categoryId)
            .Where(p => minPrice == null || p.Price >= minPrice)
            .Where(p => maxPrice == null || p.Price <= maxPrice);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ProductResponse>(
            items.Select(ProductMapping.ToResponse).ToList(), totalCount, pageNumber, pageSize);
        if (_cache.Value is not null)
            await _cache.Value.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
        return result;
    }

    public async Task<ProductDetailResponse?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{id}";
        // Check cache exists
        try
        {
            var cached = await _cache.Value.GetAsync<ProductDetailResponse>(cacheKey, cancellationToken);
            if (cached is not null) return cached;
        }
        catch { }
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null) return null;

        Car? car = null;
        Part? part = null;
        if (product.ProductType == "Car")
        {
            car = await _db.Cars.AsNoTracking()
                .Include(c => c.Brand).Include(c => c.Model)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        else
        {
            part = await _db.Parts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        var response = ProductMapping.ToDetail(product, car, part);
        try
        {
            await _cache.Value.SetAsync(cacheKey, response, cancellationToken: cancellationToken);
        }
        catch { }
        return response;
    }

    public async Task<PartResponse?> GetPartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var part = await _db.Parts.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return part is null ? null : ProductMapping.ToPartResponse(part);
    }

    public async Task<PagedResult<PartResponse>> GetPartsAsync(Guid? categoryId, decimal? minPrice, decimal? maxPrice, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"parts:list:{categoryId}:{minPrice}:{maxPrice}:{pageNumber}:{pageSize}";
        try
        {
            var cached = await _cache.Value.GetAsync<PagedResult<PartResponse>>(cacheKey, cancellationToken);
            if (cached is not null) return cached;
        }
        catch { }
        IQueryable<Part> query = _db.Parts.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductImages);

        query = query
            .Where(p => categoryId == null || p.CategoryId == categoryId)
            .Where(p => minPrice == null || p.Price >= minPrice)
            .Where(p => maxPrice == null || p.Price <= maxPrice);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<PartResponse>(
            items.Select(ProductMapping.ToPartResponse).ToList(), totalCount, pageNumber, pageSize);
        try
        {
            await _cache.Value.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
        }
        catch { }
        return result;
    }

    public async Task<Part> CreatePartAsync(Part part, CancellationToken cancellationToken = default)
    {
        part.ProductType = "Part";
        await _db.Parts.AddAsync(part, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateProductAsync(part.Id, "created", cancellationToken);
        return part;
    }

    public async Task<Part> UpdatePartAsync(Part part, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Parts.FirstOrDefaultAsync(p => p.Id == part.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Part {part.Id} not found");

        existing.Name = part.Name;
        existing.Brand = part.Brand;
        existing.Sku = part.Sku;
        existing.SellerId = part.SellerId;
        existing.CategoryId = part.CategoryId;
        existing.Price = part.Price;
        existing.Quantity = part.Quantity;
        existing.Status = part.Status;
        existing.Description = part.Description;
        existing.Specs = part.Specs;

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateProductAsync(part.Id, "updated", cancellationToken);
        return existing;
    }

    public async Task<Car> CreateCarAsync(Car car, CancellationToken cancellationToken = default)
    {
        car.ProductType = "Car";
        car.Quantity = 1;
        await _db.Cars.AddAsync(car, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateProductAsync(car.Id, "created", cancellationToken);
        return car;
    }

    public async Task<Car> UpdateCarAsync(Car car, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Cars.FirstOrDefaultAsync(c => c.Id == car.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Car {car.Id} not found");

        existing.BrandId = car.BrandId;
        existing.ModelId = car.ModelId;
        existing.Year = car.Year;
        existing.Mileage = car.Mileage;
        existing.Color = car.Color;
        existing.SellerId = car.SellerId;
        existing.CategoryId = car.CategoryId;
        existing.Price = car.Price;
        existing.Quantity = car.Quantity;
        existing.Status = car.Status;
        existing.Description = car.Description;
        existing.Specs = car.Specs;

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateProductAsync(car.Id, "updated", cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FindAsync([id], cancellationToken);
        if (product is not null)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync(cancellationToken);
        }
        await InvalidateProductAsync(id, "deleted", cancellationToken);
    }

    public async Task<bool> AddFavoriteAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        if (await _db.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId, cancellationToken))
            return false;

        _db.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var favorite = await _db.Favorites.FirstOrDefaultAsync(
            f => f.UserId == userId && f.ProductId == productId, cancellationToken);
        if (favorite is not null)
        {
            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> AddCartItemAsync(Guid userId, Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 1) throw new ArgumentException("Quantity must be at least 1");
        if (await _db.CartItems.AnyAsync(c => c.UserId == userId && c.ProductId == productId, cancellationToken))
            return false;

        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null || product.Status != "Active" || product.Quantity < quantity)
            throw new InsufficientStockException(productId);

        _db.CartItems.Add(new CartItem { UserId = userId, ProductId = productId, Quantity = quantity });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RemoveCartItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(
            c => c.UserId == userId && c.ProductId == productId, cancellationToken);
        if (item is not null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task InvalidateProductAsync(Guid id, string action, CancellationToken cancellationToken)
    {
        try
        {
            await _kafkaProducer.Value.PublishInvalidationAsync("Product", id, action, cancellationToken);
            await _cache.Value.DeleteAsync($"product:{id}", cancellationToken);
            await _cache.Value.DeleteByPatternAsync("products:list:*", cancellationToken);
        }
        catch { }
    }
}
