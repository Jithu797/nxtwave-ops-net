using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiStructureService _ai;

    public AiController(IAiStructureService ai) => _ai = ai;

    /// <summary>Send raw content rows to the AI microservice for structuring.</summary>
    [HttpPost("structure")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> Structure([FromBody] AiStructureRequest request)
    {
        try
        {
            var result = await _ai.StructureAsync(request.Rows);
            return Ok(ApiResponse<object>.Ok(result));
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, ApiResponse<object>.Fail($"AI service unavailable: {ex.Message}"));
        }
    }
}
