namespace OnlineShop.Api.DTOs;

public class AdminUserSummary
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserDetail : AdminUserSummary
{
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public DateTime? AddressUpdatedAt { get; set; }

    public int OrdersCount { get; set; }
    public int PaidOrdersCount { get; set; }
    public long TotalSpent { get; set; } 
    public DateTime? LastOrderDate { get; set; }
    public int PendingOrdersCount { get; set; }

    public int CommentsCount { get; set; }
    public int RatingsCount { get; set; }
}

public class DisableUserRequest
{
    public bool Confirm { get; set; } = false;
}