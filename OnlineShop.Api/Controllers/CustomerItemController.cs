using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Entities.Enums;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/customers/items")]
public class CustomerItemController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerItemController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Item, ItemDetail>> ToDetail = i => new ItemDetail
    {
        Id = i.Id,
        Name = i.Name,
        Description = i.Description,
        Price = i.Price,
        ImageUrl = i.ImageUrl,
        InStock = i.StockQuantity - i.ReservedQuantity > 0,
        StockLeft = i.StockQuantity - i.ReservedQuantity > 0 && i.StockQuantity - i.ReservedQuantity < 10
            ? i.StockQuantity - i.ReservedQuantity
            : (int?)null,
        AverageRating = i.Ratings.Average(r => (double?)r.Stars),
        RatingCount = i.Ratings.Count(),
        CommentCount = i.Comments.Count(c => c.IsVisible),
        Categories = i.ItemCategories
            .Where(ic => ic.Category.IsActive)
            .Select(ic => new CategorySummary { Id = ic.Category.Id, Name = ic.Category.Name })
            .ToList()
    };

    //// Search Parsing for Category Id
    private static List<int>? ParseIds(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        
        var ids = new List<int>();
        foreach (var part in text.Split( new[] {',',' ', 'و','،'},
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var id)) return null;
            ids.Add(id);
        }
        return ids.Distinct().ToList();
    }
    //

    ////Sorting Options
    private static readonly string[] AllowedSorts =
        { "price_asc", "price_desc", "popular", "newest", "best_selling" };
    //

    ////Get
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ItemListQuery q)
    {
        //// Price Input Validation
        if (q.MinPrice.HasValue && q.MaxPrice.HasValue && q.MinPrice > q.MaxPrice)
            return BadRequest(new { message = "حداقل قیمت نمی‌تواند از حداکثر قیمت بیشتر باشد." });
        //

        ////Sorting Validation for Price Order
        var sort = q.Sort?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(sort) && !AllowedSorts.Contains(sort))
            return BadRequest(new
            {
                message = "مرتب‌سازی نامعتبر است.",
                allowed = AllowedSorts
            });
        //

        ////CategoryID Input Validation
        List<int>? categoryIds = null;
        if (!string.IsNullOrWhiteSpace(q.CategoryIds))
        {
            categoryIds = ParseIds(q.CategoryIds);
            if (categoryIds is null)
                return BadRequest(new { message = "شناسه دسته‌بندی‌ها باید فهرستی از اعداد باشند که با فاصله و یا ویرگول جدا شده‌اند.. " });
        }
        //

        ////Categories Name Input
        if (!string.IsNullOrWhiteSpace(q.Categories))
        {
            var names = q.Categories
                .Split(new[] { ',', '،' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(TextNormalizer.Normalize)
                .ToList();

            var activeCategories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var matchedIds = activeCategories
                .Where(c => names.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .Select(c => c.Id)
                .ToList();

            categoryIds = (categoryIds ?? new List<int>()).Concat(matchedIds).Distinct().ToList();
        }
        //

        ////Query
        var query = _db.Items.AsNoTracking().Where(i => i.IsActive);

        query = query.ApplySearch(q.Search, activeCategoriesOnly: true);

        if (categoryIds is not null)
            query = query.Where(i => i.ItemCategories.Any(ic =>
                ic.Category.IsActive && categoryIds.Contains(ic.CategoryId)));

        if (q.MinPrice.HasValue)
            query = query.Where(i => i.Price >= q.MinPrice.Value);

        if (q.MaxPrice.HasValue)
            query = query.Where(i => i.Price <= q.MaxPrice.Value);

        if (q.InStock == true)
            query = query.Where(i => i.StockQuantity - i.ReservedQuantity > 0);
        
        //

        ////Counting
        var totalCount = await query.CountAsync();
        //

        ////Sorting
        var ordered = sort switch
        {
            "price_asc"  => query.OrderBy(i => i.Price).ThenBy(i => i.Id),
            "price_desc" => query.OrderByDescending(i => i.Price).ThenBy(i => i.Id),

            "newest" => query.OrderByDescending(i => i.CreatedAt)
                .ThenByDescending(i => i.Id),

            "popular" => query.OrderByDescending(i =>
                (i.Ratings.Average(r => (double?)r.Stars) ?? 0) * 10 + i.Comments.Count(c => c.IsVisible))
                .ThenByDescending(i => i.Id),

            "best_selling" => query.OrderByDescending(i => _db.OrderItems
                .Where(oi => oi.ItemId == i.Id && oi.Order.PaymentStatus == PaymentStatus.Paid)
                .Sum(oi => (int?)oi.Quantity) ?? 0)
                .ThenByDescending(i => i.Id),

                _ => query.OrderByDescending(i => i.StockQuantity - i.ReservedQuantity > 0)
                        .ThenByDescending(i => i.CreatedAt)
                        .ThenByDescending(i => i.Id)
        };
        //

        ////Paging Base on Counting and Mapping Base on Sorting
        var items = await ordered
            .Skip(q.Skip)
            .Take(q.PageSize)
            .Select(ItemMappings.ToSummary)
            .ToListAsync();
        //

        return Ok(new PagedResult<ItemSummary>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////Get by ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Where(i => i.Id == id && i.IsActive)
            .Select(ToDetail)
            .FirstOrDefaultAsync();
        
        return item is null
            ? NotFound(new { message = "کالا مورد نظر یافت نشد."})
            : Ok (item);
    }















}