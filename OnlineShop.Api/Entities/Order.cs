using OnlineShop.Api.Entities.Enums;

namespace OnlineShop.Api.Entities;

public class Order
{
    //Properties
    public int Id { get; set;}
    public int UserId { get; set;}
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public long TotalAmount { get; set;}
    public OrderStatus Status { get; set;}
    public PaymentStatus PaymentStatus { get; set;}
    //
    
    //Navigations
    public User User { get; set; } = null!;
    public List<OrderItem> OrderItems { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    //
}