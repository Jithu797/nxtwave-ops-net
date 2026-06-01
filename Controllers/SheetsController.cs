using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSDashboard.Data;
using LMSDashboard.DTOs;
using LMSDashboard.Services;
using Microsoft.EntityFrameworkCore;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/sheets")]
[Authorize]
public class SheetsController : ControllerBase
{
    private readonly ISheetsService _sheets;
    private readonly AppDbContext _db;

    public SheetsController(ISheetsService sheets, AppDbContext db)
    {
        _sheets = sheets;
        _db = db;
    }

    /// <summary>Manually trigger a Google Sheets sync for items changed in the last 24 hours.</summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    public async Task<IActionResult> Sync()
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var count = await _sheets.SyncRecentChangesAsync(since);
        return Ok(ApiResponse<int>.Ok(count, $"Synced {count} items to Google Sheets."));
    }

    /// <summary>Get recent sync logs.</summary>
    [HttpGet("logs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logs([FromQuery] int take = 10)
    {
        var logs = await _db.SyncLogs
            .OrderByDescending(l => l.SyncedAt)
            .Take(Math.Min(take, 100))
            .ToListAsync();
        return Ok(ApiResponse<object>.Ok(logs));
    }
}
