using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Pages.Content;

public class ViewModel : PageModel
{
    private readonly IContentService _content;
    private readonly IValidationService _validation;

    public ViewModel(IContentService content, IValidationService validation)
    {
        _content = content;
        _validation = validation;
    }

    public ContentItemDto? Item { get; set; }

    public async Task OnGetAsync(Guid id)
    {
        Item = await _content.GetByIdAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        await _validation.ValidateAsync(id);
        return RedirectToPage(new { id });
    }
}
