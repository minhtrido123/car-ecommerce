namespace Models;

public class RefreshToken : EntityBase
{
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
