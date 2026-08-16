namespace Models;

public class Review : EntityBase
{
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
