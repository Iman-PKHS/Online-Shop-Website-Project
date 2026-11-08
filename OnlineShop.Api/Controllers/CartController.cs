using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Helpers;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartController(AppDbContext db)
    {
        _db = db;
    }
    
    ////Cart Builder
    private async Task<CartResponse> BuildCartAsync(int userId)
    {
        var rows = await _db.CartItems
            .AsNoTracking()
            .Where(ci => ci.Cart.UserId == userId)
            .OrderBy(ci => ci.Id)
            .Select(ci => new
            {
                CartItemId = ci.Id,
                ci.ItemId,
                ItemName = ci.Item.Name,
                ci.Item.ImageUrl,
                CurrentPrice = ci.Item.Price,
                ci.UnitPriceAtAdd,
                ci.Quantity,
                ci.Item.IsActive,
                ci.Item.StockQuantity
            })
            .ToListAsync();

        var lines = rows.Select(r =>
        {
            var unavailable = !r.IsActive || r.StockQuantity == 0;
            var notEnough = !unavailable && r.StockQuantity < r.Quantity;
            var isAvailable = !unavailable && !notEnough;

            return new CartItemResponse
            {
                CartItemId = r.CartItemId,
                ItemId = r.ItemId,
                Name = r.ItemName,
                ImageUrl = r.ImageUrl,
                UnitPrice = r.CurrentPrice,
                PriceAtAdd = r.UnitPriceAtAdd,
                PriceChanged = r.CurrentPrice != r.UnitPriceAtAdd,
                Quantity = r.Quantity,
                LineTotal = r.CurrentPrice * r.Quantity,
                IsAvailable = isAvailable,
                Message = unavailable ? CartRules.UnavailableMessage
                        : notEnough   ? CartRules.OnlyLeftMessage(r.StockQuantity)
                        : null,
                AvailableQuantity = notEnough ? r.StockQuantity : null,
                StockLeft = r.StockQuantity > 0 && r.StockQuantity < 10 ? r.StockQuantity : null
            };
        }).ToList();

        return new CartResponse
        {
            Lines = lines,
            ItemCount = lines.Sum(l => l.Quantity),
            TotalAmount = lines.Where(l => l.IsAvailable).Sum(l => l.LineTotal),
            HasUnavailableItems = lines.Any(l => !l.IsAvailable)
        };
    }
    //

    ////Get
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        return Ok(await BuildCartAsync(userId.Value));
    }

    ////Post Item
    [HttpPost("items")]
    public async Task<IActionResult> Add(AddToCartRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var item = await _db.Items
            .AsNoTracking()
            .Where(i => i.Id == request.ItemId)
            .Select(i => new { i.Price, i.IsActive, i.StockQuantity })
            .FirstOrDefaultAsync();

        if (item is null)
            return NotFound(new { message = "کالای مورد نظر یافت نشد." });

        if (!item.IsActive || item.StockQuantity == 0)
            return BadRequest(new { message = CartRules.UnavailableMessage });

        var cart = await CartRules.GetOrCreateCartAsync(_db, userId.Value);

        var line = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == request.ItemId);

        if (line is not null && !request.IncreaseIfExists)
        {
            return Conflict(new
            {
                message = "این کالا از قبل در سبد خرید شما هست.",
                cartItemId = line.Id,
                currentQuantity = line.Quantity
            });
        }

        var newQuantity = (line?.Quantity ?? 0) + request.Quantity;

        if (newQuantity > CartRules.MaxQuantity)
            return BadRequest(new { message = $"حداکثر تعداد برای هر کالا {CartRules.MaxQuantity} است." });

        if (newQuantity > item.StockQuantity)
            return BadRequest(new
            {
                message = CartRules.OnlyLeftMessage(item.StockQuantity),
                availableQuantity = item.StockQuantity
            });

        if (line is null)
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ItemId = request.ItemId,
                Quantity = newQuantity,
                UnitPriceAtAdd = item.Price
            });
        }
        else
        {
            line.Quantity = newQuantity;
            line.UnitPriceAtAdd = item.Price;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ////double click
            return Conflict(new { message = "درخواست هم‌زمان ثبت شد. دوباره تلاش کنید." });
            //
        }

        return Ok(await BuildCartAsync(userId.Value));
    }
    //

    ////put Item Set Quantity
    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateQuantity(int itemId, UpdateCartItemRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var line = await _db.CartItems
            .Include(ci => ci.Item)
            .FirstOrDefaultAsync(ci => ci.ItemId == itemId && ci.Cart.UserId == userId.Value);

        if (line is null)
            return NotFound(new { message = "این مورد در سبد خرید شما پیدا نشد." });

        ////stock check fo increasing quantity
        if (request.Quantity > line.Quantity)
        {
            if (!line.Item.IsActive || line.Item.StockQuantity == 0)
                return BadRequest(new { message = CartRules.UnavailableMessage });

            if (request.Quantity > line.Item.StockQuantity)
                return BadRequest(new
                {
                    message = CartRules.OnlyLeftMessage(line.Item.StockQuantity),
                    availableQuantity = line.Item.StockQuantity
                });
        }
        //

        line.Quantity = request.Quantity;
        line.UnitPriceAtAdd = line.Item.Price;

        await _db.SaveChangesAsync();

        return Ok(await BuildCartAsync(userId.Value));
    }
    //

    ////Delete Item
    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var line = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.ItemId == itemId && ci.Cart.UserId == userId.Value);

        if (line is null)
            return NotFound(new { message = "این مورد در سبد خرید شما پیدا نشد." });

        _db.CartItems.Remove(line);
        await _db.SaveChangesAsync();

        return Ok(await BuildCartAsync(userId.Value));
    }
    //

    ////clear Cart
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var cartId = await _db.Carts
            .Where(c => c.UserId == userId.Value)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        if (cartId is not null)
            await _db.CartItems.Where(ci => ci.CartId == cartId.Value).ExecuteDeleteAsync();

        return NoContent();
    }
    //







}