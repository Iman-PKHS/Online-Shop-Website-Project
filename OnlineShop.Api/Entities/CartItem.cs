namespace OnlineShop.Api.Entities;

public class CartItem
{
    //Properties
    public int Id { get; set;}
    public int CartId { get; set;}
    public int ItemId { get; set;}
    public int Quantity { get; set;}
    public long UnitPriceAtAdd { get; set; }
    //
    
    //Navigations
    public Item Item { get; set; } = null!;
    public Cart Cart { get; set;} = null!;
    //
}