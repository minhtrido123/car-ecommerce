using System.Security.Cryptography;
using System.Text;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Models;
using Repositories;
using Services;

namespace Tests;

public class AuthServiceTests
{
    private readonly SorchaDbContext _db;
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<SorchaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new SorchaDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-test-secret-test-secret-test-secret",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();
        _auth = new AuthService(new EfRepository<User>(_db), new FakeRefreshTokenRepository(_db), config);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPasswordAndCustomerRole()
    {
        var result = await _auth.RegisterAsync("alice@test.com", "password123", "Alice", false);

        var user = Assert.Single(await _db.Users.ToListAsync());
        Assert.Equal("Customer", user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash));
        Assert.Equal("alice@test.com", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenEmailTaken()
    {
        await _auth.RegisterAsync("dup@test.com", "password123", "Alice", false);

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _auth.RegisterAsync("dup@test.com", "other", "Bob", false));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WithCorrectCredentials()
    {
        await _auth.RegisterAsync("bob@test.com", "password123", "Bob", false);

        var result = await _auth.LoginAsync("bob@test.com", "password123", true);

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal("bob@test.com", result.Email);
    }

    [Fact]
    public async Task LoginAsync_Throws_WithWrongPassword()
    {
        await _auth.RegisterAsync("carol@test.com", "password123", "Carol", false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _auth.LoginAsync("carol@test.com", "wrong-password", false));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserMissing()
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _auth.LoginAsync("missing@test.com", "password123", false));
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_AndRevokesOld()
    {
        var registered = await _auth.RegisterAsync("dave@test.com", "password123", "Dave", true);

        var refreshed = await _auth.RefreshAsync(registered.RefreshToken);

        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
        var oldHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(registered.RefreshToken))).ToLowerInvariant();
        var oldRow = Assert.Single(await _db.RefreshTokens.ToListAsync(), t => t.Token == oldHash);
        Assert.True(oldRow.IsRevoked);
        Assert.Single(await _db.RefreshTokens.ToListAsync(), t => !t.IsRevoked);
        Assert.Equal(2, (await _db.RefreshTokens.ToListAsync()).Count);
    }

    [Fact]
    public async Task RefreshAsync_Throws_WhenTokenReused()
    {
        var registered = await _auth.RegisterAsync("erin@test.com", "password123", "Erin", true);
        await _auth.RefreshAsync(registered.RefreshToken);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            _auth.RefreshAsync(registered.RefreshToken));
    }

    [Fact]
    public async Task RefreshAsync_Throws_WhenTokenUnknown()
    {
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            _auth.RefreshAsync("definitely-not-a-real-token"));
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly EfRepository<RefreshToken> _inner;

    public FakeRefreshTokenRepository(SorchaDbContext db)
    {
        _inner = new EfRepository<RefreshToken>(db);
    }

    public async Task<bool> RevokeIfActiveAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var match = (await _inner.GetFilteredAsync(t => t.Token == tokenHash, cancellationToken)).FirstOrDefault();
        if (match is null || match.IsRevoked || match.ExpiresAt <= DateTime.UtcNow) return false;
        match.IsRevoked = true;
        await _inner.UpdateAsync(match, cancellationToken);
        return true;
    }

    public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _inner.GetByIdAsync(id, cancellationToken);
    public Task<List<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default) => _inner.GetAllAsync(cancellationToken);
    public Task<PagedResult<RefreshToken>> GetPagedAsync(int pageNumber, int pageSize, string[] includes, CancellationToken cancellationToken = default) => _inner.GetPagedAsync(pageNumber, pageSize, includes, cancellationToken);
    public Task<List<RefreshToken>> GetFilteredAsync(System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate, CancellationToken cancellationToken = default) => _inner.GetFilteredAsync(predicate, cancellationToken);
    public Task<RefreshToken> AddAsync(RefreshToken entity, CancellationToken cancellationToken = default) => _inner.AddAsync(entity, cancellationToken);
    public Task<RefreshToken> UpdateAsync(RefreshToken entity, CancellationToken cancellationToken = default) => _inner.UpdateAsync(entity, cancellationToken);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => _inner.DeleteAsync(id, cancellationToken);
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => _inner.ExistsAsync(id, cancellationToken);
}
