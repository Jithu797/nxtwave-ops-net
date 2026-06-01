using Microsoft.EntityFrameworkCore;
using LMSDashboard.Data;
using LMSDashboard.DTOs;
using LMSDashboard.Models;
using OfficeOpenXml;

namespace LMSDashboard.Services;

public interface IContentService
{
    Task<UploadSummaryDto> UploadFromExcelAsync(Stream fileStream, string uploadedBy);
    Task<PaginatedResult<ContentItemDto>> GetPaginatedAsync(ContentFilterParams filters);
    Task<ContentItemDto?> GetByIdAsync(Guid id);
    Task<ContentItemDto?> UpdateStatusAsync(Guid id, string newStatus);
    Task<bool> SoftDeleteAsync(Guid id);
}

public class ContentService : IContentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ContentService> _logger;

    public ContentService(AppDbContext db, ILogger<ContentService> logger)
    {
        _db = db;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<UploadSummaryDto> UploadFromExcelAsync(Stream fileStream, string uploadedBy)
    {
        var created = 0;
        var failed = 0;
        var errors = new List<string>();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return new UploadSummaryDto(0, 0, new List<string> { "No worksheet found in file." });
        }

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= worksheet.Dimension?.Columns; col++)
        {
            var header = worksheet.Cells[1, col].Text.Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        var required = new[] { "Title", "Type", "Track", "Difficulty" };
        var missing = required.Where(r => !headers.ContainsKey(r)).ToList();
        if (missing.Any())
        {
            return new UploadSummaryDto(0, 0,
                new List<string> { $"Missing required columns: {string.Join(", ", missing)}" });
        }

        var rowCount = worksheet.Dimension?.Rows ?? 1;
        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                var title = worksheet.Cells[row, headers["Title"]].Text.Trim();
                var typeStr = worksheet.Cells[row, headers["Type"]].Text.Trim();
                var trackStr = worksheet.Cells[row, headers["Track"]].Text.Trim();
                var diffStr = worksheet.Cells[row, headers["Difficulty"]].Text.Trim();

                if (string.IsNullOrEmpty(title))
                {
                    errors.Add($"Row {row}: Title is empty.");
                    failed++;
                    continue;
                }

                if (!Enum.TryParse<ContentType>(typeStr, true, out var type))
                {
                    errors.Add($"Row {row}: Invalid Type '{typeStr}'.");
                    failed++;
                    continue;
                }

                if (!Enum.TryParse<Track>(trackStr, true, out var track))
                {
                    errors.Add($"Row {row}: Invalid Track '{trackStr}'.");
                    failed++;
                    continue;
                }

                if (!Enum.TryParse<Difficulty>(diffStr, true, out var difficulty))
                {
                    errors.Add($"Row {row}: Invalid Difficulty '{diffStr}'.");
                    failed++;
                    continue;
                }

                var notes = headers.TryGetValue("Notes", out var notesCol)
                    ? worksheet.Cells[row, notesCol].Text.Trim() : null;

                var item = new ContentItem
                {
                    Title = title,
                    Type = type,
                    Track = track,
                    Difficulty = difficulty,
                    Status = ContentStatus.Pending,
                    CreatedBy = uploadedBy,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ContentItems.Add(item);
                created++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing row {Row}", row);
                errors.Add($"Row {row}: Unexpected error — {ex.Message}");
                failed++;
            }
        }

        await _db.SaveChangesAsync();
        return new UploadSummaryDto(created, failed, errors);
    }

    public async Task<PaginatedResult<ContentItemDto>> GetPaginatedAsync(ContentFilterParams filters)
    {
        var query = _db.ContentItems
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filters.Status) && Enum.TryParse<ContentStatus>(filters.Status, true, out var status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(filters.Track) && Enum.TryParse<Track>(filters.Track, true, out var track))
            query = query.Where(c => c.Track == track);

        if (!string.IsNullOrEmpty(filters.Type) && Enum.TryParse<ContentType>(filters.Type, true, out var type))
            query = query.Where(c => c.Type == type);

        var total = await query.CountAsync();
        var page = Math.Max(1, filters.Page);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToDto(c, null))
            .ToListAsync();

        return new PaginatedResult<ContentItemDto>(items, total, page, pageSize, totalPages);
    }

    public async Task<ContentItemDto?> GetByIdAsync(Guid id)
    {
        var item = await _db.ContentItems
            .Include(c => c.ValidationLogs)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (item == null) return null;

        var logs = item.ValidationLogs
            .OrderByDescending(v => v.CheckedAt)
            .Select(v => new ValidationLogDto(v.Id, v.RuleName, v.Result.ToString(), v.Message, v.CheckedAt))
            .ToList();

        return MapToDto(item, logs);
    }

    public async Task<ContentItemDto?> UpdateStatusAsync(Guid id, string newStatus)
    {
        if (!Enum.TryParse<ContentStatus>(newStatus, true, out var status)) return null;

        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (item == null) return null;

        item.Status = status;
        item.StatusChangedAt = DateTime.UtcNow;

        if (status >= ContentStatus.InBeta && item.BetaUploadedAt == null)
            item.BetaUploadedAt = DateTime.UtcNow;
        if (status >= ContentStatus.Validated && item.ValidatedAt == null)
            item.ValidatedAt = DateTime.UtcNow;
        if (status == ContentStatus.InProduction && item.ProdUploadedAt == null)
            item.ProdUploadedAt = DateTime.UtcNow;

        _db.SyncLogs.Add(new SyncLog
        {
            SheetName = "ContentStatus",
            RowsUpdated = 1,
            SyncedAt = DateTime.UtcNow,
            Status = "Queued"
        });

        await _db.SaveChangesAsync();
        return MapToDto(item, null);
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (item == null) return false;

        item.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }

    private static ContentItemDto MapToDto(ContentItem c, List<ValidationLogDto>? logs) =>
        new(c.Id, c.Title, c.Type.ToString(), c.Track.ToString(), c.Difficulty.ToString(),
            c.Status.ToString(), c.BetaUploadedAt, c.ProdUploadedAt, c.ValidatedAt,
            c.CreatedAt, c.CreatedBy, c.Notes, logs);
}
