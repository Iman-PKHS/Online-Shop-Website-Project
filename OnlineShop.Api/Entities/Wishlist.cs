namespace OnlineShop.Api.Entities;

public class Wishlist
{
    //Properties
    public int Id { get; set;}
    public int UserId { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    //
    
    //Navigations
    public User User { get; set; } = null!;
    public List<WishlistItem> WishlistItems { get; set;} = new();
    //
}