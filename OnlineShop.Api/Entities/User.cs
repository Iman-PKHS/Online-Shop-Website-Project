using OnlineShop.Api.Entities.Enums;

namespace OnlineShop.Api.Entities;

public class User
{
    // Properties
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool Isactive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    //

    //Navigations
    public Cart? Cart { get; set; }
    public Wishlist? Wishlist { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Rating> Ratings { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    //
}