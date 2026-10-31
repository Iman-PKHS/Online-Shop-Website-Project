using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin/items")]
[Authorize(Roles = "Admin")]
public class AdminItemController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminItemController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Item, ItemResponse>> ToResponse = i => new ItemResponse
    {
        Id = i.Id,
        Name = i.Name,
        Description = i.Description,
        Price = i.Price,
        StockQuantity = i.StockQuantity,
        ImageUrl = i.ImageUrl,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        IsActive = i.IsActive,
        Categories = i.ItemCategories.Select(ic => new CategorySummary
        {
            Id = ic.Category.Id,
            Name = ic.Category.Name
        }).ToList()
    };

    private Task<ItemResponse?> LoadResponseAsync(int id) =>
        _db.Items.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync();
    
    private async Task<string?> CheckCategoriesAsync(List<int> ids)
    {
        if (ids.Count == 0)
            return null;
        
        var valid = await _db.Categories
            .Where(c => ids.Contains(c.Id) && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        var bad = ids.Except(valid).ToList();
        return bad.Count == 0
            ? null
            : $"دسته‌بندی‌های نامعتبر یا غیرفعال: {string.Join(", ", bad)}";
    }

    ////Get
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagingQuery paging,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int? categoryId)
    {
        var query = _db.Items.AsNoTracking().AsQueryable();

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(i => EF.Functions.Like(i.Name, $"%{term}%"));
        }
        if (isActive.HasValue)
        {
            query = query.Where(i => i.IsActive == isActive.Value);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(i => i.ItemCategories.Any(ic => ic.CategoryId == categoryId.Value));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(ToResponse)
            .ToListAsync();
        
        return Ok(new PagedResult<ItemResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////GET by Id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await LoadResponseAsync(id);
        return item is null
            ? NotFound(new { message = "کالای مورد نظر یافت نشد." })
            : Ok(item);
    }
    //

    ////Post
    [HttpPost]
    public async Task<IActionResult> Create(ItemRequest request)
    {
       var name = request.Name.Trim();
        if (name.Length == 0)
            return BadRequest(new { message = "نام کالا الزامی است." });
        
        var ids = request.CategoryIds.Distinct().ToList();
        var error = await CheckCategoriesAsync(ids);
        if (error is not null)
            return BadRequest(new { message = error });

        var item = new Item
        {
            Name = name,
            Description = request.Description.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl.Trim(),
            ItemCategories = ids.Select(id => new ItemCategory { CategoryId = id }).ToList()
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var response = await LoadResponseAsync(item.Id);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }
    //

    ////Put by Id
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ItemRequest request)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        var name = request.Name.Trim();
        if (name.Length == 0)
            return BadRequest(new { message = "نام کالا الزامی است." });
        
        item.Name = name;
        item.Description = request.Description.Trim();
        item.Price = request.Price;
        item.StockQuantity = request.StockQuantity;
        item.ImageUrl = request.ImageUrl.Trim();
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();

        return Ok(await LoadResponseAsync(item.Id));
    }
    //

    ////Delete by Id
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return NoContent();
    }
    //

    //PUT by Id and StockQuantity
    [HttpPut("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, StockRequest request)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        item.StockQuantity = request.Quantity;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(await LoadResponseAsync(item.Id));
    }
    //

    //PUT by Id and Categories
    [HttpPut("{id:int}/categories")]
    public async Task<IActionResult> SetCategories(int id, CategoryIdsRequest request)
    {
        var item = await _db.Items
            .Include(i => i.ItemCategories)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        var ids = request.CategoryIds.Distinct().ToList();
        var error = await CheckCategoriesAsync(ids);
        if (error is not null)
            return BadRequest(new { message = error });

        var toRemove = item.ItemCategories.Where(ic => !ids.Contains(ic.CategoryId)).ToList();
        _db.ItemCategories.RemoveRange(toRemove);

        var existing = item.ItemCategories.Select(ic => ic.CategoryId).ToHashSet();
        foreach (var caegoruId in ids.Where(id => !existing.Contains(id)))
        {
            item.ItemCategories.Add(new ItemCategory { CategoryId = caegoruId });
        }
        
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(await LoadResponseAsync(id));
    }
    //







































}