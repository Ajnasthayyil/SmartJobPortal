using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.Common;
using System.Security.Claims;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobSearchService _jobSearchService;

    public JobsController(IJobSearchService jobSearchService)
    {
        _jobSearchService = jobSearchService;
    }

    private int? UserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("{jobId:int}")]
    public async Task<IActionResult> GetJobDetail(int jobId)
    {
        var result = await _jobSearchService.GetDetailAsync(UserId ?? 0, jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> SearchJobs([FromQuery] SmartJobPortal.Application.DTOs.Candidate.JobSearchRequest request)
    {
        var result = await _jobSearchService.SearchAsync(UserId ?? 0, request);
        return StatusCode(result.StatusCode, result);
    }
}
