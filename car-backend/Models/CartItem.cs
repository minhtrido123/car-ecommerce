namespace Models;

public class CartItem : EntityBase
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
