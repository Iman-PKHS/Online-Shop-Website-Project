using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class RatingRequest
{
    [Range(1, 5, ErrorMessage = "امتیاز باید بین 1 تا 5 باشد.")]
    public int Stars { get; set; }
}

public class RatingResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Stars { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RatingSummaryResponse
{
    public double? Average { get; set; }
    public int Count { get; set; }
    public int? MyStars { get; set; }
    public bool CanReview { get; set; }
}

public class RatingListResponse : RatingSummaryResponse
{
    public PagedResult<RatingResponse> Ratings { get; set; } = new();
}