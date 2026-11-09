namespace OnlineShop.Api.DTOs;

public class CheckoutProblem
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool RemovedFromCart { get; set; }
    public int? AvailableQuantity { get; set; }
}