using LMSDashboard.Data;
using LMSDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace LMSDashboard.Services;

public interface IJobRecordService
{
    Task<JobRecord> StartAsync(string jobType, string? payloadJson = null);
    Task CompleteAsync(Guid jobId, string result);
    Task FailAsync(Guid jobId, string errorMessage);
    Task<List<JobRecord>> GetRecentAsync(int take = 20);
}

public class JobRecordService : IJobRecordService
{
    private readonly AppDbContext _db;

    public JobRecordService(AppDbContext db) => _db = db;

    public async Task<JobRecord> StartAsync(string jobType, string? payloadJson = null)
    {
        var record = new JobRecord
        {
            JobType = jobType,
            PayloadJson = payloadJson,
            StartedAt = DateTime.UtcNow
        };
        _db.JobRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task CompleteAsync(Guid jobId, string result)
    {
        var record = await _db.JobRecords.FindAsync(jobId);
        if (record == null) return;
        record.CompletedAt = DateTime.UtcNow;
        record.Result = result;
        await _db.SaveChangesAsync();
    }

    public async Task FailAsync(Guid jobId, string errorMessage)
    {
        var record = await _db.JobRecords.FindAsync(jobId);
        if (record == null) return;
        record.CompletedAt = DateTime.UtcNow;
        record.Result = $"FAILED: {errorMessage}";
        await _db.SaveChangesAsync();
    }

    public async Task<List<JobRecord>> GetRecentAsync(int take = 20)
    {
        return await _db.JobRecords
            .OrderByDescending(j => j.StartedAt)
            .Take(Math.Min(take, 100))
            .ToListAsync();
    }
}
