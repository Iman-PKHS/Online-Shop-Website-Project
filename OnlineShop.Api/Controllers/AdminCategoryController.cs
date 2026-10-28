using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminCategoryController(AppDbContext db)
    {
        _db = db;
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };

    //// GET Categories
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagingQuery paging,
        [FromQuery] string ? search,
        [FromQuery] bool ? isActive)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();

        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%"));

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);
        
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Name)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
        
        return Ok(new PagedResult<CategoryResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////Post Gategories
    [HttpPost]
    public async Task<IActionResult> Create(CategoryRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
            return BadRequest(new { message = "نام دسته بندی الزامی است." });

        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Name == name);
        
        if (existing != null)
        {
            if (existing.IsActive)
                return Conflict(new { message = "این دسته بندی قبلا ثبت شده است." });
            
            return Conflict(new
            { 
                message = "این دسته بندی قبلا ثبت شده است اما غیر فعال است. لطفا آن را فعال کنید.",
                inactiveCategoryId = existing.Id
            });
        }

        var category = new Category {Name = name};

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToResponse(category));
    }
    //

    ////Get Category By Id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return NotFound(new { message = "دسته بندی مورد نظر یافت نشد." });

        return Ok(ToResponse(category));
    }
    //

    ////Put Category by Id
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return NotFound(new { message = "دسته بندی مورد نظر یافت نشد." });

        var name = request.Name.Trim();
        if (name.Length == 0)
            return BadRequest(new { message = "نام دسته بندی الزامی است." });

        if (await _db.Categories.AnyAsync(c => c.Id != id && c.Name == name))
            return Conflict(new { message = "این دسته بندی قبلا ثبت شده است." });

        category.Name = name;
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(category));
    }
    //

    ////Delete Category by Id
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return NotFound(new { message = "دسته بندی مورد نظر یافت نشد." });

        category.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
    //




}