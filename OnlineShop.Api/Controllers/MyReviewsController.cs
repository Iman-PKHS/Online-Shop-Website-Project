using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class MyReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    public MyReviewsController(AppDbContext db) => _db = db;

    private async Task<MyCommentResponse?> LoadAsync(int id, int uid)
    {
        var c = await _db.Comments.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == uid)
            .Select(x => new MyCommentResponse
            {
                Id = x.Id, ItemId = x.ItemId, Username = x.User.Username,
                Text = x.Text, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
                IsMine = true, IsHidden = !x.IsVisible,
                ItemName = x.Item.Name, ItemImageUrl = x.Item.ImageUrl
            })
            .FirstOrDefaultAsync();

        if (c is not null)
            await CommentViews.AttachRepliesAsync(_db, new List<MyCommentResponse> { c });
        return c;
    }

    [HttpGet("comments/my")]
    public async Task<IActionResult> MyComments([FromQuery] PagingQuery paging, [FromQuery] int? itemId)
    {
        var uid = User.GetUserId();
        if (uid is null) return Unauthorized();
        var me = uid.Value;

        var query = _db.Comments.AsNoTracking().Where(c => c.UserId == me);
        if (itemId.HasValue)
        {
            var i = itemId.Value;
            query = query.Where(c => c.ItemId == i);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new MyCommentResponse
            {
                Id = x.Id, ItemId = x.ItemId, Username = x.User.Username,
                Text = x.Text, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt,
                IsMine = true, IsHidden = !x.IsVisible,
                ItemName = x.Item.Name, ItemImageUrl = x.Item.ImageUrl
            })
            .ToListAsync();

        await CommentViews.AttachRepliesAsync(_db, items);

        return Ok(new PagedResult<MyCommentResponse>
        {
            Items = items, Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }

    [HttpPut("comments/{commentId:int}")]
    public async Task<IActionResult> EditComment(int commentId, CommentRequest request)
    {
        var uid = User.GetUserId();
        if (uid is null) return Unauthorized();

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == uid.Value);
        if (comment is null) return NotFound(new { message = "نظر پیدا نشد." });

        var text = TextNormalizer.Normalize(request.Text);
        if (text.Length == 0 || text.Length > ReviewRules.MaxCommentLength)
            return BadRequest(new { message = "متن نظر باید بین 1 تا 2000 کاراکتر باشد." });

        comment.Text = text;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await LoadAsync(commentId, uid.Value));
    }

    [HttpDelete("comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var uid = User.GetUserId();
        if (uid is null) return Unauthorized();

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == uid.Value);
        if (comment is null) return NotFound(new { message = "نظر پیدا نشد." });

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("ratings/my")]
    public async Task<IActionResult> MyRatings([FromQuery] PagingQuery paging)
    {
        var uid = User.GetUserId();
        if (uid is null) return Unauthorized();
        var me = uid.Value;

        var query = _db.Ratings.AsNoTracking().Where(r => r.UserId == me);
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(r => new MyRatingResponse
            {
                RatingId = r.Id, ItemId = r.ItemId, ItemName = r.Item.Name,
                ItemImageUrl = r.Item.ImageUrl, Stars = r.Stars,
                CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<MyRatingResponse>
        {
            Items = items, Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }
}