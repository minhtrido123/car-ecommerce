# Auth Endpoints Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add register, login, and refresh-token endpoints to the car-backend minimal API, with BCrypt password hashing and rotating opaque refresh tokens stored hashed in Postgres.

**Architecture:** JWT bearer auth is already wired (`AddJwtAuthentication` in `Sorcha.ServiceDefaults`). This adds a `RefreshToken` entity + migration, a new `AuthService` in the Services layer issuing HS256 access tokens + opaque refresh tokens, and three anonymous minimal-API endpoints in `API/Program.cs`.

**Tech Stack:** .NET 10 / C# 13, ASP.NET Core minimal APIs, EF Core 10 + Npgsql, `BCrypt.Net-Next`, `System.IdentityModel.Tokens.Jwt` (available via the AspNetCore shared framework — no new package), xUnit + EF InMemory for tests.

## Global Constraints

- Target framework is `net10.0` everywhere. `Nullable` and `ImplicitUsings` enabled.
- **Workspace has no git repo.** Commit steps assume a repo; if `git rev-parse` fails, skip the commit step and note it.
- No solution file. Build/test per project from `car-backend/`:
  - Build: `dotnet build Infrastructure`, `dotnet build Services`, `dotnet build API`
  - Test: `dotnet test Tests`
- Migration command (DesignTime factory exists in Infrastructure):
  - `dotnet ef migrations add AddRefreshTokens --project Infrastructure --startup-project API`
- Follow existing patterns: generic repositories via `IRepository<>`, global usings per project, minimal APIs only (no controllers).
- AuthService must NOT be cached. It consumes `IRepository<User>` and `IRepository<RefreshToken>` (registered as `EfRepository`).
- Endpoints return HTTP status codes by catching AuthService exceptions:
  - `EmailAlreadyExistsException` → 409
  - `InvalidCredentialsException` → 401
  - `InvalidRefreshTokenException` → 401
- Access token: JWT HS256, 15 min. Refresh token: 64 random bytes base64, 7 days, stored as lowercase SHA-256 hex.
- `User` role on register is always `Customer`.
- No code comments in new C# files (project convention).

---

### Task 1: RefreshToken Entity + DbContext + Migration

**Files:**
- Create: `car-backend/Models/RefreshToken.cs`
- Modify: `car-backend/Infrastructure/SorchaDbContext.cs`
- Create (generated): `car-backend/Infrastructure/Migrations/*AddRefreshTokens*`

**Interfaces:**
- Consumes: `EntityBase` (from `Models/EntityBase.cs`).
- Produces: `Models.RefreshToken` with properties `Token` (string), `UserId` (Guid), `ExpiresAt` (DateTime), `IsRevoked` (bool). DbSet `RefreshTokens` on `SorchaDbContext`.

- [ ] **Step 1: Create the entity**

Create `car-backend/Models/RefreshToken.cs`:

```csharp
namespace Models;

public class RefreshToken : EntityBase
{
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

- [ ] **Step 2: Register DbSet and configuration**

In `car-backend/Infrastructure/SorchaDbContext.cs`, add to the DbSet block (after `Users`):

```csharp
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

In `OnModelCreating`, after the `User` configuration block:

```csharp
modelBuilder.Entity<RefreshToken>(entity =>
{
    entity.HasIndex(r => r.Token).IsUnique();
    entity.Property(r => r.Token).HasMaxLength(64);
    entity.HasIndex(r => r.UserId);
});
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build Infrastructure` then `dotnet build Models` (in `car-backend/`)
Expected: Build succeeded.

- [ ] **Step 4: Generate the migration**

Run: `dotnet ef migrations add AddRefreshTokens --project Infrastructure --startup-project API`
Expected: A new `*_AddRefreshTokens.cs` (+ Designer + updated model snapshot) under `car-backend/Infrastructure/Migrations/`. Open the migration and confirm it creates table `RefreshTokens` with unique index on `Token`.

- [ ] **Step 5: Apply migration if DB reachable**

Run: `dotnet ef database update --project Infrastructure --startup-project API`
If the Postgres connection fails, note it and continue — the migration file is the deliverable.

- [ ] **Step 6: Run existing tests**

Run: `dotnet test Tests`
Expected: All existing tests pass.

- [ ] **Step 7: Commit (skip if no git repo)**

```bash
git add Models/RefreshToken.cs Infrastructure/SorchaDbContext.cs Infrastructure/Migrations/
git commit -m "feat: add RefreshToken entity and migration"
```

---

### Task 2: BCrypt Hashing + Register/Login

**Files:**
- Modify: `car-backend/Services/Services.csproj`
- Create: `car-backend/Services/AuthExceptions.cs`
- Create: `car-backend/Services/AuthService.cs` (contains `IAuthService`, `AuthService`, `AuthResult`)
- Create: `car-backend/Tests/AuthServiceTests.cs` (register/login tests only; refresh tests come in Task 3)

