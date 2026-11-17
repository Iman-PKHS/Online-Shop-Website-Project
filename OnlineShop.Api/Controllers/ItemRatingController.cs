using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/items/{itemId:int}/ratings")]
public class ItemRatingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ItemRatingsController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<RatingSummaryResponse> SummaryAsync(int itemId, int me)
    {
        var q = _db.Ratings.AsNoTracking().Where(r => r.ItemId == itemId);

        var count = await q.CountAsync();
        var average = await q.AverageAsync(r => (double?)r.Stars);

        int? mine = null;
        if (me != 0)
            mine = await q.Where(r => r.UserId == me)
                          .Select(r => (int?)r.Stars)
                          .FirstOrDefaultAsync();

        var canReview = me != 0 && await ReviewRules.CanReviewAsync(_db, me, itemId);

        return new RatingSummaryResponse
        {
            Average = average,
            Count = count,
            MyStars = mine,
            CanReview = canReview
        };
    }

    //// Get Item Rating
    [HttpGet]
    public async Task<IActionResult> GetAll(int itemId, [FromQuery] PagingQuery paging)
    {
        if (!await ReviewRules.ItemIsPublicAsync(_db, itemId))
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        var me = User.GetUserId() ?? 0;
        var summary = await SummaryAsync(itemId, me);

        var query = _db.Ratings.AsNoTracking().Where(r => r.ItemId == itemId);

        var items = await query
            .OrderByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(r => new RatingResponse
            {
                Id = r.Id,
                Username = r.User.Username,
                Stars = r.Stars,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new RatingListResponse
        {
            Average = summary.Average,
            Count = summary.Count,
            MyStars = summary.MyStars,
            CanReview = summary.CanReview,
            Ratings = new PagedResult<RatingResponse>
            {
                Items = items,
                Page = paging.Page,
                PageSize = paging.PageSize,
                TotalCount = summary.Count
            }
        });
    }
    //

    ////POST Rate (create and or update)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Rate(int itemId, RatingRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        if (!await ReviewRules.IsActiveUserAsync(_db, uid)) return Unauthorized();

        if (!await ReviewRules.ItemIsPublicAsync(_db, itemId))
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        if (!await ReviewRules.CanReviewAsync(_db, uid, itemId))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ReviewRules.NotBuyerMessage });

        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.ItemId == itemId && r.UserId == uid);
        var created = rating is null;
        var now = DateTime.UtcNow;

        if (rating is null)
        {
            rating = new Rating { ItemId = itemId, UserId = uid, Stars = request.Stars, CreatedAt = now, UpdatedAt = now };
            _db.Ratings.Add(rating);
        }
        else
        {
            rating.Stars = request.Stars;
            rating.UpdatedAt = now;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(rating).State = EntityState.Detached;

            var existing = await _db.Ratings.FirstAsync(r => r.ItemId == itemId && r.UserId == uid);
            existing.Stars = request.Stars;
            existing.UpdatedAt = now;
            await _db.SaveChangesAsync();
            created = false;
        }

        var summary = await SummaryAsync(itemId, uid);
        return created ? StatusCode(StatusCodes.Status201Created, summary) : Ok(summary);
    }
    //

    //// Delete
    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> RemoveMine(int itemId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        var rating = await _db.Ratings.FirstOrDefaultAsync(
            r => r.ItemId == itemId && r.UserId == uid);
        if (rating is null)
            return NotFound(new { message = "امتیازی از شما برای این کالا ثبت نشده است." });

        _db.Ratings.Remove(rating);
        await _db.SaveChangesAsync();

        return Ok(await SummaryAsync(itemId, uid));
    }
    //
}