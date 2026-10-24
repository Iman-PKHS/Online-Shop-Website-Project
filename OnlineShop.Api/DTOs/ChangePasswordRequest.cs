using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class ChangePasswordRequest
{
    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}