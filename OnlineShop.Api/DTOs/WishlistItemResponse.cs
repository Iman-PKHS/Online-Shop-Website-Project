namespace OnlineShop.Api.DTOs;

public class WishlistItemResponse
{
    public int WishlistItemId { get; set; }
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public long Price { get; set; }
    public bool IsAvailable { get; set; }
    public string? Message { get; set; }
    public int? StockLeft { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public bool Purchased { get; set; }
    public bool? PurchasedManually { get; set; }
}