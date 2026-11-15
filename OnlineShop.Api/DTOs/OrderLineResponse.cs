namespace OnlineShop.Api.DTOs;

public class OrderLineResponse
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
}