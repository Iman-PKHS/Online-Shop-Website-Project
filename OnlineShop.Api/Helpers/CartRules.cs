using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Entities.Enums;

namespace OnlineShop.Api.Helpers;

public static class CartRules
{
    public const int MaxQuantity = 99;
    public const string UnavailableMessage = "این کالا موجود نیست.";

    public static string OnlyLeftMessage(int stock) => $"فقط {stock} عدد موجود است.";

    public static async Task<Cart> GetOrCreateCartAsync(AppDbContext db, int userId)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart is not null) return cart;

        cart = new Cart { UserId = userId };
        db.Carts.Add(cart);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.Entry(cart).State = EntityState.Detached;
            cart = await db.Carts.FirstAsync(c => c.UserId == userId);
        }

        return cart;
    }

    public static async Task<Wishlist> GetOrCreateWishlistAsync(AppDbContext db, int userId)
    {
        var wishlist = await db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wishlist is not null) return wishlist;

        wishlist = new Wishlist { UserId = userId };
        db.Wishlists.Add(wishlist);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.Entry(wishlist).State = EntityState.Detached;
            wishlist = await db.Wishlists.FirstAsync(w => w.UserId == userId);
        }

        return wishlist;
    }

    public static async Task<Dictionary<int, int>> GetOwnReservedAsync(AppDbContext db, int userId)
    {
        var rows = await db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.UserId == userId && oi.Order.Status == OrderStatus.Pending)
            .Select(oi => new { oi.ItemId, oi.Quantity })
            .ToListAsync();

        return rows.GroupBy(r => r.ItemId).ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
    }

}