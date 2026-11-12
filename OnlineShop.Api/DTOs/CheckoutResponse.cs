namespace OnlineShop.Api.DTOs;

public class CheckoutResponse
{
    public int OrderId { get; set; }
    public Guid ReferenceId { get; set; }
    public long Amount { get; set; }
    public string FakeGatewayUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsResumed { get; set; }
}