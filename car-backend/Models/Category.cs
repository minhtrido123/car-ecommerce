namespace Models;

public class Category : EntityBase
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string Type { get; set; } = "Car";
}
