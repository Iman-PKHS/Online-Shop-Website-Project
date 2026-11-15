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
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderLifecycleService _lifecycle;

    public OrdersController(AppDbContext db, IOrderLifecycleService lifecycle)
    {
        _db = db;
        _lifecycle = lifecycle;
    }

    //// Get Order for customer
    [HttpGet("my")]
    public async Task<IActionResult> My([FromQuery] PagingQuery paging, [FromQuery] OrderStatus? status)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        await _lifecycle.ExpireOverdueAsync(uid);

        var query = _db.Orders.AsNoTracking().Where(o => o.UserId == uid);

        if (status.HasValue)
        {
            var wanted = status.Value;
            query = query.Where(o => o.Status == wanted);
        }

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(o => o.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(o => new
            {
                o.Id, o.OrderDate, o.TotalAmount, o.Status, o.PaymentStatus,
                o.ExpiresAt, o.CancelledAt
            })
            .ToListAsync();

        var ids = rows.Select(r => r.Id).ToList();

        var lines = await OrderViews.LoadLinesAsync(_db, ids);

        var failed = await _db.Payments
            .AsNoTracking()
            .Where(p => ids.Contains(p.OrderId) && p.Status == PaymentStatus.Failed)
            .OrderBy(p => p.Id)
            .Select(p => new { p.OrderId, p.FailureReason })
            .ToListAsync();

        var reasons = failed
            .GroupBy(f => f.OrderId)
            .ToDictionary(g => g.Key, g => g.Last().FailureReason); 

        var now = DateTime.UtcNow;

        var items = rows.Select(r =>
        {
            var orderLines = lines.GetValueOrDefault(r.Id) ?? new List<OrderLineResponse>();
            var open = r.Status == OrderStatus.Pending && r.ExpiresAt > now;

            return new OrderSummaryResponse
            {
                OrderId = r.Id,
                OrderDate = r.OrderDate,
                TotalAmount = r.TotalAmount,
                Status = r.Status.ToString(),
                PaymentStatus = r.PaymentStatus.ToString(),
                ItemCount = orderLines.Sum(l => l.Quantity),
                Preview = orderLines.Take(3).ToList(),
                ExpiresAt = open ? r.ExpiresAt : null,
                CanPay = open,
                CanCancel = r.Status == OrderStatus.Pending,
                CancelledAt = r.CancelledAt,
                CancelReason = r.Status == OrderStatus.Cancelled ? reasons.GetValueOrDefault(r.Id) : null
            };
        }).ToList();

        return Ok(new PagedResult<OrderSummaryResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////Get Order by Id
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetById(int orderId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        await _lifecycle.ExpireOverdueAsync(uid);

        var o = await _db.Orders
            .AsNoTracking()
            .Where(x => x.Id == orderId && x.UserId == uid)
            .Select(x => new
            {
                x.Id, x.OrderDate, x.TotalAmount, x.Status, x.PaymentStatus,
                x.ExpiresAt, x.CancelledAt, x.ShippedAt, x.DeliveredAt,
                x.ShippingPostalCode, x.ShippingAddress, x.ShippingAddressChangedAt
            })
            .FirstOrDefaultAsync();

        if (o is null)
            return NotFound(new { message = "سفارش پیدا نشد." });

        var lines = (await OrderViews.LoadLinesAsync(_db, new List<int> { orderId }))
            .GetValueOrDefault(orderId) ?? new List<OrderLineResponse>();

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Status, p.PaidAt, p.ReferenceId, p.FailureReason })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var open = o.Status == OrderStatus.Pending && o.ExpiresAt > now;
        var pendingPay = payments.LastOrDefault(p => p.Status == PaymentStatus.Pending);
        var paid = payments.LastOrDefault(p => p.Status == PaymentStatus.Paid);
        var failed = payments.LastOrDefault(p => p.Status == PaymentStatus.Failed);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new OrderDetailResponse
        {
            OrderId = o.Id,
            OrderDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            ItemCount = lines.Sum(l => l.Quantity),
            Preview = lines.Take(3).ToList(),
            Lines = lines,

            ExpiresAt = open ? o.ExpiresAt : null,
            CanPay = open,
            CanCancel = o.Status == OrderStatus.Pending,
            CancelledAt = o.CancelledAt,
            CancelReason = o.Status == OrderStatus.Cancelled ? failed?.FailureReason : null,

            ShippingPostalCode = o.ShippingPostalCode,
            ShippingAddress = o.ShippingAddress,
            ShippingAddressChangedAt = o.ShippingAddressChangedAt,

            PaidAt = paid?.PaidAt,
            ShippedAt = o.ShippedAt,
            DeliveredAt = o.DeliveredAt,

            ReferenceId = open ? pendingPay?.ReferenceId : null,
            FakeGatewayUrl = open && pendingPay is not null
                ? $"{baseUrl}/fake-gateway/{pendingPay.ReferenceId}"
                : null
        });
    }
    //

    ////Order cancellation
    [HttpPost("{orderId:int}/cancel")]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId.Value)
            .Select(o => new { o.Status })
            .FirstOrDefaultAsync();

        if (order is null)
            return NotFound(new { message = "سفارش پیدا نشد." });

        if (order.Status == OrderStatus.Cancelled)
            return Ok(new { message = "این سفارش قبلاً لغو شده است." });

        if (order.Status != OrderStatus.Pending)
            return Conflict(new { message = "سفارش پرداخت شده قابل لغو نیست." });

        var cancelled = await _lifecycle.CancelPendingOrderAsync(orderId, "توسط مشتری لغو شد.");

        return cancelled
            ? Ok(new { message = "سفارش لغو شد." })
            : Conflict(new { message = "وضعیت سفارش تغییر کرد. دوباره بررسی کنید." });
    }
    //
}