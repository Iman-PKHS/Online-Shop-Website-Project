using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class StockRequest
{
    [Required(ErrorMessage = "تعداد محصول الزامی است.")]
    [Range(0, 1_000_000, ErrorMessage = "موجودی نامعتبر است.")]
    public int Quantity { get; set; }
}