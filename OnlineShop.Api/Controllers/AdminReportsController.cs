using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private const int MaxPerGroup = 50;
    private readonly AppDbContext _db;

    public AdminReportsController(AppDbContext db) => _db = db;

    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock()
    {
        var report = new LowStockReport
        {
            OutOfStock = await BuildGroup(i => i.StockQuantity == 0),
            Critical   = await BuildGroup(i => i.StockQuantity >= 1 && i.StockQuantity <= 10),
            Low        = await BuildGroup(i => i.StockQuantity >= 11 && i.StockQuantity <= 20)
        };
        return Ok(report);
    }

    private async Task<StockReportGroup> BuildGroup(
        System.Linq.Expressions.Expression<Func<Entities.Item, bool>> filter)
    {
        var query = _db.Items.AsNoTracking().Where(filter);

        return new StockReportGroup
        {
            TotalCount = await query.CountAsync(),
            Items = await query
                .OrderBy(i => i.StockQuantity).ThenBy(i => i.Id)
                .Take(MaxPerGroup)
                .Select(i => new StockReportItem
                {
                    Id = i.Id, Name = i.Name,
                    StockQuantity = i.StockQuantity, IsActive = i.IsActive
                })
                .ToListAsync()
        };
    }
}