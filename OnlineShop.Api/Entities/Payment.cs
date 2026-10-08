using OnlineShop.Api.Entities.Enums;

namespace OnlineShop.Api.Entities;

public class Payment
{
    //Properties
    public int Id { get; set;}
    public int OrderId { get; set;}
    public Guid ReferenceId { get; set; } = Guid.NewGuid();
    public long Amount { get; set;}
    public PaymentStatus Status { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set;}
    //

    //Navigations
    public Order Order { get; set;} = null!;
    //
}