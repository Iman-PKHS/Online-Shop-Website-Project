using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Entities.Enums;

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
            var clothing = new Category { Name = "Clothing" };
            var shoes    = new Category { Name = "Shoes" };
            var sports   = new Category { Name = "Sports" };
            var gadgets  = new Category { Name = "Gadgets" };

            db.Items.AddRange(
                new Item
                {
                    Name = "Running Shoes", Description = "Light shoes for daily running",
                    Price = 2_500_000, StockQuantity = 20, ImageUrl = "https://example.com/img/running-shoes.jpg",
                    ItemCategories = new List<ItemCategory>
                    {
                        new() { Category = shoes },
                        new() { Category = sports }
                    }
                },
                new Item
                {
                    Name = "Leather Boots", Description = "Winter boots",
                    Price = 4_800_000, StockQuantity = 5, ImageUrl = "https://example.com/img/boots.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = shoes } }
                },
                new Item
                {
                    Name = "Sport T-Shirt", Description = "Breathable cotton shirt",
                    Price = 650_000, StockQuantity = 50, ImageUrl = "https://example.com/img/tshirt.jpg",
                    ItemCategories = new List<ItemCategory>
                    {
                        new() { Category = clothing },
                        new() { Category = sports }
                    }
                },
                new Item
                {
                    Name = "Winter Jacket", Description = "Warm waterproof jacket",
                    Price = 7_200_000, StockQuantity = 0, ImageUrl = "https://example.com/img/jacket.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = clothing } }
                },
                new Item
                {
                    Name = "Smart Watch", Description = "Fitness tracking watch",
                    Price = 9_900_000, StockQuantity = 8, ImageUrl = "https://example.com/img/watch.jpg",
                    ItemCategories = new List<ItemCategory>
                    {
                        new() { Category = gadgets },
                        new() { Category = sports }
                    }
                },
                new Item
                {
                    Name = "Phone Charger", Description = "Fast USB-C charger",
                    Price = 350_000, StockQuantity = 100, ImageUrl = "https://example.com/img/charger.jpg",
                    ItemCategories = new List<ItemCategory> { new() { Category = gadgets } }
                }
            );

            await db.SaveChangesAsync();
        }
    }
}