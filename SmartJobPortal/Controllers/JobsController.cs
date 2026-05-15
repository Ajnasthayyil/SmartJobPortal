using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.Job.Queries.GetJobDetail;
using SmartJobPortal.Application.Features.Job.Queries.SearchJobs;
using System.Security.Claims;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private int? UserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet("{jobId:int}")]
    public async Task<IActionResult> GetJobDetail(int jobId)
    {
        var result = await _mediator.Send(new GetJobDetailQuery(UserId ?? 0, jobId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> SearchJobs([FromQuery] JobSearchRequest request)
    {
        var result = await _mediator.Send(new SearchJobsQuery(UserId ?? 0, request));
        return StatusCode(result.StatusCode, result);
    }
}
