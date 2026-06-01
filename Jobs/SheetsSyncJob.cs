using LMSDashboard.Services;

namespace LMSDashboard.Jobs;

public class SheetsSyncJob
{
    private readonly ISheetsService _sheets;
    private readonly IJobRecordService _jobs;
    private readonly ILogger<SheetsSyncJob> _logger;

    public SheetsSyncJob(ISheetsService sheets, IJobRecordService jobs, ILogger<SheetsSyncJob> logger)
    {
        _sheets = sheets;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task SyncPendingStatusChanges()
    {
        _logger.LogInformation("[SheetsSyncJob] Starting 15-min sync.");
        var record = await _jobs.StartAsync("SheetsSyncJob");

        try
        {
            var since = DateTime.UtcNow.AddMinutes(-15);
            var count = await _sheets.SyncRecentChangesAsync(since);
            await _jobs.CompleteAsync(record.Id, $"Synced {count} items.");
            _logger.LogInformation("[SheetsSyncJob] Synced {Count} items.", count);
        }
        catch (Exception ex)
        {
            await _jobs.FailAsync(record.Id, ex.Message);
            _logger.LogError(ex, "[SheetsSyncJob] Failed.");
            throw;
        }
    }
}
