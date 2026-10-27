using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "نام کاربری یا ایمیل الزامی است.")]
    [StringLength(254, ErrorMessage = "نام کاربری یا ایمیل نباید بیشتر از 254 کاراکتر باشد.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "رمز عبور باید بین 8 تا 64 کاراکتر باشد.")]
    public string Password { get; set; } = string.Empty;
}