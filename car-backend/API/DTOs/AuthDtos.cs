namespace API.DTOs;

public record RegisterRequest(string Email, string Password, string Name, bool RememberMe);

public record LoginRequest(string Email, string Password, bool RememberMe);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User);
