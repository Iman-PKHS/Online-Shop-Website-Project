using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class PaymentCallbackRequest
{
    [Required]
    public Guid ReferenceId { get; set; }

    [Required(ErrorMessage = "وضعیت پرداخت الزامی است.")]
    public string Status { get; set; } = string.Empty;
}