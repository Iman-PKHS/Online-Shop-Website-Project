namespace OnlineShop.Api.Interfaces;

public interface IOrderLifecycleService
{
    // true = cancel ; false = no longer Pending
    Task<bool> CancelPendingOrderAsync(int orderId, string reason);

    // cancels overdue Pending orders ; returns how many
    Task<int> ExpireOverdueAsync(int? userId = null);
}