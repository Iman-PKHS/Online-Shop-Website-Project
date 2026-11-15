using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;

namespace OnlineShop.Api.Helpers;

public static class OrderViews
{
    public static async Task<Dictionary<int, List<OrderLineResponse>>> LoadLinesAsync(
        AppDbContext db, List<int> orderIds)
    {
        var rows = await db.OrderItems
            .AsNoTracking()
            .Where(oi => orderIds.Contains(oi.OrderId))
            .OrderBy(oi => oi.Id)
            .Select(oi => new
            {
                oi.OrderId,
                oi.ItemId,
                oi.ItemNameSnapshot,
                oi.UnitPriceSnapshot,
                oi.Quantity,
                oi.LineTotal,
                ImageUrl = oi.Item.ImageUrl
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.OrderId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new OrderLineResponse
                {
                    ItemId = r.ItemId,
                    Name = r.ItemNameSnapshot,
                    ImageUrl = r.ImageUrl,
                    UnitPrice = r.UnitPriceSnapshot,
                    Quantity = r.Quantity,
                    LineTotal = r.LineTotal
                }).ToList());
    }
}