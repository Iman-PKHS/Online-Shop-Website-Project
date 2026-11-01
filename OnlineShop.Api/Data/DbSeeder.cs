using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Entities.Enums;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. Admin user
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@shop.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        // 2. Categories and items
        if (!await db.Categories.AnyAsync() && !await db.Items.AnyAsync())
        {
            var clothing = new Category { Name = TextNormalizer.Normalize("پوشاک") };
            var shoes    = new Category { Name = TextNormalizer.Normalize("کفش") };
            var sports   = new Category { Name = TextNormalizer.Normalize("ورزشی") };
            var gadgets  = new Category { Name = TextNormalizer.Normalize("لوازم دیجیتال") };

            db.Items.AddRange(
                new Item
                {
                    Name = TextNormalizer.Normalize("کفش دویدن"), Description = TextNormalizer.Normalize("کفش سبک برای دویدن روزانه"),
                    Price = 2_500_000, StockQuantity = 20, ImageUrl = "https://example.com/img/1.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = shoes }, new() { Category = sports } }
                },
                new Item
                {
                    Name = TextNormalizer.Normalize("بوت چرمی"), Description = TextNormalizer.Normalize("بوت زمستانی"),
                    Price = 4_800_000, StockQuantity = 5, ImageUrl = "https://example.com/img/2.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = shoes } }
                },
                new Item
                {
                    Name = TextNormalizer.Normalize("تیشرت ورزشی"), Description = TextNormalizer.Normalize("تیشرت نخی"),
                    Price = 650_000, StockQuantity = 50, ImageUrl = "https://example.com/img/3.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = clothing }, new() { Category = sports } }
                },
                new Item
                {
                    Name = TextNormalizer.Normalize("کاپشن زمستانی"), Description = TextNormalizer.Normalize("کاپشن گرم ضد آب"),
                    Price = 7_200_000, StockQuantity = 0, ImageUrl = "https://example.com/img/4.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = clothing } }
                },
                new Item
                {
                    Name = TextNormalizer.Normalize("ساعت هوشمند"), Description = TextNormalizer.Normalize("ساعت ورزشی"),
                    Price = 9_900_000, StockQuantity = 8, ImageUrl = "https://example.com/img/5.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = gadgets }, new() { Category = sports } }
                },
                new Item
                {
                    Name = TextNormalizer.Normalize("شارژر موبایل"), Description = TextNormalizer.Normalize("شارژر سریع"),
                    Price = 350_000, StockQuantity = 100, ImageUrl = "https://example.com/img/6.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = gadgets } }
                }
            );

            await db.SaveChangesAsync();
        }
    }
}