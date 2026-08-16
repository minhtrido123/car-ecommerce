using Infrastructure;
using Models;
using System.Linq.Expressions;

namespace Repositories;

public class EfRepository<T> : IRepository<T> where T : EntityBase
{
    private readonly SorchaDbContext _db;
    private readonly DbSet<T> _dbSet;

    public EfRepository(SorchaDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken);
        var items = _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
        // .ToListAsync(cancellationToken);
        if (includes != null)
        {
            foreach (var include in includes)
            {
                if (!string.IsNullOrWhiteSpace(include))
                {
                    items = items.Include(include.Trim());
                }
            }
        }

        var result = await items.ToListAsync(cancellationToken);
        return new PagedResult<T>(result, totalCount, pageNumber, pageSize);
    }

    public async Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbSet.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var entry = _db.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            var tracked = _db.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => e.Entity.Id == entity.Id && e.State != EntityState.Detached);
            if (tracked is not null)
            {
                tracked.State = EntityState.Detached;
            }
            _dbSet.Update(entity);
        }
        else
        {
            entry.State = EntityState.Modified;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is not null)
        {
            _dbSet.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }
}
