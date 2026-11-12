using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
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
}