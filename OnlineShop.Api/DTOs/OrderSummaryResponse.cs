namespace OnlineShop.Api.DTOs;

public class OrderSummaryResponse
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public long TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public List<OrderLineResponse> Preview { get; set; } = new();
    public DateTime? ExpiresAt { get; set; } 
    public bool CanPay { get; set; }
    public bool CanCancel { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}