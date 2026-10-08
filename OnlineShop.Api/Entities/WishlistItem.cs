namespace OnlineShop.Api.Entities;

public class WishlistItem
{
    //Properties
    public int Id { get; set;}
    public int WishlistId { get; set;}
    public int ItemId { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    //
    
    //Navigations
    public Wishlist Wishlist { get; set; } = null!;
    public Item Item { get; set;} = new();
    //
}