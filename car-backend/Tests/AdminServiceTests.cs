using Infrastructure;
using Models;
using Repositories;
using Services;

namespace Tests;

public class AdminServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly AdminService _admin;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        _admin = new AdminService(new EfRepository<User>(_db));
    }

    [Fact]
    public async Task CreateAdminAsync_CreatesUserWithHashedPasswordAndGivenRole()
    {
        var user = await _admin.CreateAdminAsync("boss@test.com", "secret123", "Boss", "Admin");

        Assert.Equal("boss@test.com", user.Email);
        Assert.Equal("Admin", user.Role);
        Assert.Equal("Boss", user.Name);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", user.PasswordHash));
    }

    [Fact]
    public async Task CreateAdminAsync_Throws_WhenEmailTaken()
    {
        await _admin.CreateAdminAsync("dup@test.com", "secret123", "A", "Admin");

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _admin.CreateAdminAsync("dup@test.com", "other", "B", "Admin"));
    }

    [Fact]
    public async Task UpdateAdminAsync_UpdatesFieldsAndRehashesPassword()
    {
        var created = await _admin.CreateAdminAsync("a@test.com", "old-pass", "A", "Admin");

        var updated = await _admin.UpdateAdminAsync(created.Id, "b@test.com", "B", "Staff", "new-pass");

        Assert.Equal("b@test.com", updated.Email);
        Assert.Equal("B", updated.Name);
        Assert.Equal("Staff", updated.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("new-pass", updated.PasswordHash));
    }

    [Fact]
    public async Task UpdateAdminAsync_KeepsPassword_WhenBlankOrNull()
    {
        var created = await _admin.CreateAdminAsync("c@test.com", "keep-pass", "C", "Admin");

        var updated = await _admin.UpdateAdminAsync(created.Id, "c@test.com", "C", "Admin", null);

        Assert.True(BCrypt.Net.BCrypt.Verify("keep-pass", updated.PasswordHash));
    }

    [Fact]
    public async Task UpdateAdminAsync_Throws_WhenUserMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _admin.UpdateAdminAsync(Guid.NewGuid(), "x@test.com", "X", "Admin", null));
    }

    [Fact]
    public async Task UpdateAdminAsync_Throws_WhenEmailTakenByAnotherUser()
    {
        var first = await _admin.CreateAdminAsync("one@test.com", "p", "One", "Admin");
        await _admin.CreateAdminAsync("two@test.com", "p", "Two", "Admin");

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _admin.UpdateAdminAsync(first.Id, "two@test.com", "One", "Admin", null));
    }
}
