using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class LoginRequest
{
    [Required]
    [StringLength(254)]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}