namespace Models;

public class MenuItem : EntityBase
{
    public required string Label { get; set; }
    public required string Url { get; set; }
    public string? Icon { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
