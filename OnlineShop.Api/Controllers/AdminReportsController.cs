using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities.Enums;

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

    [HttpGet("paid-orders")]
    public async Task<IActionResult> PaidOrders([FromQuery] PagingQuery paging, [FromQuery] OrderStatus? status)
    {
        var wanted = status ?? OrderStatus.Paid;

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Status == wanted);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(o => new PaidOrderReportItem
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                CustomerId = o.UserId,
                Username = o.User.Username,
                Email = o.User.Email,
                PostalCode = o.ShippingPostalCode,
                Address = o.ShippingAddress,
                AddressChanged = o.ShippingAddressChangedAt != null,
                AddressChangedAt = o.ShippingAddressChangedAt,
                Lines = o.OrderItems.Select(oi => new PaidOrderLine
                {
                    ItemId = oi.ItemId,
                    Name = oi.ItemNameSnapshot,
                    UnitPrice = oi.UnitPriceSnapshot,
                    Quantity = oi.Quantity,
                    LineTotal = oi.LineTotal
                }).ToList()
            })
            .ToListAsync();

        var orderIds = items.Select(i => i.OrderId).ToList();
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId) && p.Status == PaymentStatus.Paid)
            .Select(p => new { p.OrderId, p.PaidAt, p.ReferenceId })
            .ToListAsync();

        foreach (var item in items)
        {
            var pay = payments.FirstOrDefault(p => p.OrderId == item.OrderId);
            item.PaidAt = pay?.PaidAt;
            item.PaymentReferenceId = pay?.ReferenceId;
        }

        return Ok(new PagedResult<PaidOrderReportItem>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
}