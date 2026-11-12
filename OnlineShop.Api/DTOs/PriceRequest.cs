using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class PriceRequest
{
    [Range(1, 100_000_000_000, ErrorMessage = "قیمت نامعتبر است.")]
    public long Price { get; set; }

    public bool Confirm { get; set; } = false;
}