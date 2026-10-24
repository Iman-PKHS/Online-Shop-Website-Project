using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    private const string UsernamePattern =
    @"^[a-zA-Z0-9_" +
    @"\u0600-\u06FF" +
    @"\u200C]+$";          
    [RegularExpression(UsernamePattern, ErrorMessage = "نام کاربری باید شامل حروف انگلیسی، اعداد، زیرخط، حروف فارسی و نیم‌فاصله باشد.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}