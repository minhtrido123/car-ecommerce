namespace Models;

public class Part : Product
{
    public required string Name { get; set; }
    public string? Brand { get; set; }
    public string? Sku { get; set; }
}
