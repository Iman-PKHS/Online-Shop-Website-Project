using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Api.DTOs;
using OnlineShop.Api.Interfaces;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(PaymentCallbackRequest request)
    {
        var status = request.Status.Trim().ToLowerInvariant();

        if (status is not ("success" or "failed"))
            return BadRequest(new { message = "مقدار status باید success یا failed باشد." });

        var result = await _payments.ProcessCallbackAsync(request.ReferenceId, status == "success");

        return result.Outcome == CallbackOutcome.NotFound
            ? NotFound(new { message = "پرداخت مورد نظر یافت نشد." })
            : Ok(result.Result);
    }
}