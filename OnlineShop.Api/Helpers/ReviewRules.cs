using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.Entities.Enums;

namespace OnlineShop.Api.Helpers;

public static class ReviewRules
{
    public static readonly bool RequirePurchase = true;
    public const int MaxCommentsPerItem = 5;
    public const int MaxCommentLength = 2000;

    public const string AdminLabel = "مدیر فروشگاه";
    public const string NotBuyerMessage = "فقط خریداران این کالا می‌توانند امتیاز یا نظر ثبت کنند.";

    public static Task<bool> IsActiveUserAsync(AppDbContext db, int userId) =>
        db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive);

    public static async Task<bool> CanReviewAsync(AppDbContext db, int userId, int itemId)
    {
        if (!RequirePurchase) return true;

        return await db.OrderItems
            .AsNoTracking()
            .AnyAsync(oi => oi.ItemId == itemId
                         && oi.Order.UserId == userId
                         && (oi.Order.Status == OrderStatus.Paid
                          || oi.Order.Status == OrderStatus.Shipped
                          || oi.Order.Status == OrderStatus.Delivered));
    }

    public static async Task<bool> ItemIsPublicAsync(AppDbContext db, int itemId) =>
        await db.Items.AsNoTracking().AnyAsync(i => i.Id == itemId && i.IsActive);
}