**Interfaces:**
- Consumes: `IRepository<User>`, `IRepository<RefreshToken>` (Infrastructure), `Microsoft.Extensions.Configuration.IConfiguration`, `Models.User`.
- Produces:
  - `Services.IAuthService` with `Task<AuthResult> RegisterAsync(string email, string password, string name, CancellationToken cancellationToken = default)` and `Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)`.
  - `Services.AuthResult` record: `AuthResult(Guid UserId, string Email, string Role, string Name, DateTime CreatedAt, string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt)`.
  - `Services.EmailAlreadyExistsException`, `Services.InvalidCredentialsException` (and `InvalidRefreshTokenException` for Task 3).
  - JWT claims: `sub` = user id, `email`, `name`, `role` (via `ClaimTypes.Role`).

- [ ] **Step 1: Add BCrypt package**

Run (in `car-backend/Services`): `dotnet add Services package BCrypt.Net-Next`
Expected: Package reference added to `Services.csproj`.

- [ ] **Step 2: Write the failing register/login tests**

Create `car-backend/Tests/AuthServiceTests.cs`:

```csharp
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
        _auth = new AuthService(new EfRepository<User>(_db), new EfRepository<RefreshToken>(_db), config);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPasswordAndCustomerRole()
    {
        var result = await _auth.RegisterAsync("alice@test.com", "password123", "Alice");

        var user = Assert.Single(await _db.Users.ToListAsync());
        Assert.Equal("Customer", user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash));
        Assert.Equal("alice@test.com", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenEmailTaken()
    {
        await _auth.RegisterAsync("dup@test.com", "password123", "Alice");

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            _auth.RegisterAsync("dup@test.com", "other", "Bob"));
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WithCorrectCredentials()
    {
        await _auth.RegisterAsync("bob@test.com", "password123", "Bob");

        var result = await _auth.LoginAsync("bob@test.com", "password123");

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal("bob@test.com", result.Email);
    }

    [Fact]
    public async Task LoginAsync_Throws_WithWrongPassword()
    {
        await _auth.RegisterAsync("carol@test.com", "password123", "Carol");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _auth.LoginAsync("carol@test.com", "wrong-password"));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserMissing()
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _auth.LoginAsync("missing@test.com", "password123"));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Tests`
Expected: Compilation fails — `AuthService`, `EmailAlreadyExistsException`, `InvalidCredentialsException` don't exist yet.

- [ ] **Step 4: Create exception types**

Create `car-backend/Services/AuthExceptions.cs`:

```csharp
namespace Services;

public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException() : base("Email already registered") { }
}

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password") { }
}

public sealed class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Invalid refresh token") { }
}
```

- [ ] **Step 5: Create AuthService**

Create `car-backend/Services/AuthService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string name, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
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
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<RefreshToken> refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string name, CancellationToken cancellationToken = default)
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
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetFilteredAsync(u => u.Email == email, cancellationToken);
        var user = users.FirstOrDefault();
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return await IssueTokensAsync(user, cancellationToken);
    }

    public Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        throw new InvalidRefreshTokenException();
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes);
        var refreshToken = GenerateRefreshToken();
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Token = HashToken(refreshToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false
        }, cancellationToken);

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
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Tests`
Expected: All 5 new tests pass; existing tests still pass.

- [ ] **Step 7: Commit (skip if no git repo)**

```bash
git add Services/Services.csproj Services/AuthExceptions.cs Services/AuthService.cs Tests/AuthServiceTests.cs
git commit -m "feat: add register and login with BCrypt password hashing"
```

---

### Task 3: Refresh Token Rotation

**Files:**
- Modify: `car-backend/Tests/AuthServiceTests.cs` (add refresh tests)
- Modify: `car-backend/Services/AuthService.cs` (implement `RefreshAsync`)

**Interfaces:**
- Consumes: `Services.AuthResult` from Task 2.
- Produces: Fully implemented `RefreshAsync` — validates stored token (hashed), rejects revoked/expired/unknown, revokes old token, issues new pair.

- [ ] **Step 1: Write the failing refresh tests**

Append to `car-backend/Tests/AuthServiceTests.cs` (add `using System.Security.Cryptography;` and `using System.Text;` at top if not present):

```csharp
[Fact]
public async Task RefreshAsync_RotatesToken_AndRevokesOld()
{
    var registered = await _auth.RegisterAsync("dave@test.com", "password123", "Dave");

    var refreshed = await _auth.RefreshAsync(registered.RefreshToken);

    Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
    var oldHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(registered.RefreshToken))).ToLowerInvariant();
    var oldRow = Assert.Single(await _db.RefreshTokens.ToListAsync(), t => t.Token == oldHash);
    Assert.True(oldRow.IsRevoked);
    Assert.Equal(2, (await _db.RefreshTokens.ToListAsync()).Count);
}

[Fact]
public async Task RefreshAsync_Throws_WhenTokenReused()
{
    var registered = await _auth.RegisterAsync("erin@test.com", "password123", "Erin");
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests`
Expected: `RefreshAsync_RotatesToken_AndRevokesOld` fails (stub throws `InvalidRefreshTokenException` instead of returning a new pair). `Throws_WhenTokenReused` and `Throws_WhenTokenUnknown` may pass — false-green, acceptable; they only assert the stub throws.

