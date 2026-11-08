using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class AddToWishlistRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "شناسه کالا نامعتبر است.")]
    public int ItemId { get; set; }
}