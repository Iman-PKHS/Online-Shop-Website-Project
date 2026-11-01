namespace OnlineShop.Api.DTOs;

public class ItemSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool InStock { get; set; }
    public double? AverageRating { get; set; }
    public List<CategorySummary> Categories { get; set; } = new();
}