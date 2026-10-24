using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class ChangeUsernameRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    private const string UsernamePattern =
    @"^[a-zA-Z0-9_" +
    @"\u0600-\u06FF" +
    @"\u200C]+$";          
    [RegularExpression(UsernamePattern, ErrorMessage = "نام کاربری باید شامل حروف انگلیسی، اعداد، زیرخط، حروف فارسی و نیم‌فاصله باشد.")]
    public string NewUsername { get; set; } = string.Empty;
}