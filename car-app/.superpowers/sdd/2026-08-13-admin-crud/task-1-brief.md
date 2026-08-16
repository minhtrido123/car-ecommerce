### Task 1: Backend — AdminService (create/update admin users with bcrypt)

**Files:**
- Create: `car-backend/Services/AdminService.cs`
- Test: `car-backend/Tests/AdminServiceTests.cs`

**Interfaces:**
- Consumes: `IRepository<User>` (`GetFilteredAsync(Expression<Func<User,bool>>, ct)`, `GetByIdAsync(Guid, ct)`, `AddAsync(User, ct)`, `UpdateAsync(User, ct)`), `EmailAlreadyExistsException`, `BCrypt.Net.BCrypt` (all already in the solution).
- Produces: `Services.IAdminService` with `CreateAdminAsync(string email, string password, string name, string role, CancellationToken) : Task<User>` and `UpdateAdminAsync(Guid id, string email, string name, string role, string? password, CancellationToken) : Task<User>`.

- [ ] **Step 1: Write the failing tests**

Create `car-backend/Tests/AdminServiceTests.cs`:

```csharp
using Infrastructure;
using Models;
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run (from `car-backend`): `dotnet test`
Expected: FAIL — `AdminService` type not found.

- [ ] **Step 3: Write minimal implementation**

Create `car-backend/Services/AdminService.cs`:

```csharp
namespace Services;

public interface IAdminService
{
    Task<User> CreateAdminAsync(string email, string password, string name, string role, CancellationToken cancellationToken = default);
    Task<User> UpdateAdminAsync(Guid id, string email, string name, string role, string? password, CancellationToken cancellationToken = default);
}

public class AdminService : IAdminService
{
    private readonly IRepository<User> _userRepository;

    public AdminService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> CreateAdminAsync(string email, string password, string name, string role, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetFilteredAsync(u => u.Email == email, cancellationToken);
        if (existing.Count > 0) throw new EmailAlreadyExistsException();

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Name = name
        };
        return await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<User> UpdateAdminAsync(Guid id, string email, string name, string role, string? password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User {id} not found");

        var taken = await _userRepository.GetFilteredAsync(u => u.Email == email && u.Id != id, cancellationToken);
        if (taken.Count > 0) throw new EmailAlreadyExistsException();

        user.Email = email;
        user.Name = name;
        user.Role = role;
        if (!string.IsNullOrWhiteSpace(password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
        return await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run (from `car-backend`): `dotnet test`
Expected: PASS — all 6 AdminServiceTests green.

- [ ] **Step 5: Commit (backend repo only)**

```bash
cd car-backend
git add Services/AdminService.cs Tests/AdminServiceTests.cs
git commit -m "feat: add AdminService with bcrypt-hashed admin user management"
```

---

