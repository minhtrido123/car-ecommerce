namespace Models;

public class CarModel : EntityBase
{
    public Guid BrandId { get; set; }
    public required string Name { get; set; }
    public int? YearStart { get; set; }
    public int? YearEnd { get; set; }
}