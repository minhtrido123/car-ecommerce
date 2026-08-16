using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Services;

public interface ICarService
{
    Task<Car?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Car>> GetSimilarAsync(Guid modelId, Guid? excludeId, int limit, CancellationToken cancellationToken = default);
    Task<PagedResult<Car>> SearchCarsAsync(
        string? search, string? sortBy, string? sortDir,
        int pageNumber, int pageSize,
        decimal? minPrice = null, decimal? maxPrice = null,
        int? minMileage = null, int? maxMileage = null,
        int? minYear = null, int? maxYear = null,
        string? color = null, string? status = null, string[]? brandIds = null,
        CancellationToken cancellationToken = default);
    Task<CarFilters> GetFiltersAsync(CancellationToken cancellationToken = default);
}

public class CarService : ICarService
{
    private readonly SorchaDbContext _db;

    public CarService(SorchaDbContext db)
    {
        _db = db;
    }

    public async Task<Car?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Cars
            .Include(c => c.Brand)
            .Include(c => c.Model)
            .Include(c => c.Category)
            .Include(c => c.Seller)
            .Include(c => c.ProductImages.OrderBy(p => p.Position))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Car>> GetSimilarAsync(Guid modelId, Guid? excludeId, int limit, CancellationToken cancellationToken = default)
    {
        return await _db.Cars
            .Where(c => c.ModelId == modelId && c.Status == "Active")
            .Where(c => excludeId == null || c.Id != excludeId)
            .Include(c => c.Brand)
            .Include(c => c.Model)
            .Include(c => c.ProductImages.OrderBy(p => p.Position))
            .OrderByDescending(c => c.Year)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Car>> SearchCarsAsync(
        string? search, string? sortBy, string? sortDir,
        int pageNumber, int pageSize,
        decimal? minPrice = null, decimal? maxPrice = null,
        int? minMileage = null, int? maxMileage = null,
        int? minYear = null, int? maxYear = null,
        string? color = null, string? status = null, string[]? brandIds = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        IQueryable<Car> query = _db.Cars
            .AsNoTracking()
            .Include(c => c.Brand)
            .Include(c => c.Model)
            .Include(c => c.ProductImages.OrderBy(p => p.Position));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(c =>
                (c.Brand != null && c.Brand.Name.ToLower().Contains(q)) ||
                (c.Model != null && c.Model.Name.ToLower().Contains(q)) ||
                (c.Description != null && c.Description.ToLower().Contains(q)));
        }

        if (minPrice.HasValue) query = query.Where(c => c.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(c => c.Price <= maxPrice.Value);
        if (minMileage.HasValue) query = query.Where(c => c.Mileage >= minMileage.Value);
        if (maxMileage.HasValue) query = query.Where(c => c.Mileage <= maxMileage.Value);
        if (minYear.HasValue) query = query.Where(c => c.Year >= minYear.Value);
        if (maxYear.HasValue) query = query.Where(c => c.Year <= maxYear.Value);
        if (!string.IsNullOrWhiteSpace(color))
            query = query.Where(c => c.Color != null && c.Color.ToLower() == color.ToLower());
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status.ToLower() == status.ToLower());
        if (brandIds is { Length: > 0 })
        {
            var ids = brandIds.Where(b => Guid.TryParse(b, out _)).Select(Guid.Parse).ToArray();
            if (ids.Length > 0)
                query = query.Where(c => c.BrandId != null && ids.Contains(c.BrandId.Value));
        }

        var sort = (sortBy ?? "createdAt").ToLower();
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        query = sort switch
        {
            "price" => asc
                ? query.OrderBy(c => c.Price)
                : query.OrderByDescending(c => c.Price),
            "year" => asc
                ? query.OrderBy(c => c.Year)
                : query.OrderByDescending(c => c.Year),
            "mileage" => asc
                ? query.OrderBy(c => c.Mileage)
                : query.OrderByDescending(c => c.Mileage),
            _ => asc
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Car>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<CarFilters> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        var brandRows = await _db.Cars
            .Where(c => c.Brand != null)
            .Select(c => new { c.Brand!.Id, c.Brand!.Name })
            .Distinct()
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
        var brands = brandRows
            .Select(b => new CarFilterBrand(b.Id, b.Name))
            .ToList();

        var colors = await _db.Cars
            .Where(c => c.Color != null)
            .Select(c => c.Color)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        var statuses = await _db.Cars
            .Select(c => c.Status)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        var minPrice = await _db.Cars.MinAsync(c => (decimal?)c.Price, cancellationToken) ?? 0;
        var maxPrice = await _db.Cars.MaxAsync(c => (decimal?)c.Price, cancellationToken) ?? 0;
        var minMileage = await _db.Cars.MinAsync(c => (int?)c.Mileage, cancellationToken) ?? 0;
        var maxMileage = await _db.Cars.MaxAsync(c => (int?)c.Mileage, cancellationToken) ?? 0;
        var minYear = await _db.Cars.MinAsync(c => (int?)c.Year, cancellationToken) ?? 0;
        var maxYear = await _db.Cars.MaxAsync(c => (int?)c.Year, cancellationToken) ?? 0;

        if (maxPrice <= minPrice) maxPrice = minPrice + 1;
        if (maxMileage <= minMileage) maxMileage = minMileage + 1;
        if (maxYear <= minYear) maxYear = minYear + 1;

        return new CarFilters(brands, colors, statuses, minPrice, maxPrice, minMileage, maxMileage, minYear, maxYear);
    }
}
