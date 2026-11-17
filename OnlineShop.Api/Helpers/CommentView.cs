using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;

namespace OnlineShop.Api.Helpers;

public static class CommentViews
{
    public static async Task AttachRepliesAsync<T>(AppDbContext db, List<T> comments) where T : CommentResponse
    {
        if (comments.Count == 0) return;
        var ids = comments.Select(c => c.Id).ToList();

        var replies = await db.CommentReplies
            .AsNoTracking()
            .Where(r => ids.Contains(r.CommentId))
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            .Select(r => new { r.CommentId, r.Id, r.Text, r.CreatedAt, r.UpdatedAt })
            .ToListAsync();

        var byComment = replies.GroupBy(r => r.CommentId).ToDictionary(g => g.Key,
            g => g.Select(r => new ReplyResponse
            {
                Id = r.Id, Author = ReviewRules.AdminLabel, Text = r.Text,
                CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
            }).ToList());

        foreach (var c in comments)
            c.Replies = byComment.GetValueOrDefault(c.Id) ?? new List<ReplyResponse>();
    }
}