namespace OnlineShop.Api.Entities;

public class Category
{
    //Properties
    public int Id { get; set;}
    public string Name { set; get;} = string.Empty;
    public bool Isactive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    //Navigations
    public List<ItemCategory> ItemCategories { get; set; } = new();
    //
}