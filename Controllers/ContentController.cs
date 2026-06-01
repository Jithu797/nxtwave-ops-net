using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/content")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IContentService _content;
    private readonly IValidationService _validation;

    public ContentController(IContentService content, IValidationService validation)
    {
        _content = content;
        _validation = validation;
    }

    /// <summary>Upload an Excel (.xlsx) file to bulk-create content items.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadSummaryDto>), 200)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<UploadSummaryDto>.Fail("No file uploaded."));

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<UploadSummaryDto>.Fail("Only .xlsx files are accepted."));

        var uploader = User.Identity?.Name ?? "unknown";
        using var stream = file.OpenReadStream();
        var summary = await _content.UploadFromExcelAsync(stream, uploader);

        return Ok(ApiResponse<UploadSummaryDto>.Ok(summary,
            $"Upload complete: {summary.Created} created, {summary.Failed} failed."));
    }

    /// <summary>Get paginated, filtered list of content items.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ContentItemDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ContentFilterParams filters)
    {
        var result = await _content.GetPaginatedAsync(filters);
        return Ok(ApiResponse<PaginatedResult<ContentItemDto>>.Ok(result));
    }

    /// <summary>Get a single content item with its validation logs.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContentItemDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _content.GetByIdAsync(id);
        if (item == null)
            return NotFound(ApiResponse<ContentItemDto>.Fail("Content item not found."));
        return Ok(ApiResponse<ContentItemDto>.Ok(item));
    }

    /// <summary>Update the status of a content item.</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ContentItemDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var item = await _content.UpdateStatusAsync(id, request.Status);
        if (item == null)
            return NotFound(ApiResponse<ContentItemDto>.Fail("Content item not found or invalid status."));
        return Ok(ApiResponse<ContentItemDto>.Ok(item, "Status updated."));
    }

    /// <summary>Run validation rules on a content item.</summary>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(ApiResponse<List<ValidationResultDto>>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Validate(Guid id)
    {
        var results = await _validation.ValidateAsync(id);
        if (!results.Any())
            return NotFound(ApiResponse<List<ValidationResultDto>>.Fail("Content item not found."));

        var allPassed = results.All(r => r.Result == "Pass");
        return Ok(ApiResponse<List<ValidationResultDto>>.Ok(results,
            allPassed ? "All validation rules passed." : "Some validation rules failed."));
    }

    /// <summary>Soft-delete a content item.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _content.SoftDeleteAsync(id);
        if (!deleted)
            return NotFound(ApiResponse<bool>.Fail("Content item not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Item deleted."));
    }
}
