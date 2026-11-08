namespace OnlineShop.Api.DTOs;

public class CartResponse
{
    public List<CartItemResponse> Lines { get; set; } = new();
    public int ItemCount { get; set; }
    public long TotalAmount { get; set; }
    public bool HasUnavailableItems { get; set; }
}