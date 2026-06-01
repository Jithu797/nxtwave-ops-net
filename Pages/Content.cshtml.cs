using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Pages;

public class ContentPageModel : PageModel
{
    private readonly IContentService _content;
    private readonly IValidationService _validation;

    public ContentPageModel(IContentService content, IValidationService validation)
    {
        _content = content;
        _validation = validation;
    }

    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? TrackFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? TypeFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public List<ContentItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        var result = await _content.GetPaginatedAsync(new ContentFilterParams
        {
            Status = StatusFilter,
            Track = TrackFilter,
            Type = TypeFilter,
            Page = CurrentPage,
            PageSize = 20
        });

        Items = result.Items;
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    public async Task<IActionResult> OnPostValidateAsync(Guid id)
    {
        await _validation.ValidateAsync(id);
        Message = "Validation complete.";
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid id, string newStatus)
    {
        await _content.UpdateStatusAsync(id, newStatus);
        return RedirectToPage(new { status = StatusFilter, track = TrackFilter, type = TypeFilter, page = CurrentPage });
    }
}
