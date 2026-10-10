namespace OnlineShop.Api.Entities;

public class Item
{
    //Properties
    public int Id { get; set;}
    public string Name { set; get;} = string.Empty;
    public string Description { get; set;} = string.Empty;
    public long Price { get; set;}
    public int StockQuantity { get; set;}
    public string ImageUrl { get; set;} = string.Empty;
    public bool Isactive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    //
    
    //Navigations
    public List<ItemCategory> ItemCategories { get; set; } = new();
    public List<Rating> Ratings { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    //
}