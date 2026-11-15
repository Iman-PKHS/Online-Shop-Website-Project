namespace OnlineShop.Api.DTOs;

public class AdminOrderSummary
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public long TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string PostalCode { get; set; } = string.Empty;
}

public class AdminPaymentInfo
{
    public int PaymentId { get; set; }
    public Guid ReferenceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }
}

public class AdminOrderDetail : AdminOrderSummary
{
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool AddressChanged { get; set; }
    public DateTime? AddressChangedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public List<OrderLineResponse> Lines { get; set; } = new();
    public List<AdminPaymentInfo> Payments { get; set; } = new();
}