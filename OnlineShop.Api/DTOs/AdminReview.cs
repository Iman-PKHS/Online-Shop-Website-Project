using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class ReplyRequest
{
    [Required(ErrorMessage = "متن پاسخ الزامی است.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "متن پاسخ باید بین 1 تا 2000 کاراکتر باشد.")]
    public string Text { get; set; } = string.Empty;
}

public class VisibilityRequest
{
    public bool IsVisible { get; set; }
}

public class AdminReplyResponse
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminCommentResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AdminReplyResponse> Replies { get; set; } = new();
}

public class AdminRatingResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Stars { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}