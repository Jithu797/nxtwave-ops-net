using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Pages;

public class UploadModel : PageModel
{
    private readonly IContentService _content;

    public UploadModel(IContentService content) => _content = content;

    public UploadSummaryDto? Summary { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file.");
            return Page();
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Only .xlsx files are accepted.");
            return Page();
        }

        using var stream = file.OpenReadStream();
        Summary = await _content.UploadFromExcelAsync(stream, User.Identity?.Name ?? "web-user");
        return Page();
    }
}
