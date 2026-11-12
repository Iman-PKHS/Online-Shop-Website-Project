using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Entities.Enums;

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
        ReservedQuantity = i.ReservedQuantity,
        AvailableQuantity = i.StockQuantity - i.ReservedQuantity,
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

    private record PendingUse(int OrderId, int Quantity, DateTime ExpiresAt);

    private async Task<List<PendingUse>> GetPendingUseAsync(int itemId) =>
        await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ItemId == itemId && oi.Order.Status == OrderStatus.Pending)
            .Select(oi => new PendingUse(oi.OrderId, oi.Quantity, oi.Order.ExpiresAt))
            .ToListAsync();

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
        
        query = query.ApplySearch(search, activeCategoriesOnly: false);

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
       var name = TextNormalizer.Normalize(request.Name);
        if (name.Length == 0)
            return BadRequest(new { message = "نام کالا الزامی است." });
        
        var description = TextNormalizer.Normalize(request.Description);
        if (description.Length == 0)
            return BadRequest(new { message = "توضیحات کالا الزامی است." });
        
        var duplicate = await _db.Items
            .Where(i => i.Name == name && i.Description == description)
            .Select(i => new { i.Id, i.IsActive })
            .FirstOrDefaultAsync();

        if (duplicate is not null)
            return Conflict(new
            {
                message = duplicate.IsActive
                    ? "کالایی با همین نام و توضیحات قبلاً ثبت شده است."
                    : $"کالایی با همین نام و توضیحات غیرفعال است (id {duplicate.Id}). آن را بازیابی کنید.",
                existingItemId = duplicate.Id
            });

        var ids = request.CategoryIds.Distinct().ToList();
        var error = await CheckCategoriesAsync(ids);
        if (error is not null)
            return BadRequest(new { message = error });

        var item = new Item
        {
            Name = name,
            Description = description,
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

        var name = TextNormalizer.Normalize(request.Name);
        if (name.Length == 0)
            return BadRequest(new { message = "نام کالا الزامی است." });
        
        var description = TextNormalizer.Normalize(request.Description);
        if (description.Length == 0)
            return BadRequest(new { message = "توضیحات کالا الزامی است." });
        
        item.Name = name;
        item.Description = description;
        item.Price = request.Price;
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

    
    ////PUT by Id and StockQuantity
    [HttpPut("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, StockRequest request)
    {
        var info = await _db.Items.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new { i.ReservedQuantity })
            .FirstOrDefaultAsync();

        if (info is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        // hard rule: never below what pending orders reserved
        if (request.Quantity < info.ReservedQuantity)
            return BadRequest(new
            {
                message = $"{info.ReservedQuantity} عدد توسط سفارش‌های در انتظار پرداخت رزرو شده است؛ موجودی نمی‌تواند کمتر از آن باشد.",
                reservedQuantity = info.ReservedQuantity
            });

        var pending = await GetPendingUseAsync(id);

        if (pending.Count > 0 && !request.Confirm)
            return Conflict(new
            {
                requiresConfirm = true,
                message = $"این کالا در {pending.Count} سفارش در انتظار پرداخت هست ({info.ReservedQuantity} عدد رزرو). برای ادامه confirm را true بفرستید.",
                pendingOrdersCount = pending.Count,
                reservedQuantity = info.ReservedQuantity,
                pendingOrderIds = pending.Select(p => p.OrderId).ToList()
            });

        var qty = request.Quantity;
        var now = DateTime.UtcNow;

        var updated = await _db.Items
            .Where(i => i.Id == id && i.ReservedQuantity <= qty)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.StockQuantity, qty)
                .SetProperty(i => i.UpdatedAt, now));

        if (updated == 0)
            return Conflict(new { message = "رزرو جدیدی ثبت شد. دوباره تلاش کنید." });

        return Ok(await LoadResponseAsync(id));
    }
    //

    ////PUT by Id and Categories
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

    ////PUT by Id and Price
    [HttpPut("{id:int}/price")]
    public async Task<IActionResult> UpdatePrice(int id, PriceRequest request)
    {
        var item = await _db.Items.FindAsync(id);
        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        var pending = await GetPendingUseAsync(id);

        if (pending.Count > 0 && !request.Confirm)
            return Conflict(new
            {
                requiresConfirm = true,
                message = $"این کالا در {pending.Count} سفارش در انتظار پرداخت هست. قیمت آن سفارش‌ها تغییر نمی‌کند. برای ادامه باید تایید کنید.",
                pendingOrdersCount = pending.Count,
                pendingOrderIds = pending.Select(p => p.OrderId).ToList()
            });

        item.Price = request.Price;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await LoadResponseAsync(id));
    }


}