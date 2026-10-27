using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "نام کاربری الزامی است.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "نام کاربری باید بین 3 تا 50 کاراکتر باشد.")]
    private const string UsernamePattern =
    @"^[a-zA-Z0-9_" +
    @"\u0600-\u06FF" +
    @"\u200C]+$";          
    [RegularExpression(UsernamePattern, ErrorMessage = "نام کاربری باید شامل حروف انگلیسی، اعداد، زیرخط، حروف فارسی و نیم‌فاصله باشد.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "فرمت ایمیل نامعتبر است.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "رمز عبور باید بین 8 تا 64 کاراکتر باشد.")]
    public string Password { get; set; } = string.Empty;
}