using Microsoft.EntityFrameworkCore;
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

    public FakePaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<InitiateResult> InitiateAsync(int userId, string baseUrl)
    {
        var lines = await _db.CartItems
            .Include(ci => ci.Item)
            .Where(ci => ci.Cart.UserId == userId)
            .OrderBy(ci => ci.Id)
            .ToListAsync();

        if (lines.Count == 0)
            return new InitiateResult { Outcome = InitiateOutcome.CartEmpty };

        var problems = new List<CheckoutProblem>();
        var toRemove = new List<CartItem>();

        foreach (var line in lines)
        {
            var item = line.Item;

            if (!item.IsActive || item.StockQuantity == 0)
            {
                problems.Add(new CheckoutProblem
                {
                    ItemId = item.Id,
                    Name = item.Name,
                    Message = CartRules.UnavailableMessage,
                    RemovedFromCart = true
                });
                toRemove.Add(line);
            }
            else if (item.StockQuantity < line.Quantity)
            {
                problems.Add(new CheckoutProblem
                {
                    ItemId = item.Id,
                    Name = item.Name,
                    Message = CartRules.OnlyLeftMessage(item.StockQuantity),
                    AvailableQuantity = item.StockQuantity
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

            return new InitiateResult 
            {
                Outcome = InitiateOutcome.ProblemsFound, Problems = problems
            };
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        ////Reattempt
        var oldOrders = await _db.Orders
            .Include(o => o.Payments)
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending)
            .ToListAsync();

        foreach (var old in oldOrders)
        {
            old.Status = OrderStatus.Cancelled;
            old.PaymentStatus = PaymentStatus.Failed;

            foreach (var p in old.Payments.Where(p => p.Status == PaymentStatus.Pending))
            {
                p.Status = PaymentStatus.Failed;
                p.FailureReason = "جایگزین شد.";
            }
        }
        //

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
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

        var payment = new Payment
        {
            Amount = order.TotalAmount,
            Status = PaymentStatus.Pending
        };
        order.Payments.Add(payment);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new InitiateResult
        {
            Outcome = InitiateOutcome.Created,
            Response = new CheckoutResponse
            {
                OrderId = order.Id,
                ReferenceId = payment.ReferenceId,
                Amount = payment.Amount,
                FakeGatewayUrl = $"{baseUrl}/fake-gateway/{payment.ReferenceId}"
            }
        };
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

    // already processed: return the same result, change nothing
    if (payment.Status != PaymentStatus.Pending)
        return await ProcessedAsync(payment.Id, payment.OrderId);

    if (!success)
    {
        await FailAsync(payment.Id, payment.OrderId, "پرداخت توسط درگاه ناموفق اعلام شد.");
        return await ProcessedAsync(payment.Id, payment.OrderId);
    }

    var (done, failReason) = await TryCompleteAsync(payment.Id, payment.OrderId);

    if (!done && failReason is not null)
        await FailAsync(payment.Id, payment.OrderId, failReason);

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
                .Where(i => i.Id == itemId && i.IsActive && i.StockQuantity >= qty)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.StockQuantity, i => i.StockQuantity - qty)
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