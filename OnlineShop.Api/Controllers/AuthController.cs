using System.Security.Claims;
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
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public AuthController(AppDbContext db, IPasswordService passwords, ITokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var Id) ? Id : null;
    }
    
    //Register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var username = TextNormalizer.Normalize(request.Username);
        var email = request.Email.Trim();

        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Conflict(new { message = "این نام کاربری توسط کاربر دیگری انتخاب شده است." });

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "این ایمیل قبلاً ثبت شده است." });

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = _passwords.HashPassword(request.Password),
            Role = UserRole.Customer
        };
        
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch  (DbUpdateException)
        {
            return Conflict(new { message = "این نام کاریری و یا ایمیل توسط کاربر دیگری درحال استفاده است." });
        }
        return Ok(_tokens.CreateToken(user));
    }
    ////
    
    //Login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var input = TextNormalizer.Normalize(request.UsernameOrEmail);

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == input || u.Email == input);

        if (user is null || !_passwords.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "حساب کاربری با این مشخصات یافت نشد."});

        if (!user.IsActive)
            return Unauthorized(new { message = "این حساب کاربری غیرفعال است."});

        return Ok(_tokens.CreateToken(user));
    }
    ////
    
    [Authorize]
    [HttpPut("change-username")]
    public async Task<IActionResult> ChangeUsername(ChangeUsernameRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null || !user.IsActive)
            return Unauthorized();

        var newUsername = TextNormalizer.Normalize(request.NewUsername);

        if (await _db.Users.AnyAsync(u => u.Username == newUsername && u.Id != user.Id))
            return Conflict(new { message = "این نام کاربری توسط کاربر دیگری انتخاب شده است." });

        user.Username = newUsername;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
             return Conflict(new { message = "این نام کاربری توسط کاربر دیگری انتخاب شده است." });
        }
        
        return Ok(_tokens.CreateToken(user));
    }
    
    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null || !user.IsActive)
            return Unauthorized();

        if (!_passwords.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "رمز عبور فعلی اشتباه است." });

        user.PasswordHash = _passwords.HashPassword(request.NewPassword);

        await _db.SaveChangesAsync();

        return Ok(new { message = "رمز عبور با موفقیت تغییر یافت." });
    }
}