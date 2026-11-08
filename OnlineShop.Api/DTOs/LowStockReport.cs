namespace OnlineShop.Api.DTOs;

public class StockReportItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class StockReportGroup
{
    public int TotalCount { get; set; }
    public List<StockReportItem> Items { get; set; } = new();
}

public class LowStockReport
{
    public StockReportGroup OutOfStock { get; set; } = new(); // 0
    public StockReportGroup Critical { get; set; } = new(); // under 10
    public StockReportGroup Low { get; set; } = new(); // under 20
}