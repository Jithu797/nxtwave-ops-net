using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSDashboard.Data;
using LMSDashboard.DTOs;
using LMSDashboard.Models;

namespace LMSDashboard.Pages;

public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;

    public DashboardModel(AppDbContext db) => _db = db;

    public int TotalItems { get; set; }
    public int InBeta { get; set; }
    public int Validated { get; set; }
    public int InProduction { get; set; }
    public List<ContentItemDto> RecentItems { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public Dictionary<string, int> TrackCounts { get; set; } = new();

    public async Task OnGetAsync()
    {
        var all = await _db.ContentItems.Where(c => !c.IsDeleted).ToListAsync();

        TotalItems = all.Count;
        InBeta = all.Count(c => c.Status == ContentStatus.InBeta);
        Validated = all.Count(c => c.Status == ContentStatus.Validated);
        InProduction = all.Count(c => c.Status == ContentStatus.InProduction);

        StatusCounts = Enum.GetNames<ContentStatus>()
            .ToDictionary(s => s, s => all.Count(c => c.Status.ToString() == s));

        TrackCounts = Enum.GetNames<Track>()
            .ToDictionary(t => t, t => all.Count(c => c.Track.ToString() == t));

        RecentItems = all
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .Select(c => new ContentItemDto(c.Id, c.Title, c.Type.ToString(), c.Track.ToString(),
                c.Difficulty.ToString(), c.Status.ToString(), c.BetaUploadedAt, c.ProdUploadedAt,
                c.ValidatedAt, c.CreatedAt, c.CreatedBy, c.Notes))
            .ToList();
    }
}
