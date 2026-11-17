using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class MyCommentResponse : CommentResponse
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemImageUrl { get; set; } = string.Empty;
}
public class CommentRequest
{
    [Required(ErrorMessage = "متن نظر الزامی است.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "متن نظر باید بین 1 تا 2000 کاراکتر باشد.")]
    public string Text { get; set; } = string.Empty;
}

public class CommentResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEdited => UpdatedAt > CreatedAt.AddSeconds(1);
    public bool IsMine { get; set; }
    public bool IsHidden { get; set; }// only for owner of comment
    public List<ReplyResponse> Replies { get; set; } = new();
}

public class ReplyResponse
{
    public int Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEdited => UpdatedAt > CreatedAt.AddSeconds(1);
}

public class MyRatingResponse
{
    public int RatingId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemImageUrl { get; set; } = string.Empty;
    public int Stars { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}