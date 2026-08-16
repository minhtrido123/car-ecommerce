namespace Models;

public class Product : EntityBase
{
    public string ProductType { get; set; } = "Car";
    public Guid? SellerId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = "Active";
    public string? Description { get; set; }
    public string? Specs { get; set; }
    public Category? Category { get; set; }
    public User? Seller { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
