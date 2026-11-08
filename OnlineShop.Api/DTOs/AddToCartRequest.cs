using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class AddToCartRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "شناسه کالا نامعتبر است.")]
    public int ItemId { get; set; }

    [Range(1, 99, ErrorMessage = "تعداد باید بین 1 تا 99 باشد.")]
    public int Quantity { get; set; } = 1;

    public bool IncreaseIfExists { get; set; } = false;
}