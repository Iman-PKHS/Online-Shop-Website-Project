using System.Linq.Expressions;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;

namespace OnlineShop.Api.Helpers;

public static class ItemMappings
{
    public static readonly Expression<Func<Item, ItemSummary>> ToSummary = i => new ItemSummary
    {
        Id = i.Id,
        Name = i.Name,
        Price = i.Price,
        ImageUrl = i.ImageUrl,
        InStock = i.StockQuantity > 0,
        StockLeft = i.StockQuantity > 0 && i.StockQuantity < 10 ? i.StockQuantity : (int?)null,
        AverageRating = i.Ratings.Average(r => (double?)r.Stars),
        Categories = i.ItemCategories
            .Where(ic => ic.Category.IsActive)
            .Select(ic => new CategorySummary { Id = ic.Category.Id, Name = ic.Category.Name })
            .ToList()
    };
}