using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Api.DTOs;

public class CategoryIdsRequest
{
    [Required(ErrorMessage = "شناسه دسته‌بندی الزامی است.")]
    public List<int> CategoryIds { get; set; } = new();
}