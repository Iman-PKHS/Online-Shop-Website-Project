namespace OnlineShop.Api.DTOs;

public class PaymentResultResponse
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}