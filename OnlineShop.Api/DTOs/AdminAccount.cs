using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class CreateAdminRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_\u0600-\u06FF\u200C ]+$")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(64, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}