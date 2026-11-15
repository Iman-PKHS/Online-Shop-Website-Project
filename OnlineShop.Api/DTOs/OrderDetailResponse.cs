namespace OnlineShop.Api.DTOs;

public class OrderDetailResponse : OrderSummaryResponse
{
    public List<OrderLineResponse> Lines { get; set; } = new();

    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime? ShippingAddressChangedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? ReferenceId { get; set; } //for resuming pending order by customer
    public string? FakeGatewayUrl { get; set; }
}