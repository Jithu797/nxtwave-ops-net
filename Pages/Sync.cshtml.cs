using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSDashboard.Data;
using LMSDashboard.Models;
using LMSDashboard.Services;

namespace LMSDashboard.Pages;

public class SyncModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ISheetsService _sheets;

    public SyncModel(AppDbContext db, ISheetsService sheets)
    {
        _db = db;
        _sheets = sheets;
    }

    public List<SyncLog> RecentLogs { get; set; } = new();
    public DateTime? LastSync { get; set; }
    public string? SyncMessage { get; set; }

    public async Task OnGetAsync()
    {
        RecentLogs = await _db.SyncLogs
            .OrderByDescending(l => l.SyncedAt)
            .Take(10)
            .ToListAsync();

        LastSync = RecentLogs.FirstOrDefault()?.SyncedAt;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var count = await _sheets.SyncAllAsync();
        SyncMessage = $"Sync triggered — {count} items synced.";
        await OnGetAsync();
        return Page();
    }
}
