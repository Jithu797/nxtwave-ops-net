using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Pages;

public class ReportsModel : PageModel
{
    private readonly IReportService _reports;

    public ReportsModel(IReportService reports) => _reports = reports;

    public MonthlyReportDto? Report { get; set; }

    public async Task OnGetAsync()
    {
        Report = await _reports.GetMonthlyReportAsync();
    }
}
