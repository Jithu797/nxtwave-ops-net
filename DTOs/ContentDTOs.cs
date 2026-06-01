using LMSDashboard.Models;

namespace LMSDashboard.DTOs;

public record ContentItemDto(
    Guid Id,
    string Title,
    string Type,
    string Track,
    string Difficulty,
    string Status,
    DateTime? BetaUploadedAt,
    DateTime? ProdUploadedAt,
    DateTime? ValidatedAt,
    DateTime CreatedAt,
    string CreatedBy,
    string? Notes,
    List<ValidationLogDto>? ValidationLogs = null
);

public record ValidationLogDto(
    Guid Id,
    string RuleName,
    string Result,
    string Message,
    DateTime CheckedAt
);

public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record UpdateStatusRequest(string Status);

public record UploadSummaryDto(
    int Created,
    int Failed,
    List<string> Errors
);

public record ContentFilterParams
{
    public string? Status { get; init; }
    public string? Track { get; init; }
    public string? Type { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record ValidationResultDto(
    string RuleName,
    string Result,
    string Message
);

public record AiStructureRequest(object[] Rows);
