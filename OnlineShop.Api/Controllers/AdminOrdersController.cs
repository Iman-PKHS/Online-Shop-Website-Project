using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities.Enums;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderLifecycleService _lifecycle;
    private readonly ILogger<AdminUsersController> _log;

    public AdminOrdersController(
        AppDbContext db,
        IOrderLifecycleService lifecycle,
        ILogger<AdminUsersController> log)
    {
        _db = db;
        _lifecycle = lifecycle;
        _log = log;
    }

    //// Get order
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagingQuery paging,
        [FromQuery] OrderStatus? status,
        [FromQuery] int? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            var s = status.Value;
            query = query.Where(o => o.Status == s);
        }

        if (userId.HasValue)
        {
            var u = userId.Value;
            query = query.Where(o => o.UserId == u);
        }

        if (from.HasValue)
        {
            var f = from.Value.Date;
            query = query.Where(o => o.OrderDate >= f);
        }

        if (to.HasValue)
        {
            var t = to.Value.Date.AddDays(1);
            query = query.Where(o => o.OrderDate < t);
        }

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(o => o.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(o => new AdminOrderSummary
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                UserId = o.UserId,
                Username = o.User.Username,
                PostalCode = o.ShippingPostalCode
            })
            .ToListAsync();

        var ids = rows.Select(r => r.OrderId).ToList();

        var counts = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => ids.Contains(oi.OrderId))
            .GroupBy(oi => oi.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Sum(x => x.Quantity) })
            .ToListAsync();

        var countById = counts.ToDictionary(c => c.OrderId, c => c.Count);
        foreach (var r in rows)
            r.ItemCount = countById.GetValueOrDefault(r.OrderId);

        return Ok(new PagedResult<AdminOrderSummary>
        {
            Items = rows,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    //// Get order by id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var o = await _db.Orders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminOrderDetail
            {
                OrderId = x.Id,
                OrderDate = x.OrderDate,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString(),
                PaymentStatus = x.PaymentStatus.ToString(),
                UserId = x.UserId,
                Username = x.User.Username,
                Email = x.User.Email,
                PostalCode = x.ShippingPostalCode,
                Address = x.ShippingAddress,
                AddressChanged = x.ShippingAddressChangedAt != null,
                AddressChangedAt = x.ShippingAddressChangedAt,
                ExpiresAt = x.ExpiresAt,
                CancelledAt = x.CancelledAt,
                ShippedAt = x.ShippedAt,
                DeliveredAt = x.DeliveredAt
            })
            .FirstOrDefaultAsync();

        if (o is null)
            return NotFound(new { message = "سفارش پیدا نشد." });

        var lines = await OrderViews.LoadLinesAsync(_db, new List<int> { id });
        o.Lines = lines.GetValueOrDefault(id) ?? new List<OrderLineResponse>();
        o.ItemCount = o.Lines.Sum(l => l.Quantity);

        o.Payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == id)
            .OrderBy(p => p.Id)
            .Select(p => new AdminPaymentInfo
            {
                PaymentId = p.Id,
                ReferenceId = p.ReferenceId,
                Status = p.Status.ToString(),
                Amount = p.Amount,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                FailureReason = p.FailureReason
            })
            .ToListAsync();

        return Ok(o);
    }
    //

    ////Put status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status.Trim(), ignoreCase: true, out var target)
            || !Enum.IsDefined(target))
            return BadRequest(new { message = "وضعیت نامعتبر است. مقدارهای مجاز: Shipped, Delivered, Cancelled." });

        var current = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new { o.Status })
            .FirstOrDefaultAsync();

        if (current is null)
            return NotFound(new { message = "سفارش پیدا نشد." });

        var now = DateTime.UtcNow;
        int changed;

        switch (target)
        {
            case OrderStatus.Shipped:
                changed = await _db.Orders
                    .Where(o => o.Id == id && o.Status == OrderStatus.Paid)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.Status, OrderStatus.Shipped)
                        .SetProperty(o => o.ShippedAt, (DateTime?)now));
                break;

            case OrderStatus.Delivered:
                changed = await _db.Orders
                    .Where(o => o.Id == id && o.Status == OrderStatus.Shipped)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.Status, OrderStatus.Delivered)
                        .SetProperty(o => o.DeliveredAt, (DateTime?)now));
                break;

            case OrderStatus.Cancelled:
                changed = await _lifecycle.CancelPendingOrderAsync(id, "توسط فروشنده لغو شد.") ? 1 : 0;
                break;

            default:   // Pending or Paid
                return Conflict(new { message = "وضعیت «در انتظار» و «پرداخت شده» فقط توسط سیستم پرداخت تغییر می‌کند." });
        }

        if (changed == 0)
        {
            var message = target switch
            {
                OrderStatus.Shipped => "این سفارش قبلا ارسال شده است.",
                OrderStatus.Delivered => "فقط سفارش ارسال شده را می‌توان تحویل شده کرد.",
                _ => "فقط سفارش در انتظار پرداخت قابل لغو است؛ سفارش پرداخت شده، باز پرداخت ندارد."
            };

            return Conflict(new
            {
                message,
                currentStatus = (await _db.Orders.AsNoTracking()
                    .Where(o => o.Id == id).Select(o => o.Status).FirstAsync()).ToString()
            });
        }

        _log.LogInformation("Admin changed order {OrderId} to {Status}", id, target);

        return await GetById(id);
    }
    //
}