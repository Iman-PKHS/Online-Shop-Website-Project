using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Entities.Enums;

namespace OnlineShop.Api.Controllers;

//for current user
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;

    public MeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var me = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.Value && u.IsActive)
            .Select(u => new MeResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                PostalCode = u.PostalCode,
                Address = u.Address,
                HasAddress = u.PostalCode != null && u.PostalCode != "" && u.Address != null && u.Address != "",
                AddressUpdatedAt = u.AddressUpdatedAt
            })
            .FirstOrDefaultAsync();

        return me is null ? Unauthorized() : Ok(me);
    }

    [HttpPut("address")]
    public async Task<IActionResult> UpdateAddress(UpdateAddressRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null || !user.IsActive) return Unauthorized();

        if (!AddressRules.TryNormalize(request.PostalCode, request.Address,
                out var postal, out var address, out var error))
            return BadRequest(new { message = error });

        var now = DateTime.UtcNow;
        user.PostalCode = postal;
        user.Address = address;
        user.AddressUpdatedAt = now;
        await _db.SaveChangesAsync();

        var uid = user.Id;
        await _db.Orders
            .Where(o => o.UserId == uid
                    && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Paid)
                    && (o.ShippingPostalCode != postal || o.ShippingAddress != address))
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.ShippingPostalCode, postal)
                .SetProperty(o => o.ShippingAddress, address)
                .SetProperty(o => o.ShippingAddressChangedAt, (DateTime?)now));

        return Ok(new { postalCode = user.PostalCode, address = user.Address, addressUpdatedAt = user.AddressUpdatedAt });
    }
}