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
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderLifecycleService _lifecycle;

    public AdminUsersController(AppDbContext db, IOrderLifecycleService lifecycle)
    {
        _db = db;
        _lifecycle = lifecycle;
    }


    ////Get
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagingQuery paging,
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        var term = string.IsNullOrWhiteSpace(search) ? null : TextNormalizer.Normalize(search);
        if (term is not null)
        {
            var pattern = $"%{term}%";
            query = query.Where(u => EF.Functions.Like(u.Username, pattern)
                                || EF.Functions.Like(u.Email, pattern));
        }

        if (role.HasValue)
        {
            var r = role.Value;
            query = query.Where(u => u.Role == r);
        }

        if (isActive.HasValue)
        {
            var a = isActive.Value;
            query = query.Where(u => u.IsActive == a);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(u => new AdminUserSummary
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AdminUserSummary>
        {
            Items = items, Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }
    //
    
    ////Detailed Get
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var u = await _db.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminUserDetail
            {
                Id = x.Id, Username = x.Username, Email = x.Email,
                Role = x.Role.ToString(), IsActive = x.IsActive, CreatedAt = x.CreatedAt,
                PostalCode = x.PostalCode, Address = x.Address, AddressUpdatedAt = x.AddressUpdatedAt
            })
            .FirstOrDefaultAsync();

        if (u is null)
            return NotFound(new { message = "کاربر پیدا نشد." });

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == id)
            .Select(o => new { o.Status, o.TotalAmount, o.OrderDate })
            .ToListAsync();

        u.OrdersCount = orders.Count;
        u.PendingOrdersCount = orders.Count(o => o.Status == OrderStatus.Pending);
        u.PaidOrdersCount = orders.Count(o => o.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered);
        u.TotalSpent = orders
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            .Sum(o => o.TotalAmount);
        u.LastOrderDate = orders.Count == 0 ? null : orders.Max(o => o.OrderDate);

        u.CommentsCount = await _db.Comments.CountAsync(c => c.UserId == id);
        u.RatingsCount = await _db.Ratings.CountAsync(r => r.UserId == id);

        return Ok(u);
    }
    //

    //// Put Disable user
    [HttpPut("{id:int}/disable")]
    public async Task<IActionResult> Disable(int id, [FromBody] DisableUserRequest? request)
    {
        var confirm = request?.Confirm ?? false;
        var me = User.GetUserId();
        if (me == id)
            return Conflict(new { message = "نمی‌توانید حساب خودتان را غیرفعال کنید." });

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Role, u.IsActive })
            .FirstOrDefaultAsync();

        if (user is null)
            return NotFound(new { message = "کاربر پیدا نشد." });

        if (!user.IsActive)
            return Ok(new { message = "این کاربر از قبل غیرفعال است." });

        if (user.Role == UserRole.Admin)
        {
            var otherActiveAdmins = await _db.Users
                .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id);

            if (otherActiveAdmins == 0)
                return Conflict(new { message = "آخرین مدیر فعال را نمی‌توان غیرفعال کرد." });
        }

        var unfinished = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == id
                    && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped))
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .ToListAsync();

        if (unfinished.Count > 0 && !confirm)
        {
            var pendingCount = await _db.Orders
                .CountAsync(o => o.UserId == id && o.Status == OrderStatus.Pending);

            return Conflict(new
            {
                requiresConfirm = true,
                message = $"این کاربر {unfinished.Count} سفارش پرداخت شده و تحویل نشده دارد. با غیرفعال‌سازی، او دیگر نمی‌تواند وارد حساب شود و وضعیت سفارش‌هایش را ببیند. برای ادامه confirm را true بفرستید.",
                paidUnfinishedOrdersCount = unfinished.Count,
                pendingOrdersCount = pendingCount,
                orderIds = unfinished
            });
        }

        await _db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));

        var pendingIds = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == id && o.Status == OrderStatus.Pending)
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .ToListAsync();

        foreach (var orderId in pendingIds)
            await _lifecycle.CancelPendingOrderAsync(orderId, "حساب کاربری توسط مدیر غیرفعال شد.");

        return Ok(new
        {
            message = "کاربر غیرفعال شد.",
            cancelledPendingOrders = pendingIds.Count,
            paidUnfinishedOrders = unfinished.Count
        });
    }
    //

    ////Put Enable User
    [HttpPut("{id:int}/enable")]
    public async Task<IActionResult> Enable(int id)
    {
        var updated = await _db.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, true));

        if (updated == 0)
            return NotFound(new { message = "کاربر پیدا نشد." });

        return Ok(new { message = "کاربر فعال شد." });
    }
    //

}