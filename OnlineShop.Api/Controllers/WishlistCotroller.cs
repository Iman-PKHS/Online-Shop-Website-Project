using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly AppDbContext _db;

    public WishlistController(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<WishlistItem, WishlistItemResponse>> ToLine =
        wi => new WishlistItemResponse
        {
            WishlistItemId = wi.Id,
            ItemId = wi.ItemId,
            Name = wi.Item.Name,
            ImageUrl = wi.Item.ImageUrl,
            Price = wi.Item.Price,
            IsAvailable = wi.Item.IsActive && wi.Item.StockQuantity > 0,
            Message = wi.Item.IsActive && wi.Item.StockQuantity > 0
                ? null
                : CartRules.UnavailableMessage,
            StockLeft = wi.Item.IsActive && wi.Item.StockQuantity > 0 && wi.Item.StockQuantity < 10
                ? wi.Item.StockQuantity
                : (int?)null,
            AddedAt = wi.CreatedAt,
            Purchased = false,
            PurchasedAt = null
        };
    
    ////Get
    [HttpGet]
    public async Task<IActionResult> GetWishlist([FromQuery] PagingQuery paging)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var query = _db.WishlistItems
            .AsNoTracking()
            .Where(wi => wi.Wishlist.UserId == userId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(wi => wi.CreatedAt)
            .ThenByDescending(wi => wi.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(ToLine)
            .ToListAsync();

        return Ok(new PagedResult<WishlistItemResponse>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalCount = totalCount
        });
    }
    //

    ////Post Item
    [HttpPost("items")]
    public async Task<IActionResult> Add(AddToWishlistRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var item = await _db.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.IsActive })
            .FirstOrDefaultAsync();

        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        if (!item.IsActive)
            return BadRequest(new { message = CartRules.UnavailableMessage });

        var wishlist = await CartRules.GetOrCreateWishlistAsync(_db, userId.Value);

        var existing = await _db.WishlistItems
            .Where(wi => wi.WishlistId == wishlist.Id && wi.ItemId == request.ItemId)
            .Select(wi => wi.Id)
            .FirstOrDefaultAsync();

        if (existing != 0)
            return Conflict(new
            {
                message = "این کالا قبلاً در علاقه‌مندی‌های شما ثبت شده است.",
                wishlistItemId = existing
            });

        var line = new WishlistItem { WishlistId = wishlist.Id, ItemId = request.ItemId };
        _db.WishlistItems.Add(line);
        
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "این کالا قبلاً در علاقه‌مندی‌های شما ثبت شده است." });
        }

        var response = await _db.WishlistItems
            .AsNoTracking()
            .Where(wi => wi.Id == line.Id)
            .Select(ToLine)
            .FirstAsync();

        return StatusCode(StatusCodes.Status201Created, response);
    }
    //

    ////Delete Item
    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> Remove(int itemId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var line = await _db.WishlistItems
            .FirstOrDefaultAsync(wi => wi.ItemId == itemId && wi.Wishlist.UserId == userId.Value);

        if (line is null)
            return NotFound(new { message = "این مورد در علاقه‌مندی‌های شما پیدا نشد." });

        _db.WishlistItems.Remove(line);
        await _db.SaveChangesAsync();

        return NoContent();
    }
    //

    ////Post Item to Cart
    [HttpPost("items/{itemId:int}/move-to-cart")]
    public async Task<IActionResult> MoveToCart(int itemId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var wishLine = await _db.WishlistItems
            .Include(wi => wi.Item)
            .FirstOrDefaultAsync(wi => wi.ItemId == itemId && wi.Wishlist.UserId == userId.Value);

        if (wishLine is null)
            return NotFound(new { message = "این مورد در علاقه‌مندی‌های شما پیدا نشد." });

        var item = wishLine.Item;

        if (!item.IsActive || item.StockQuantity == 0)
            return BadRequest(new { message = CartRules.UnavailableMessage });//remain in wishlist

        var cart = await CartRules.GetOrCreateCartAsync(_db, userId.Value);

        var cartLine = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == item.Id);

        var newQuantity = (cartLine?.Quantity ?? 0) + 1;

        if (newQuantity > CartRules.MaxQuantity)
            return BadRequest(new { message = $"حداکثر تعداد برای هر کالا {CartRules.MaxQuantity} است." });

        if (newQuantity > item.StockQuantity)
            return BadRequest(new
            {
                message = CartRules.OnlyLeftMessage(item.StockQuantity),
                availableQuantity = item.StockQuantity
            });

        if (cartLine is null)
        {
            cartLine = new CartItem
            {
                CartId = cart.Id,
                ItemId = item.Id,
                Quantity = 1,
                UnitPriceAtAdd = item.Price
            };
            _db.CartItems.Add(cartLine);
        }
        else
        {
            cartLine.Quantity = newQuantity;
            cartLine.UnitPriceAtAdd = item.Price;
        }

        _db.WishlistItems.Remove(wishLine);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "درخواست هم‌زمان ثبت شد. دوباره تلاش کنید." });
        }

        return Ok(new
        {
            message = "کالا به سبد خرید منتقل شد.",
            cartItemId = cartLine.Id,
            quantity = cartLine.Quantity
        });
    }
    //

    ////clear Wishlist
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var wishlistId = await _db.Wishlists
            .Where(w => w.UserId == userId.Value)
            .Select(w => (int?)w.Id)
            .FirstOrDefaultAsync();

        if (wishlistId is not null)
            await _db.WishlistItems.Where(wi => wi.WishlistId == wishlistId.Value).ExecuteDeleteAsync();

        return NoContent();
    }
    //





}