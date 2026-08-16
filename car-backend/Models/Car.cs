namespace Models;

public class Car : Product
{
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public int Year { get; set; }
    public int? Mileage { get; set; }
    public string? Color { get; set; }
    public Brand? Brand { get; set; }
    public CarModel? Model { get; set; }
}
