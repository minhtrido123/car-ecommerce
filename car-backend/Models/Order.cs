namespace Models;

public class Order : EntityBase
{
    public Guid BuyerId { get; set; }
    public required string Status { get; set; }
    public decimal TotalAmount { get; set; }
}