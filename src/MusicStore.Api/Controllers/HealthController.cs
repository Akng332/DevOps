using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Data;

namespace MusicStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!await _db.Database.CanConnectAsync())
            return StatusCode(503, new { status = "Unhealthy" });
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
