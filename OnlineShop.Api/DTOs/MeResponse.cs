namespace OnlineShop.Api.DTOs;

//Me is for current User
public class MeResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public bool HasAddress { get; set; }
    public DateTime? AddressUpdatedAt { get; set;}
}