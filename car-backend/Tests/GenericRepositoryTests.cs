using Infrastructure;
using Models;
using Repositories;

namespace Tests;

public class GenericRepositoryTests
{
    private readonly SorchaDbContext _db;
    private readonly EfRepository<User> _repo;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _repo = new EfRepository<User>(_db);
    }

    [Fact]
    public async Task AddAsync_SavesEntity_WithGeneratedId()
    {
        var entity = new User { Email = "test@test.com", PasswordHash = "hash", Role = "Customer", Name = "Test User" };
        var result = await _repo.AddAsync(entity);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("test@test.com", result.Email);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var entity = new User { Email = "found@test.com", PasswordHash = "hash", Role = "Customer", Name = "Found User" };
        await _repo.AddAsync(entity);

        var result = await _repo.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal("found@test.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        await _repo.AddAsync(new User { Email = "a@test.com", PasswordHash = "hash", Role = "Customer", Name = "A" });
        await _repo.AddAsync(new User { Email = "b@test.com", PasswordHash = "hash", Role = "Customer", Name = "B" });

        var result = await _repo.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResult()
    {
        for (int i = 0; i < 5; i++)
        {
            await _repo.AddAsync(new User { Email = $"item{i}@test.com", PasswordHash = "hash", Role = "Customer", Name = $"Item{i}" });
        }

        var result = await _repo.GetPagedAsync(1, 2, []);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingEntity()
    {
        var entity = new User { Email = "original@test.com", PasswordHash = "hash", Role = "Customer", Name = "Original" };
        await _repo.AddAsync(entity);

        entity.Email = "updated@test.com";
        var result = await _repo.UpdateAsync(entity);

        Assert.Equal("updated@test.com", result.Email);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var entity = new User { Email = "delete@test.com", PasswordHash = "hash", Role = "Customer", Name = "DeleteMe" };
        await _repo.AddAsync(entity);

        await _repo.DeleteAsync(entity.Id);

        var result = await _repo.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenEntityExists()
    {
        var entity = new User { Email = "exists@test.com", PasswordHash = "hash", Role = "Customer", Name = "Exists" };
        await _repo.AddAsync(entity);

        var result = await _repo.ExistsAsync(entity.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenEntityNotFound()
    {
        var result = await _repo.ExistsAsync(Guid.NewGuid());
        Assert.False(result);
    }
}