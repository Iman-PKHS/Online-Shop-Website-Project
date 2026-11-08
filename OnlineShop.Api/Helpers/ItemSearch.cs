using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Entities;

namespace OnlineShop.Api.Helpers;

public static class ItemSearch
{
    public static IQueryable<Item> ApplySearch(
        this IQueryable<Item> query, string? text, bool activeCategoriesOnly)
    {
        var words = (text ?? string.Empty)
            .Split(new[] { ' ', ',', '،', '\u200C' },
                   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(TextNormalizer.Normalize)
            .Distinct()
            .Take(5)
            .ToList();

        foreach (var word in words)
        {
            var pattern = $"%{word}%";

            query = activeCategoriesOnly
                ? query.Where(i =>
                    EF.Functions.Like(i.Name, pattern) ||
                    i.ItemCategories.Any(ic => ic.Category.IsActive &&
                                               EF.Functions.Like(ic.Category.Name, pattern)))
                : query.Where(i =>
                    EF.Functions.Like(i.Name, pattern) ||
                    i.ItemCategories.Any(ic => EF.Functions.Like(ic.Category.Name, pattern)));
        }

        return query;
    }
}