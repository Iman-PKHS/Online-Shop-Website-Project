namespace OnlineShop.Api.DTOs;

public class CategoryWithItems
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PagedResult<ItemSummary> Items { get; set; } = new();
}