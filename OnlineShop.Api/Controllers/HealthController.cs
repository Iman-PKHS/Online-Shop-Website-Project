using Microsoft.AspNetCore.Mvc;

namespace OnlineShop.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // throw new Exception("test");
        return Ok(new { status = "ok" });
    }

}