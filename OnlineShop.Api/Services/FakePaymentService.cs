using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Entities.Enums;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Services;

public class FakePaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IOrderLifecycleService _lifecycle;
    private readonly CheckoutSettings _settings;

    public FakePaymentService(
        AppDbContext db,
        IOrderLifecycleService lifecycle,
        IOptions<CheckoutSettings> options)
    {
        _db = db;
        _lifecycle = lifecycle;
        _settings = options.Value;
    }

    public async Task<InitiateResult> InitiateAsync(int userId, string baseUrl)
    {
        await _lifecycle.ExpireOverdueAsync(userId);

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.PostalCode, u.Address })
            .FirstAsync();

        if (string.IsNullOrWhiteSpace(user.PostalCode) || string.IsNullOrWhiteSpace(user.Address))
            return new InitiateResult { Outcome = InitiateOutcome.AddressRequired };

        var lines = await _db.CartItems
            .Include(ci => ci.Item)
            .Where(ci => ci.Cart.UserId == userId)
            .OrderBy(ci => ci.Id)
            .ToListAsync();

        if (lines.Count == 0)
            return new InitiateResult { Outcome = InitiateOutcome.CartEmpty };

        var now = DateTime.UtcNow;

        //// resume with order
        var pending = await _db.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Payments)
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending && o.ExpiresAt > now)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();

        if (pending is not null && IsSameAsCart(pending, lines, user.PostalCode, user.Address))
        {
            var open = pending.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
            if (open is not null)
                return Created(pending, open, baseUrl, resumed: true);
        }
        //

        //// pre check
        var own = await CartRules.GetOwnReservedAsync(_db, userId);
        var problems = new List<CheckoutProblem>();
        var toRemove = new List<CartItem>();

        foreach (var line in lines)
        {
            var item = line.Item;
            var available = item.StockQuantity - item.ReservedQuantity + own.GetValueOrDefault(item.Id);

            if (!item.IsActive || item.StockQuantity == 0)
            {
                //// really gone, remove from the cart and report
                problems.Add(new CheckoutProblem
                {
                    ItemId = item.Id, Name = item.Name,
                    Message = CartRules.UnavailableMessage, RemovedFromCart = true
                });
                toRemove.Add(line);
            }
            else if (available < line.Quantity)
            {
                //// reserved by others, report, keep in the cart
                problems.Add(new CheckoutProblem
                {
                    ItemId = item.Id, Name = item.Name,
                    Message = available <= 0 ? CartRules.UnavailableMessage : CartRules.OnlyLeftMessage(available),
                    AvailableQuantity = Math.Max(available, 0)
                });
            }
        }

        if (problems.Count > 0)
        {
            if (toRemove.Count > 0)
            {
                _db.CartItems.RemoveRange(toRemove);
                await _db.SaveChangesAsync();
            }
            return new InitiateResult { Outcome = InitiateOutcome.ProblemsFound, Problems = problems };
        }

        ////release the old order, reserve atomically, create the new order
        await using var tx = await _db.Database.BeginTransactionAsync();

        var oldIds = await _db.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending)
            .Select(o => o.Id)
            .ToListAsync();

        foreach (var oldId in oldIds)
            await _lifecycle.CancelPendingOrderAsync(oldId, "با تلاش جدید جایگزین شد.");

        foreach (var l in lines)
        {
            var itemId = l.ItemId;
            var qty = l.Quantity;

            var reserved = await _db.Items
                .Where(i => i.Id == itemId && i.IsActive && i.StockQuantity - i.ReservedQuantity >= qty)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity + qty));

            if (reserved == 0)
            {
                await tx.RollbackAsync();   ////restores the old order and its reservation
                return new InitiateResult
                {
                    Outcome = InitiateOutcome.ProblemsFound,
                    Problems = await FreshShortfallsAsync(userId, lines)
                };
            }
        }

        var order = new Order
        {
            UserId = userId,
            OrderDate = now,
            ExpiresAt = now.AddHours(_settings.ReservationHours),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            ShippingPostalCode = user.PostalCode,
            ShippingAddress = user.Address,
            OrderItems = lines.Select(l => new OrderItem
            {
                ItemId = l.ItemId,
                ItemNameSnapshot = l.Item.Name,
                UnitPriceSnapshot = l.Item.Price,
                Quantity = l.Quantity,
                LineTotal = l.Item.Price * l.Quantity
            }).ToList()
        };

        order.TotalAmount = order.OrderItems.Sum(oi => oi.LineTotal);

        var payment = new Payment { Amount = order.TotalAmount, Status = PaymentStatus.Pending };
        order.Payments.Add(payment);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Created(order, payment, baseUrl, resumed: false);
    }

    private static bool IsSameAsCart(Order order, List<CartItem> lines, string postal, string address) =>
        order.ShippingPostalCode == postal &&
        order.ShippingAddress == address &&
        order.OrderItems.Count == lines.Count &&
        lines.All(l => order.OrderItems.Any(oi => oi.ItemId == l.ItemId && oi.Quantity == l.Quantity));

    private static InitiateResult Created(Order order, Payment payment, string baseUrl, bool resumed) => new()
    {
        Outcome = InitiateOutcome.Created,
        Response = new CheckoutResponse
        {
            OrderId = order.Id,
            ReferenceId = payment.ReferenceId,
            Amount = payment.Amount,
            ExpiresAt = order.ExpiresAt,
            IsResumed = resumed,
            FakeGatewayUrl = $"{baseUrl}/fake-gateway/{payment.ReferenceId}"
        }
    };

    private async Task<List<CheckoutProblem>> FreshShortfallsAsync(int userId, List<CartItem> lines)
    {
        var own = await CartRules.GetOwnReservedAsync(_db, userId);
        var ids = lines.Select(l => l.ItemId).ToList();

        var items = await _db.Items.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.IsActive, i.StockQuantity, i.ReservedQuantity })
            .ToListAsync();

        var list = new List<CheckoutProblem>();

        foreach (var l in lines)
        {
            var i = items.First(x => x.Id == l.ItemId);
            var available = i.StockQuantity - i.ReservedQuantity + own.GetValueOrDefault(i.Id);

            if (!i.IsActive || available < l.Quantity)
                list.Add(new CheckoutProblem
                {
                    ItemId = i.Id, Name = i.Name,
                    Message = !i.IsActive || available <= 0 ? CartRules.UnavailableMessage : CartRules.OnlyLeftMessage(available),
                    AvailableQuantity = Math.Max(available, 0)
                });
        }

        if (list.Count == 0)
            list.Add(new CheckoutProblem { ItemId = 0, Name = "", Message = "درخواست هم‌زمان ثبت شد. دوباره تلاش کنید." });

        return list;
    }

    public async Task<CallbackResult> ProcessCallbackAsync(Guid referenceId, bool success)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.ReferenceId == referenceId)
            .Select(p => new { p.Id, p.OrderId, p.Status })
            .FirstOrDefaultAsync();

        if (payment is null)
            return new CallbackResult { Outcome = CallbackOutcome.NotFound };

        if (payment.Status != PaymentStatus.Pending)
            return await ProcessedAsync(payment.Id, payment.OrderId);

        var now = DateTime.UtcNow;

        ////overdue
        var expired = await _db.Orders.AnyAsync(o =>
            o.Id == payment.OrderId && o.Status == OrderStatus.Pending && o.ExpiresAt <= now);

        if (expired)
        {
            await _lifecycle.CancelPendingOrderAsync(payment.OrderId, "مهلت ۷۲ ساعته پرداخت به پایان رسید.");
            return await ProcessedAsync(payment.Id, payment.OrderId);
        }

        if (!success)
        {
            await _lifecycle.CancelPendingOrderAsync(payment.OrderId, "پرداخت توسط درگاه ناموفق اعلام شد.");
            return await ProcessedAsync(payment.Id, payment.OrderId);
        }

        var (done, failReason) = await TryCompleteAsync(payment.Id, payment.OrderId);

        if (!done && failReason is not null)
            await _lifecycle.CancelPendingOrderAsync(payment.OrderId, failReason);

        return await ProcessedAsync(payment.Id, payment.OrderId);
    }

    //// Callback
    private async Task<(bool Done, string? FailReason)> TryCompleteAsync(int paymentId, int orderId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;

        //// Request Limit on Pending Payment
        var claimed = await _db.Payments
            .Where(p => p.Id == paymentId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Paid)
                .SetProperty(p => p.PaidAt, (DateTime?)now));

        if (claimed == 0)
        {
            await tx.RollbackAsync();
            return (false, null);
        }
        //

        //// Stock decrease
        var lines = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.OrderId == orderId)
            .Select(oi => new { oi.ItemId, oi.Quantity })
            .ToListAsync();

        foreach (var line in lines)
        {
            var itemId = line.ItemId;
            var qty = line.Quantity;

            var updated = await _db.Items
                .Where(i => i.Id == itemId && i.ReservedQuantity >= qty && i.StockQuantity >= qty)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, i => i.StockQuantity - qty)
                    .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity - qty)
                    .SetProperty(i => i.UpdatedAt, now));

            if (updated == 0)
            {
                await tx.RollbackAsync();
                return (false, "یکی از کالا‌ها دیگر موجود نیست");
            }
        }
        //

        //// Mark Paid Order and Remove Bought Items from Cart
        await _db.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Paid)
                .SetProperty(o => o.PaymentStatus, PaymentStatus.Paid));

        var userId = await _db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => o.UserId)
            .FirstAsync();

        var cartId = await _db.Carts
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        if (cartId is not null)
        {
            var boughtIds = lines.Select(l => l.ItemId).ToList();

            await _db.CartItems
                .Where(ci => ci.CartId == cartId.Value && boughtIds.Contains(ci.ItemId))
                .ExecuteDeleteAsync();
        }

        await tx.CommitAsync();
        return (true, null);
        //
    }

    ////Failed Payment
    private async Task FailAsync(int paymentId, int orderId, string reason)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        await _db.Payments
            .Where(p => p.Id == paymentId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Failed)
                .SetProperty(p => p.FailureReason, reason));

        await _db.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatus.Cancelled)
                .SetProperty(o => o.PaymentStatus, PaymentStatus.Failed));

        await tx.CommitAsync();
    }
    //

    ////Processed Payment
    private async Task<CallbackResult> ProcessedAsync(int paymentId, int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.Status, o.PaymentStatus })
            .FirstAsync();

        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.Id == paymentId)
            .Select(p => new { p.Status, p.FailureReason })
            .FirstAsync();

        var message = payment.Status switch
        {
            PaymentStatus.Paid => "پرداخت با موفقیت انجام شد.",
            PaymentStatus.Failed => payment.FailureReason ?? "پرداخت ناموفق بود.",
            _ => "پرداخت در انتظار نتیجه است."
        };

        return new CallbackResult
        {
            Outcome = CallbackOutcome.Processed,
            Result = new PaymentResultResponse
            {
                OrderId = orderId,
                OrderStatus = order.Status.ToString(),
                PaymentStatus = payment.Status.ToString(),
                Message = message
            }
        };
    }
    //
}