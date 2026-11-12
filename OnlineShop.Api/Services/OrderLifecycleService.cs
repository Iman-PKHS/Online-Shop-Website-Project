using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.Entities.Enums;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Services;

public class OrderLifecycleService : IOrderLifecycleService
{
    private readonly AppDbContext _db;

    public OrderLifecycleService(AppDbContext db) => _db = db;

    public async Task<bool> CancelPendingOrderAsync(int orderId, string reason)
    {
        var ownsTx = _db.Database.CurrentTransaction is null;
        var tx = ownsTx ? await _db.Database.BeginTransactionAsync() : null;

        try
        {
            var now = DateTime.UtcNow;

            var claimed = await _db.Orders
                .Where(o => o.Id == orderId && o.Status == OrderStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, OrderStatus.Cancelled)
                    .SetProperty(o => o.PaymentStatus, PaymentStatus.Failed)
                    .SetProperty(o => o.CancelledAt, (DateTime?)now));

            if (claimed == 0)
            {
                if (ownsTx) await tx!.RollbackAsync();
                return false;
            }

            await _db.Payments
                .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, PaymentStatus.Failed)
                    .SetProperty(p => p.FailureReason, reason));

            var lines = await _db.OrderItems
                .AsNoTracking()
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => new { oi.ItemId, oi.Quantity })
                .ToListAsync();

            foreach (var line in lines)
            {
                var itemId = line.ItemId;
                var qty = line.Quantity;

                await _db.Items
                    .Where(i => i.Id == itemId && i.ReservedQuantity >= qty)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity - qty));
            }

            if (ownsTx) await tx!.CommitAsync();
            return true;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    public async Task<int> ExpireOverdueAsync(int? userId = null)
    {
        var now = DateTime.UtcNow;

        var query = _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt <= now);

        if (userId is not null)
        {
            var uid = userId.Value;
            query = query.Where(o => o.UserId == uid);
        }

        var ids = await query.OrderBy(o => o.Id).Select(o => o.Id).Take(200).ToListAsync();

        var count = 0;
        foreach (var id in ids)
            if (await CancelPendingOrderAsync(id, "مهلت ۷۲ ساعته پرداخت به پایان رسید."))
                count++;

        return count;
    }
}