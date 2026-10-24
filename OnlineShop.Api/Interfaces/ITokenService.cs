using OnlineShop.Api.DTOs;
using OnlineShop.Api.Entities;

namespace OnlineShop.Api.Interfaces;

public interface ITokenService
{
    AuthResponse CreateToken(User user);
}