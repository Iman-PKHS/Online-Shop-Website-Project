using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class CategoryRequest
{
    [Required(ErrorMessage = "نام دسته بندی الزامی است.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "نام دسته بندی باید بین 1 تا 100 کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}