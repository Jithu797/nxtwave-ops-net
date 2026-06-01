using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    /// <summary>Get monthly stats report.</summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(ApiResponse<MonthlyReportDto>), 200)]
    public async Task<IActionResult> Monthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var report = await _reports.GetMonthlyReportAsync(year, month);
        return Ok(ApiResponse<MonthlyReportDto>.Ok(report));
    }

    /// <summary>Export monthly report as Excel file.</summary>
    [HttpGet("monthly/export")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ExportMonthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var bytes = await _reports.ExportMonthlyReportToExcelAsync(year, month);
        var filename = $"LMS_Report_{year ?? DateTime.UtcNow.Year}_{month ?? DateTime.UtcNow.Month:D2}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            filename);
    }
}
