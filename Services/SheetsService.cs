using LMSDashboard.Data;
using LMSDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace LMSDashboard.Services;

public interface ISheetsService
{
    Task<int> SyncRecentChangesAsync(DateTime since);
    Task<int> SyncAllAsync();
}

public class SheetsService : ISheetsService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SheetsService> _logger;
    private readonly IConfiguration _config;

    public SheetsService(AppDbContext db, ILogger<SheetsService> logger, IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _config = config;
    }

    public async Task<int> SyncRecentChangesAsync(DateTime since)
    {
        var items = await _db.ContentItems
            .Where(c => !c.IsDeleted && c.StatusChangedAt >= since)
            .ToListAsync();

        return await SyncItemsAsync(items);
    }

    public async Task<int> SyncAllAsync()
    {
        var items = await _db.ContentItems
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        return await SyncItemsAsync(items);
    }

    private async Task<int> SyncItemsAsync(List<ContentItem> items)
    {
        if (!items.Any()) return 0;

        var useRealApi = _config.GetValue<bool>("GoogleSheets:Enabled");
        int rowsUpdated;

        if (useRealApi)
            rowsUpdated = await SyncToGoogleSheetsAsync(items);
        else
            rowsUpdated = MockSync(items);

        _db.SyncLogs.Add(new SyncLog
        {
            SheetName = _config["GoogleSheets:SheetName"] ?? "LMSContent",
            RowsUpdated = rowsUpdated,
            SyncedAt = DateTime.UtcNow,
            Status = "Success"
        });

        await _db.SaveChangesAsync();
        return rowsUpdated;
    }

    private int MockSync(List<ContentItem> items)
    {
        _logger.LogInformation("[MOCK Sheets Sync] Would sync {Count} items to Google Sheets.", items.Count);
        foreach (var item in items)
            _logger.LogDebug("[MOCK] Item {Id} | {Title} | Status={Status}", item.Id, item.Title, item.Status);
        return items.Count;
    }

    private async Task<int> SyncToGoogleSheetsAsync(List<ContentItem> items)
    {
        // Real Google Sheets API integration placeholder.
        // Requires credentials at GoogleSheets:CredentialsPath and sheet ID at GoogleSheets:SpreadsheetId.
        _logger.LogInformation("Real Google Sheets sync for {Count} items — not yet wired.", items.Count);
        await Task.CompletedTask;
        return items.Count;
    }
}
