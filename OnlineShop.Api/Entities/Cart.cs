namespace OnlineShop.Api.Entities;

public class Cart
{
    //Properties
    public int Id { get; set;}
    public int UserId { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    //
    
    //Navigations
    public User User { get; set; } = null!;
    public List<CartItem> CartItems { get; set; } = new();
    //
}