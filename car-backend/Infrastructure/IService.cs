using Models;
using System.Linq.Expressions;

namespace Infrastructure;

public interface IService<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default);
    Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(CreateRequest<T> request, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(UpdateRequest<T> request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
