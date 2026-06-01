using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LMSDashboard.Data;
using LMSDashboard.DTOs;
using LMSDashboard.Models;
using ClosedXML.Excel;

namespace LMSDashboard.Services;

public interface IReportService
{
    Task<MonthlyReportDto> GetMonthlyReportAsync(int? year = null, int? month = null);
    Task<byte[]> ExportMonthlyReportToExcelAsync(int? year = null, int? month = null);
    Task CacheNightlyReportAsync();
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;

        var start = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var items = await _db.ContentItems
            .Where(c => !c.IsDeleted && c.CreatedAt >= start && c.CreatedAt < end)
            .ToListAsync();

        var byStatus = Enum.GetNames<ContentStatus>()
            .ToDictionary(s => s, s => items.Count(i => i.Status.ToString() == s));

        var byTrack = Enum.GetNames<Track>()
            .ToDictionary(t => t, t => items.Count(i => i.Track.ToString() == t));

        var byType = Enum.GetNames<ContentType>()
            .ToDictionary(t => t, t => items.Count(i => i.Type.ToString() == t));

        var dailyCounts = items
            .GroupBy(i => i.CreatedAt.Date)
            .Select(g => new DailyCountDto(g.Key, g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        var peak = dailyCounts.OrderByDescending(d => d.Count).FirstOrDefault();
        var peakDay = peak != null ? new PeakDayDto(peak.Date, peak.Count) : null;

        var failedCount = items.Count(i => i.Status == ContentStatus.Failed);
        var errorRate = items.Count > 0 ? Math.Round((double)failedCount / items.Count * 100, 2) : 0;

        return new MonthlyReportDto(items.Count, byStatus, byTrack, byType, peakDay, errorRate, dailyCounts);
    }

    public async Task<byte[]> ExportMonthlyReportToExcelAsync(int? year = null, int? month = null)
    {
        var report = await GetMonthlyReportAsync(year, month);
        using var wb = new XLWorkbook();

        var summary = wb.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Total Items This Month";
        summary.Cell(1, 2).Value = report.TotalItemsThisMonth;
        summary.Cell(2, 1).Value = "Error Rate %";
        summary.Cell(2, 2).Value = report.ErrorRatePercent;

        var byStatus = wb.Worksheets.Add("By Status");
        byStatus.Cell(1, 1).Value = "Status";
        byStatus.Cell(1, 2).Value = "Count";
        int row = 2;
        foreach (var kv in report.ByStatus)
        {
            byStatus.Cell(row, 1).Value = kv.Key;
            byStatus.Cell(row, 2).Value = kv.Value;
            row++;
        }

        var daily = wb.Worksheets.Add("Daily");
        daily.Cell(1, 1).Value = "Date";
        daily.Cell(1, 2).Value = "Count";
        row = 2;
        foreach (var d in report.DailyCounts)
        {
            daily.Cell(row, 1).Value = d.Date.ToString("yyyy-MM-dd");
            daily.Cell(row, 2).Value = d.Count;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task CacheNightlyReportAsync()
    {
        var report = await GetMonthlyReportAsync();
        var json = JsonSerializer.Serialize(report);
        var key = $"monthly_{DateTime.UtcNow:yyyy-MM}";

        var existing = await _db.ReportCaches.FirstOrDefaultAsync(r => r.ReportKey == key);
        if (existing != null)
        {
            existing.DataJson = json;
            existing.GeneratedAt = DateTime.UtcNow;
            existing.ExpiresAt = DateTime.UtcNow.AddHours(25);
        }
        else
        {
            _db.ReportCaches.Add(new ReportCache
            {
                ReportKey = key,
                DataJson = json,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(25)
            });
        }

        await _db.SaveChangesAsync();
    }
}
