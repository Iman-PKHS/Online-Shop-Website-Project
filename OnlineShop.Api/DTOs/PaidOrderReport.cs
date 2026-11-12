namespace OnlineShop.Api.DTOs;

public class PaidOrderLine
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
}

public class PaidOrderReportItem
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? PaymentReferenceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long TotalAmount { get; set; }

    public int CustomerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool AddressChanged { get; set; }
    public DateTime? AddressChangedAt { get; set; }
    public List<PaidOrderLine> Lines { get; set; } = new();
}