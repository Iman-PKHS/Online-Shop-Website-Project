namespace OnlineShop.Api.DTOs;

public class ItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CategorySummary> Categories { get; set; } = new();
}