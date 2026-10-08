namespace OnlineShop.Api.Entities;

public class OrderItem
{
    //Properties
    public int Id { get; set;}
    public int OrderId { get; set;}
    public int ItemId { get; set;}
    public string ItemNameSnapshot { get; set;} = string.Empty;
    public long UnitPriceSnapshot { get; set;}
    public int Quantity { get; set;}
    public long LineTotal { get; set;}
    //
    
    //Navigations
    public Item Item { get; set; } = null!;
    public Order Order { get; set;} = null!;
    //
}