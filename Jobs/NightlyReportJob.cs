using LMSDashboard.Services;

namespace LMSDashboard.Jobs;

public class NightlyReportJob
{
    private readonly IReportService _reports;
    private readonly IJobRecordService _jobs;
    private readonly ILogger<NightlyReportJob> _logger;

    public NightlyReportJob(IReportService reports, IJobRecordService jobs, ILogger<NightlyReportJob> logger)
    {
        _reports = reports;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task GenerateNightlyReport()
    {
        _logger.LogInformation("[NightlyReportJob] Generating and caching monthly report.");
        var record = await _jobs.StartAsync("NightlyReportJob");

        try
        {
            await _reports.CacheNightlyReportAsync();
            await _jobs.CompleteAsync(record.Id, "Monthly report cached successfully.");
            _logger.LogInformation("[NightlyReportJob] Done.");
        }
        catch (Exception ex)
        {
            await _jobs.FailAsync(record.Id, ex.Message);
            _logger.LogError(ex, "[NightlyReportJob] Failed.");
            throw;
        }
    }
}
