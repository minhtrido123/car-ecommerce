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
