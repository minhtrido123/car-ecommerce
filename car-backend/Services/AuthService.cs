using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string name, bool rememberMe, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public record AuthResult(
    Guid UserId,
    string Email,
    string Role,
    string Name,
    DateTime CreatedAt,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IRepository<User> userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string name, bool rememberMe, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetFilteredAsync(u => u.Email == email, cancellationToken);
        if (existing.Count > 0) throw new EmailAlreadyExistsException();

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Customer",
            Name = name
        };
        await _userRepository.AddAsync(user, cancellationToken);
        return await IssueTokensAsync(user, rememberMe, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetFilteredAsync(u => u.Email == email, cancellationToken);
        var user = users.FirstOrDefault();
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return await IssueTokensAsync(user, rememberMe, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var matches = await _refreshTokenRepository.GetFilteredAsync(t => t.Token == tokenHash, cancellationToken);
        var stored = matches.FirstOrDefault();
        if (stored is null) throw new InvalidRefreshTokenException();

        var revoked = await _refreshTokenRepository.RevokeIfActiveAsync(tokenHash, cancellationToken);
        if (!revoked) throw new InvalidRefreshTokenException();

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null) throw new InvalidRefreshTokenException();

        return await IssueTokensAsync(user, true, cancellationToken);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, bool rememberMe, CancellationToken cancellationToken)
    {
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes);

        var refreshToken = "";
        if (rememberMe)
        {
            refreshToken = GenerateRefreshToken();
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                Token = HashToken(refreshToken),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                IsRevoked = false
            }, cancellationToken);
        }
        return new AuthResult(
            user.Id,
            user.Email,
            user.Role,
            user.Name,
            user.CreatedAt,
            GenerateAccessToken(user, accessTokenExpiry),
            refreshToken,
            accessTokenExpiry);
    }

    private string GenerateAccessToken(User user, DateTime expires)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "",
            audience: _configuration["Jwt:Audience"] ?? "",
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    private int AccessTokenExpiryMinutes =>
        int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var minutes) ? minutes : 15;

    private int RefreshTokenExpiryDays =>
        int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var days) ? days : 7;
}
