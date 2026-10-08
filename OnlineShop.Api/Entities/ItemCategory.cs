namespace OnlineShop.Api.Entities;

public class ItemCategory
{
    //Properties
    public int ItemId { get; set;}
    public int CategoryId { get; set;}
    //
    
    //Navigations
    public Item Item { get; set; } = null!;
    public Category Category { get; set; } = null!;
    //
}