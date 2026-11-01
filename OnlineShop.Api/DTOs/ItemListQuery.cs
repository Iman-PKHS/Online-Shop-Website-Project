namespace OnlineShop.Api.DTOs;

public class ItemListQuery : PagingQuery
{
    public string? Search { get; set; }
    public string? CategoryIds { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public string? Sort { get; set; }
}