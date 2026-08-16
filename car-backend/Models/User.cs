namespace Models;

public class User : EntityBase
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public required string Name { get; set; }
}