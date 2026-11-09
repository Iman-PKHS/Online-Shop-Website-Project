using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Api.Helpers;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly IPaymentService _payments;

    public CheckoutController(IPaymentService payments)
    {
        _payments = payments;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _payments.InitiateAsync(userId.Value, baseUrl);

        return result.Outcome switch
        {
            InitiateOutcome.CartEmpty =>
                BadRequest(new { message = "سبد خرید شما خالی است." }),

            InitiateOutcome.ProblemsFound =>
                Conflict(new
                {
                    message = "برخی کالاهای سبد خرید قابل خرید نیستند.",
                    problems = result.Problems
                }),

            _ => Ok(result.Response)
        };
    }
}