using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/items/{itemId:int}/comments")]
public class ItemCommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ItemCommentsController(AppDbContext db)
    {
        _db = db;
    }

    // Replies load in a secondary query
    private async Task AttachRepliesAsync(List<CommentResponse> comments)
    {
        if (comments.Count == 0) return;

        var ids = comments.Select(c => c.Id).ToList();

        var replies = await _db.CommentReplies
            .AsNoTracking()
            .Where(r => ids.Contains(r.CommentId))
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Select(r => new { r.CommentId, r.Id, r.Text, r.CreatedAt, r.UpdatedAt })
            .ToListAsync();

        var byComment = replies
            .GroupBy(r => r.CommentId)
            .ToDictionary(g => g.Key, g => g.Select(r => new ReplyResponse
            {
                Id = r.Id,
                Author = ReviewRules.AdminLabel,
                Text = r.Text,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList());

        foreach (var c in comments)
            c.Replies = byComment.GetValueOrDefault(c.Id) ?? new List<ReplyResponse>();
    }

    private async Task<CommentResponse?> LoadAsync(int commentId, int me)
    {
        var c = await _db.Comments
            .AsNoTracking()
            .Where(x => x.Id == commentId)
            .Select(x => new CommentResponse
            {
                Id = x.Id,
                ItemId = x.ItemId,
                Username = x.User.Username,
                Text = x.Text,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsMine = x.UserId == me,
                IsHidden = !x.IsVisible
            })
            .FirstOrDefaultAsync();

        if (c is null) return null;
        await AttachRepliesAsync(new List<CommentResponse> { c });
        return c;
    }

    ////GET Comments
    [HttpGet]
    public async Task<IActionResult> GetAll(int itemId, [FromQuery] PagingQuery paging)
    {
        if (!await ReviewRules.ItemIsPublicAsync(_db, itemId))
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        var me = User.GetUserId() ?? 0;

        var query = _db.Comments
            .AsNoTracking()
            .Where(c => c.ItemId == itemId && (c.IsVisible || c.UserId == me));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                ItemId = c.ItemId,
                Username = c.User.Username,
                Text = c.Text,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsMine = c.UserId == me,
                IsHidden = !c.IsVisible
            })
            .ToListAsync();

        await AttachRepliesAsync(items);

        return Ok(new PagedResult<CommentResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////POST Create
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(int itemId, CommentRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        if (!await ReviewRules.IsActiveUserAsync(_db, uid)) return Unauthorized();

        if (!await ReviewRules.ItemIsPublicAsync(_db, itemId))
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        if (!await ReviewRules.CanReviewAsync(_db, uid, itemId))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ReviewRules.NotBuyerMessage });

        var text = TextNormalizer.Normalize(request.Text);
        if (text.Length == 0 || text.Length > ReviewRules.MaxCommentLength)
            return BadRequest(new { message = "متن نظر باید بین 1 تا 2000 کاراکتر باشد." });

        var already = await _db.Comments.CountAsync(c => c.ItemId == itemId && c.UserId == uid);
        if (already >= ReviewRules.MaxCommentsPerItem)
            return Conflict(new { message = $"حداکثر {ReviewRules.MaxCommentsPerItem} نظر برای هر کالا مجاز است." });

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            ItemId = itemId,
            UserId = uid,
            Text = text,
            IsVisible = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        var response = await LoadAsync(comment.Id, uid);
        return StatusCode(StatusCodes.Status201Created, response);
    }
    //

    ////PUT by Comment Id for editing
    [Authorize]
    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> Edit(int itemId, int commentId, CommentRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.ItemId == itemId && c.UserId == uid);

        if (comment is null)
            return NotFound(new { message = "نظر پیدا نشد." });

        var text = TextNormalizer.Normalize(request.Text);
        if (text.Length == 0 || text.Length > ReviewRules.MaxCommentLength)
            return BadRequest(new { message = "متن نظر باید بین 1 تا 2000 کاراکتر باشد." });

        comment.Text = text;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await LoadAsync(commentId, uid));
    }
    //

    //// Delete by Comment Id
    [Authorize]
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int itemId, int commentId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        var uid = userId.Value;

        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.ItemId == itemId && c.UserId == uid);

        if (comment is null)
            return NotFound(new { message = "نظر پیدا نشد." });

        _db.Comments.Remove(comment);// Cascade removal of Replies
        await _db.SaveChangesAsync();

        return NoContent();
    }
    //
}
