using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSDashboard.DTOs;
using LMSDashboard.Services;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IJobRecordService _jobs;

    public AdminController(IJobRecordService jobs) => _jobs = jobs;

    /// <summary>Get recent background job execution history.</summary>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetJobs([FromQuery] int take = 20)
    {
        var records = await _jobs.GetRecentAsync(take);
        return Ok(ApiResponse<object>.Ok(records));
    }
}
