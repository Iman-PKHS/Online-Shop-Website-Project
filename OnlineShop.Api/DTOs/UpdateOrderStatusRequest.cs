using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "وضعیت الزامی است.")]
    public string Status { get; set; } = string.Empty;   //Shipped, Delivered or Cancelled
}