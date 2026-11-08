using System.Security.Claims;

namespace OnlineShop.Api.Helpers;

public static class ClaimsExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out int id) ? id : null;
    }
}