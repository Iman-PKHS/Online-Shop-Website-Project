using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Item, ItemSummary>> ToSummary = i => new ItemSummary
    {
        Id = i.Id,
        Name = i.Name,
        Price = i.Price,
        ImageUrl = i.ImageUrl,
        InStock = i.StockQuantity > 0,
        AverageRating = i.Ratings.Average(r => (double?)r.Stars),
        Categories = i.ItemCategories
            .Where(ic => ic.Category.IsActive)
            .Select(ic => new CategorySummary { Id = ic.Category.Id, Name = ic.Category.Name })
            .ToList()
    };

    ////Get Categories
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] PagingQuery paging)
    {
        var query = _db.Categories.AsNoTracking().Where(c => c.IsActive);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Name)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(c => new CustomerCategoryResponse {Id = c.Id, Name = c.Name})
            .ToListAsync();

        return Ok(new PagedResult<CustomerCategoryResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
            
    }
    //

    ////Get Category with Items by id
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id, [FromQuery] PagingQuery paging)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new {c.Id, c.Name})
            .FirstOrDefaultAsync();
        
        if (category is null)
            return NotFound(new { Message = "دسته بندی مورد نظر یافت نشد." });   
        
        var query = _db.Items
            .AsNoTracking()
            .Where(i => i.IsActive && i.ItemCategories.Any(ic => ic.CategoryId == id));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(ToSummary)
            .ToListAsync();
        
        return Ok(new CategoryWithItems
        {
            Id = category.Id,
            Name = category.Name,
            Items = new PagedResult<ItemSummary>
            {
                Items = items,
                Page = paging.Page,
                PageSize = paging.PageSize,
                TotalCount = totalCount
            }
        });
    }
    //
}