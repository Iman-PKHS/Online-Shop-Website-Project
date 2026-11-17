using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Api.Data;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;
using OnlineShop.Api.Entities.Enums;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/admin/admins")]
[Authorize(Roles = "Admin")]
public class AdminAdminsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwords;

    public AdminAdminsController(AppDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAdminRequest request)
    {
        var username = TextNormalizer.NormalizeUsername(request.Username);
        var email = request.Email.Trim();

        if (username.Length < 3 || username.Length > 50)
            return BadRequest(new { message = "نام کاربری باید بین 3 تا 50 کاراکتر باشد." });

        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Conflict(new { message = "این نام کاربری قبلاً ثبت شده است." });

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "این ایمیل قبلاً ثبت شده است." });

        var admin = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwords.HashPassword(request.Password),
            Role = UserRole.Admin
        };

        _db.Users.Add(admin);

        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "نام کاربری یا ایمیل قبلاً ثبت شده است." });
        }

        return StatusCode(StatusCodes.Status201Created,
            new { id = admin.Id, username = admin.Username, email = admin.Email, role = "Admin" });
    }

    [HttpPut("{userId:int}/promote")]
    public async Task<IActionResult> Promote(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { message = "کاربر پیدا نشد." });
        if (!user.IsActive) return Conflict(new { message = "کاربر غیرفعال را نمی‌توان مدیر کرد." });
        if (user.Role == UserRole.Admin) return Ok(new { message = "این کاربر مدیر است." });

        user.Role = UserRole.Admin;
        await _db.SaveChangesAsync();
        return Ok(new { message = "کاربر مدیر شد. باید دوباره وارد شود تا دسترسی جدید فعال شود." });
    }

    [HttpPut("{userId:int}/demote")]
    public async Task<IActionResult> Demote(int userId)
    {
        var me = User.GetUserId();
        if (me == userId)
            return Conflict(new { message = "نمی‌توانید نقش خودتان را کاهش دهید." });

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { message = "کاربر پیدا نشد." });
        if (user.Role != UserRole.Admin) return Ok(new { message = "این کاربر مدیر نیست." });

        var otherAdmins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != userId);
        if (otherAdmins == 0)
            return Conflict(new { message = "آخرین مدیر فعال را نمی‌توان کاهش داد." });

        user.Role = UserRole.Customer;
        await _db.SaveChangesAsync();
        return Ok(new { message = "نقش مدیر برداشته شد." });
    }
}