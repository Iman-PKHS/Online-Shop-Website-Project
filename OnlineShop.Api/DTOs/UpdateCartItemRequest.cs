using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class UpdateCartItemRequest
{
    [Range(1, 99, ErrorMessage = "تعداد باید بین 1 تا 99 باشد.")]
    public int Quantity { get; set; }
}