using Infrastructure;
using Models;
using Repositories;
using Services;
using System.Linq.Expressions;

namespace Tests;

public class GenericServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly EfRepository<User> _repo;
    private readonly Service<User> _service;

    public GenericServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _repo = new EfRepository<User>(_db);
        _service = new Service<User>(new StubCachedRepository<User>(_repo));
    }

    [Fact]
    public async Task CreateAsync_SavesNewEntity()
    {
        var result = await _service.CreateAsync(new CreateRequest<User>(new User { Email = "new@test.com", PasswordHash = "hash", Role = "Customer", Name = "New User" }));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("new@test.com", result.Email);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingEntity()
    {
        var entity = new User { Email = "original@test.com", PasswordHash = "hash", Role = "Customer", Name = "Original" };
        await _repo.AddAsync(entity);

        var result = await _service.UpdateAsync(new UpdateRequest<User>(entity.Id, new User { Email = "modified@test.com", PasswordHash = "hash", Role = "Customer", Name = "Modified" }));

        Assert.Equal("modified@test.com", result.Email);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenEntityNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateAsync(new UpdateRequest<User>(Guid.NewGuid(), new User { Email = "missing@test.com", PasswordHash = "hash", Role = "Customer", Name = "Missing" })));
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var entity = new User { Email = "delete@test.com", PasswordHash = "hash", Role = "Customer", Name = "DeleteMe" };
        await _repo.AddAsync(entity);

        await _service.DeleteAsync(entity.Id);

        var result = await _repo.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var entity = new User { Email = "found@test.com", PasswordHash = "hash", Role = "Customer", Name = "Found" };
        await _repo.AddAsync(entity);

        var result = await _service.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal("found@test.com", result.Email);
    }
}

internal sealed class StubCachedRepository<T> : ICachedRepository<T> where T : EntityBase
{
    private readonly EfRepository<T> _inner;

    public StubCachedRepository(EfRepository<T> inner)
    {
        _inner = inner;
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _inner.GetByIdAsync(id, cancellationToken);
    public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) => _inner.GetAllAsync(cancellationToken);
    public Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default) => _inner.GetPagedAsync(pageNumber, pageSize, includes, cancellationToken);
    public Task<List<T>> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => _inner.GetFilteredAsync(predicate, cancellationToken);
    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default) => _inner.AddAsync(entity, cancellationToken);
    public Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default) => _inner.UpdateAsync(entity, cancellationToken);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => _inner.DeleteAsync(id, cancellationToken);
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => _inner.ExistsAsync(id, cancellationToken);
}