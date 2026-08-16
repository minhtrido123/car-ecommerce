using Infrastructure;
using Models;
using System.Linq.Expressions;

namespace Services;

public class Service<T> : IService<T> where T : EntityBase
{
    private readonly ICachedRepository<T> _repository;

    public Service(ICachedRepository<T> repository)
    {
        _repository = repository;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default)
    {
        return await _repository.GetPagedAsync(pageNumber, pageSize, includes, cancellationToken);
    }

    public async Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _repository.GetFilteredAsync(predicate, cancellationToken);
    }

    public async Task<T> CreateAsync(CreateRequest<T> request, CancellationToken cancellationToken = default)
    {
        return await _repository.AddAsync(request.Data, cancellationToken);
    }

    public async Task<T> UpdateAsync(UpdateRequest<T> request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with id {request.Id} not found");
        }
        CopyProperties(request.Data, existing);
        return await _repository.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
    }

    private static void CopyProperties(T source, T target)
    {
        foreach (var prop in typeof(T).GetProperties())
        {
            if (prop.CanWrite && prop.Name != nameof(EntityBase.Id) && prop.Name != nameof(EntityBase.CreatedAt) && prop.Name != nameof(EntityBase.CreatedBy)
                && IsScalar(prop.PropertyType))
            {
                prop.SetValue(target, prop.GetValue(source));
            }
        }
    }

    private static bool IsScalar(Type type) =>
        type == typeof(string) || (!type.IsClass && !type.IsInterface);
}
