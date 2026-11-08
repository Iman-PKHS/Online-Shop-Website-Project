namespace OnlineShop.Api.DTOs;

public class CartItemResponse
{
    public int CartItemId { get; set; }
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    public long UnitPrice { get; set; }
    public long PriceAtAdd { get; set; } //snapshot
    public bool PriceChanged { get; set; }

    public int Quantity { get; set; }
    public long LineTotal { get; set; } //UnitPrice * Quantity

    public bool IsAvailable { get; set; }
    public string? Message { get; set; }
    public int? AvailableQuantity { get; set; } //when stock < quantity
    public int? StockLeft { get; set; } //when stock is 1 to 9

}