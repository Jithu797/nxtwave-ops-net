using Microsoft.EntityFrameworkCore;
using LMSDashboard.Data;
using LMSDashboard.DTOs;
using LMSDashboard.Models;

namespace LMSDashboard.Services;

public interface IValidationService
{
    Task<List<ValidationResultDto>> ValidateAsync(Guid contentItemId);
}

public class ValidationService : IValidationService
{
    private readonly AppDbContext _db;

    public ValidationService(AppDbContext db) => _db = db;

    public async Task<List<ValidationResultDto>> ValidateAsync(Guid contentItemId)
    {
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentItemId && !c.IsDeleted);
        if (item == null) return new();

        var results = new List<(string Rule, bool Passed, string Msg)>
        {
            ("TitleNotEmpty",
                !string.IsNullOrWhiteSpace(item.Title),
                string.IsNullOrWhiteSpace(item.Title) ? "Title must not be empty." : "Title is present."),

            ("TypeIsValidEnum",
                Enum.IsDefined(item.Type),
                Enum.IsDefined(item.Type) ? $"Type '{item.Type}' is valid." : $"Type '{item.Type}' is not a valid enum value."),

            ("TrackIsValidEnum",
                Enum.IsDefined(item.Track),
                Enum.IsDefined(item.Track) ? $"Track '{item.Track}' is valid." : $"Track '{item.Track}' is not a valid enum value."),

            ("BetaUploadedAtRequiredForBetaStatus",
                item.Status < ContentStatus.InBeta || item.BetaUploadedAt.HasValue,
                item.Status >= ContentStatus.InBeta && !item.BetaUploadedAt.HasValue
                    ? "BetaUploadedAt is required when status is InBeta or later."
                    : "BetaUploadedAt check passed.")
        };

        var logs = results.Select(r => new ValidationLog
        {
            ContentItemId = contentItemId,
            RuleName = r.Rule,
            Result = r.Passed ? Models.ValidationResult.Pass : Models.ValidationResult.Fail,
            Message = r.Msg,
            CheckedAt = DateTime.UtcNow
        }).ToList();

        _db.ValidationLogs.AddRange(logs);
        await _db.SaveChangesAsync();

        return results
            .Select(r => new ValidationResultDto(
                r.Rule,
                r.Passed ? "Pass" : "Fail",
                r.Msg))
            .ToList();
    }
}