- [ ] **Step 3: Implement RefreshAsync**

Replace the stub in `AuthService` (from Task 2, Step 5) with the full rotation implementation:

```csharp
    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var matches = await _refreshTokenRepository.GetFilteredAsync(t => t.Token == tokenHash, cancellationToken);
        var stored = matches.FirstOrDefault();
        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidRefreshTokenException();

        stored.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null) throw new InvalidRefreshTokenException();

        return await IssueTokensAsync(user, cancellationToken);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests`
Expected: All tests pass, including the 3 new refresh tests.

- [ ] **Step 5: Commit (skip if no git repo)**

```bash
git add Services/AuthService.cs Tests/AuthServiceTests.cs
git commit -m "feat: add refresh token rotation"
```

---

### Task 4: Endpoints, DTOs, Config, DI

**Files:**
- Modify: `car-backend/API/appsettings.json`
- Create: `car-backend/API/DTOs/AuthDtos.cs`
- Modify: `car-backend/API/Program.cs`

**Interfaces:**
- Consumes: `Services.IAuthService`, `Services.AuthResult`, exceptions from Task 2/3; `API.DTOs.UserResponse` (already exists at `API/DTOs/UserDto.cs:3`).
- Produces: `API.DTOs.RegisterRequest`, `LoginRequest`, `RefreshRequest`, `AuthResponse`; endpoints `POST /api/auth/register`, `/login`, `/refresh`.

- [ ] **Step 1: Add Jwt config to appsettings.json**

In `car-backend/API/appsettings.json`, after the `"Kafka"` block, add:

```json
  },
  "Jwt": {
    "Secret": "nakdfnjadnfjnaleqtwqitri@_p",
    "Issuer": "CarEcommerce",
    "Audience": "CarFrontEnd",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

(Replace the final `}` with `},` and append the `Jwt` block + closing `}`.)

- [ ] **Step 2: Create auth DTOs**

Create `car-backend/API/DTOs/AuthDtos.cs`:

```csharp
namespace API.DTOs;

public record RegisterRequest(string Email, string Password, string Name);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User);
```

- [ ] **Step 3: Register AuthService and map endpoints**

In `car-backend/API/Program.cs`:

After `builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));` (line 14), add:

```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
```

After the `MapEntityEndpoints<CartItem>(app, "cart-items");` line (line 51), before `app.Run();`, add:

```csharp
MapAuthEndpoints(app);
```

At the bottom of the file, after `MapEntityEndpoints`, add:

```csharp
static void MapAuthEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/auth");

    group.MapPost("/register", async ([FromServices] IAuthService authService, RegisterRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await authService.RegisterAsync(request.Email, request.Password, request.Name, cancellationToken);
            return Results.Json(ToAuthResponse(result), statusCode: StatusCodes.Status201Created);
        }
        catch (EmailAlreadyExistsException)
        {
            return Results.Conflict(new { error = "Email already registered" });
        }
    });

    group.MapPost("/login", async ([FromServices] IAuthService authService, LoginRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
            return Results.Ok(ToAuthResponse(result));
        }
        catch (InvalidCredentialsException)
        {
            return Results.Unauthorized();
        }
    });

    group.MapPost("/refresh", async ([FromServices] IAuthService authService, RefreshRequest request, CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
            return Results.Ok(ToAuthResponse(result));
        }
        catch (InvalidRefreshTokenException)
        {
            return Results.Unauthorized();
        }
    });
}

static AuthResponse ToAuthResponse(AuthResult result) =>
    new(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAt,
        new UserResponse(result.UserId, result.Email, result.Role, result.Name, result.CreatedAt));
```

- [ ] **Step 4: Build**

Run: `dotnet build API`
Expected: Build succeeded, no warnings about unresolved types.

- [ ] **Step 5: Run tests**

Run: `dotnet test Tests`
Expected: All tests pass.

- [ ] **Step 6: Smoke test the endpoints**

Start the API (Postgres + Redis must be reachable; run from `car-backend`):
`dotnet run --project API`

Then in another shell:

```bash
curl -s -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"smoke@test.com","password":"password123","name":"Smoke"}' | tee /tmp/auth-register.json

TOKEN=$(jq -r .accessToken /tmp/auth-register.json)
REFRESH=$(jq -r .refreshToken /tmp/auth-register.json)

curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"smoke@test.com","password":"password123"}'

curl -s -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH\"}"
```

Expected: register → HTTP 201 with accessToken/refreshToken/user; login → 200; refresh → 200 with a different refreshToken. Check the API launch port in `API/Properties/launchSettings.json` and use that instead of 5000 if different.

- [ ] **Step 7: Commit (skip if no git repo)**

```bash
git add API/appsettings.json API/DTOs/AuthDtos.cs API/Program.cs
git commit -m "feat: add register, login, and refresh auth endpoints"
```
