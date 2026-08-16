namespace Models;

public class Brand : EntityBase
{
    public required string Name { get; set; }
    public string? Country { get; set; }
}