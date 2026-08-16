namespace API.DTOs;

public record UserResponse(Guid Id, string Email, string Role, string Name, DateTime CreatedAt);

public record CreateUserRequest(string Email, string PasswordHash, string Role, string Name);

public record UpdateUserRequest(Guid Id, string Email, string Role, string Name);

public record CreateAdminRequest(string Email, string Password, string Name, string Role);

public record UpdateAdminRequest(string Email, string Name, string Role, string? Password);