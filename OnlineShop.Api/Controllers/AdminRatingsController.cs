using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminRatingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminRatingsController(AppDbContext db)
    {
        _db = db;
    }

    //// GET by Item Id
    [HttpGet("items/{itemId:int}/ratings")]
    public async Task<IActionResult> ForItem(int itemId, [FromQuery] PagingQuery paging)
    {
        if (!await _db.Items.AnyAsync(i => i.Id == itemId))
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        var query = _db.Ratings.AsNoTracking().Where(r => r.ItemId == itemId);
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(r => new AdminRatingResponse
            {
                Id = r.Id,
                ItemId = r.ItemId,
                UserId = r.UserId,
                Username = r.User.Username,
                Stars = r.Stars,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AdminRatingResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////Get Rating
    [HttpGet("ratings")]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagingQuery paging,
        [FromQuery] int? itemId,
        [FromQuery] int? userId,
        [FromQuery] int? stars)
    {
        var query = _db.Ratings.AsNoTracking().AsQueryable();

        if (itemId.HasValue)
        {
            var i = itemId.Value;
            query = query.Where(r => r.ItemId == i);
        }
        if (userId.HasValue)
        {
            var u = userId.Value;
            query = query.Where(r => r.UserId == u);
        }
        if (stars.HasValue)
        {
            var s = stars.Value;
            query = query.Where(r => r.Stars == s);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(r => new AdminRatingResponse
            {
                Id = r.Id, ItemId = r.ItemId, ItemName = r.Item.Name,
                UserId = r.UserId, Username = r.User.Username, Stars = r.Stars,
                CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AdminRatingResponse>
        {
            Items = items, Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }
    //

    //// Delete
    [HttpDelete("ratings/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.Id == id);
        if (rating is null)
            return NotFound(new { message = "امتیاز پیدا نشد." });

        _db.Ratings.Remove(rating);
        await _db.SaveChangesAsync();

        return NoContent();
    }
    //
}