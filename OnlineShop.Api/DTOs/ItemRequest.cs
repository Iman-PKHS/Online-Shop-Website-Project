using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class ItemRequest
{
    [Required(ErrorMessage = "نام محصول الزامی است.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "نام محصول باید بین 1 تا 200 کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "توضیحات محصول الزامی است.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "توضیحات محصول باید بین 1 تا 2000 کاراکتر باشد.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "قیمت الزامی است.")]
    [Range(1, 100_000_000_000, ErrorMessage = "قیمت نامعتبر است.")]
    public long Price { get; set; }

    [Required(ErrorMessage = "تعداد محصول الزامی است.")]
    [Range(0, 1_000_000,ErrorMessage = "موجودی نامعتبر است.")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "تصویر الزامی است.")]
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "آدرس تصویر نامعتبر است.")]
    [StringLength(255, MinimumLength = 1)]
    public string ImageUrl { get; set; } = string.Empty;

    public List<int> CategoryIds { get; set; } = new();

    public bool IsActive { get; set; } = true;
}