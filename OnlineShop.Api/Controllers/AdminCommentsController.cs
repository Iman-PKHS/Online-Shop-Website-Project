using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin")]
public class AdminCommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminCommentsController(AppDbContext db)
    {
        _db = db;
    }

    private async Task AttachRepliesAsync(List<AdminCommentResponse> comments)
    {
        if (comments.Count == 0) return;

        var ids = comments.Select(c => c.Id).ToList();

        var replies = await _db.CommentReplies
            .AsNoTracking()
            .Where(r => ids.Contains(r.CommentId))
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Select(r => new
            {
                r.CommentId,
                Reply = new AdminReplyResponse
                {
                    Id = r.Id,
                    AdminUserId = r.AdminUserId,
                    AdminUsername = r.AdminUser.Username,
                    Text = r.Text,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }
            })
            .ToListAsync();

        var byComment = replies
            .GroupBy(r => r.CommentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Reply).ToList());

        foreach (var c in comments)
            c.Replies = byComment.GetValueOrDefault(c.Id) ?? new List<AdminReplyResponse>();
    }

    private async Task<AdminCommentResponse?> LoadAsync(int id)
    {
        var c = await _db.Comments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AdminCommentResponse
            {
                Id = x.Id,
                ItemId = x.ItemId,
                ItemName = x.Item.Name,
                UserId = x.UserId,
                Username = x.User.Username,
                Text = x.Text,
                IsVisible = x.IsVisible,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (c is null) return null;
        await AttachRepliesAsync(new List<AdminCommentResponse> { c });
        return c;
    }

    ////Get with Filters
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagingQuery paging,
        [FromQuery] int? itemId,
        [FromQuery] int? userId,
        [FromQuery] bool? isVisible)
    {
        var query = _db.Comments.AsNoTracking().AsQueryable();

        if (itemId.HasValue)
        {
            var i = itemId.Value;
            query = query.Where(c => c.ItemId == i);
        }

        if (userId.HasValue)
        {
            var u = userId.Value;
            query = query.Where(c => c.UserId == u);
        }

        if (isVisible.HasValue)
        {
            var v = isVisible.Value;
            query = query.Where(c => c.IsVisible == v);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(c => new AdminCommentResponse
            {
                Id = c.Id,
                ItemId = c.ItemId,
                ItemName = c.Item.Name,
                UserId = c.UserId,
                Username = c.User.Username,
                Text = c.Text,
                IsVisible = c.IsVisible,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        await AttachRepliesAsync(items);

        return Ok(new PagedResult<AdminCommentResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////Delete by Comment Id
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id);
        if (comment is null)
            return NotFound(new { message = "نظر پیدا نشد." });

        _db.Comments.Remove(comment);//Cascade Removal
        await _db.SaveChangesAsync();

        return NoContent();
    }
    //

    ////PUT hiding Comments(soft delete)
    [HttpPut("{id:int}/visibility")]
    public async Task<IActionResult> SetVisibility(int id, VisibilityRequest request)
    {
        var updated = await _db.Comments
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsVisible, request.IsVisible));

        if (updated == 0)
            return NotFound(new { message = "نظر پیدا نشد." });

        return Ok(await LoadAsync(id));
    }
    //

    //// POST Reply
    [HttpPost("{id:int}/reply")]
    public async Task<IActionResult> Reply(int id, ReplyRequest request)
    {
        var adminId = User.GetUserId();
        if (adminId is null) return Unauthorized();

        if (!await _db.Comments.AnyAsync(c => c.Id == id))
            return NotFound(new { message = "نظر پیدا نشد." });

        var text = TextNormalizer.Normalize(request.Text);
        if (text.Length == 0 || text.Length > ReviewRules.MaxCommentLength)
            return BadRequest(new { message = "متن پاسخ باید بین 1 تا 2000 کاراکتر باشد." });

        var now = DateTime.UtcNow;
        var reply = new CommentReply
        {
            CommentId = id,
            AdminUserId = adminId.Value,
            Text = text,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.CommentReplies.Add(reply);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadAsync(id));
    }
    //

    //// PUT Replay
    [HttpPut("{commentId:int}/reply/{replyId:int}")]
    public async Task<IActionResult> EditReply(int commentId, int replyId, ReplyRequest request)
    {
        var reply = await _db.CommentReplies
            .FirstOrDefaultAsync(r => r.Id == replyId && r.CommentId == commentId);

        if (reply is null)
            return NotFound(new { message = "پاسخ پیدا نشد." });

        var text = TextNormalizer.Normalize(request.Text);
        if (text.Length == 0 || text.Length > ReviewRules.MaxCommentLength)
            return BadRequest(new { message = "متن پاسخ باید بین 1 تا 2000 کاراکتر باشد." });

        reply.Text = text;
        reply.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await LoadAsync(commentId));
    }
    //
    
    //// Delete Reply
    [HttpDelete("{commentId:int}/reply/{replyId:int}")]
    public async Task<IActionResult> DeleteReply(int commentId, int replyId)
    {
        var reply = await _db.CommentReplies
            .FirstOrDefaultAsync(r => r.Id == replyId && r.CommentId == commentId);

        if (reply is null)
            return NotFound(new { message = "پاسخ پیدا نشد." });

        _db.CommentReplies.Remove(reply);
        await _db.SaveChangesAsync();

        return NoContent();
    }
    //
}