using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class UpdateAddressRequest
{
    [Required(ErrorMessage = "کد پستی الزامی است.")]
    [RegularExpression(@"^[0-9۰-۹٠-٩]{10}$", ErrorMessage = "کد پستی باید ۱۰ رقم باشد.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "آدرس الزامی است.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "آدرس باید بین ۱۰ تا ۵۰۰ کاراکتر باشد.")]
    public string Address { get; set; } = string.Empty;
}