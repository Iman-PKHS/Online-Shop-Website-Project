using Microsoft.EntityFrameworkCore;

using OnlineShop.Api.Entities; 

using OnlineShop.Api.Entities.Enums; 

namespace OnlineShop.Api.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.Email).HasMaxLength(100);

            e.Property(u => u.PasswordHash).HasMaxLength(255);

            e.Property(u => u.Username).UseCollation("NOCASE");
            e.Property(u => u.Email).UseCollation("NOCASE");
        });

        modelBuilder.Entity<Item>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(100);
            e.Property(i => i.Description).HasMaxLength(255);
            e.Property(i => i.ImageUrl).HasMaxLength(255);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Items_Price", "Price >= 0");
                t.HasCheckConstraint("CK_Items_StockQuantity", "StockQuantity >= 0");
            });

        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ItemCategory>(e =>
        {
            e.HasKey(ic => new { ic.ItemId, ic.CategoryId });

            e.HasOne(ic => ic.Item)
            .WithMany(i => i.ItemCategories)
            .HasForeignKey(ic => ic.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(ic => ic.Category)
            .WithMany(c => c.ItemCategories)
            .HasForeignKey(ic => ic.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cart>(e =>
        {
            e.HasOne(c => c.User)
            .WithOne(u => u.Cart)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasOne(ci => ci.Cart)
            .WithMany(c => c.CartItems)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(ci => ci.Item)
            .WithMany()
            .HasForeignKey(ci => ci.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
            
            
            e.ToTable(t => t.HasCheckConstraint("CK_CartItems_Quantity", "Quantity >= 1"));
        });

        modelBuilder.Entity<Wishlist>(e =>
        {
            e.HasOne(w => w.User)
            .WithOne(u => u.Wishlist)
            .HasForeignKey<Wishlist>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WishlistItem>(e =>
        {
            e.HasOne(wi => wi.Wishlist)
            .WithMany(w => w.WishlistItems)
            .HasForeignKey(wi => wi.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(wi => wi.Item)
            .WithMany()
            .HasForeignKey(wi => wi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            
            e.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(oi => oi.ItemNameSnapshot).HasMaxLength(200);
            
            e.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(oi => oi.Item)
            .WithMany()
            .HasForeignKey(oi => oi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
            
            e.ToTable(t => t.HasCheckConstraint("CK_OrderItems_Quantity", "Quantity >= 1"));
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            
            e.HasIndex(p => p.ReferenceId).IsUnique();
            
            e.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rating>(e =>
        {
            e.HasIndex(r => new { r.UserId, r.ItemId }).IsUnique();
            
            e.HasOne(r => r.User)
            .WithMany(u => u.Ratings)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
            e.HasOne(r => r.Item)
            .WithMany(i => i.Ratings)
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
             
            e.ToTable(t => t.HasCheckConstraint("CK_Ratings_Stars", "Stars >= 1 AND Stars <= 5"));
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.Property(c => c.Text).HasMaxLength(2000);
            
            e.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
            e.HasOne(c => c.Item)
            .WithMany(i => i.Comments)
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommentReply>(e =>
        {
            e.Property(r => r.Text).HasMaxLength(2000);
            
            e.HasOne(r => r.Comment)
            .WithMany(c => c.CommentReplies)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
            
            e.HasOne(r => r.AdminUser)
            .WithMany()
            .HasForeignKey(r => r.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentReply> CommentReplies => Set<CommentReply>();
}