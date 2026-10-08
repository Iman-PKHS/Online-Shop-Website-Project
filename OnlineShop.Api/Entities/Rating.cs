namespace OnlineShop.Api.Entities;

public class Rating
{
    //Properties
    public int Id { get; set;}
    public int ItemId { get; set;}
    public int UserId { get; set;}
    public int Stars { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    //
    
    //Navigations
    public Item Item { get; set; } = null!;
    public User User{ get; set;} = null!;
    //
}