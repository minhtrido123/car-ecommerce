namespace Models;

public class Favorite : EntityBase
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}
