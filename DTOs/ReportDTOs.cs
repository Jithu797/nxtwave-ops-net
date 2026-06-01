namespace LMSDashboard.DTOs;

public record MonthlyReportDto(
    int TotalItemsThisMonth,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByTrack,
    Dictionary<string, int> ByType,
    PeakDayDto? PeakDay,
    double ErrorRatePercent,
    List<DailyCountDto> DailyCounts
);

public record PeakDayDto(DateTime Date, int Count);
public record DailyCountDto(DateTime Date, int Count);
