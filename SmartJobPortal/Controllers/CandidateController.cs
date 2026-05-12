using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Features.Candidate.Commands.UpdateCandidateProfile;
using SmartJobPortal.Application.Features.Candidate.Queries.GetCandidateProfile;
using SmartJobPortal.Application.Features.Candidate.Queries.GetCompanies;
using SmartJobPortal.Application.Features.Job.Queries.SearchJobs;
using SmartJobPortal.Application.Features.MatchScore.Queries.GetBulkMatchScores;
using SmartJobPortal.Application.Features.MatchScore.Queries.GetMatchScore;
using SmartJobPortal.Application.Features.Resume.Commands.UploadResume;
using SmartJobPortal.Application.Features.Job.Queries.GetJobDetail;
using SmartJobPortal.Application.Features.Job.Commands.ApplyJob;
using SmartJobPortal.Application.Features.Job.Queries.GetMyApplications;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Candidate")]
public class CandidateController : ControllerBase
{
    private readonly IMediator _mediator;

    public CandidateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token."));

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetCandidateProfileQuery(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile([FromBody] CandidateProfileRequest request)
    {
        var result = await _mediator.Send(new UpdateCandidateProfileCommand(UserId, request));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("resume")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file provided." });

        var result = await _mediator.Send(new UploadResumeCommand(UserId, file));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchJobs([FromQuery] JobSearchRequest request)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserId : 0;
        var result = await _mediator.Send(new SearchJobsQuery(userId, request));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{jobId:int}")]
    public async Task<IActionResult> GetJobDetail(int jobId)
    {
        // For now, using direct service for unimplemented handlers or I can quickly implement it
        // Since I haven't implemented GetJobDetailQuery yet, I'll assume it's coming
        // return StatusCode(501, "Not implemented in CQRS yet.");
        // Actually, I'll just use the old service for a moment if needed, but the goal is to replace.
        // Let's implement GetJobDetailQuery quickly.
        var result = await _mediator.Send(new GetJobDetailQuery(UserId, jobId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("jobs/apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyJobRequest request)
    {
        var result = await _mediator.Send(new ApplyJobCommand(UserId, request));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("applications")]
    public async Task<IActionResult> GetMyApplications()
    {
        var result = await _mediator.Send(new GetMyApplicationsQuery(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{jobId:int}/match-score")]
    public async Task<IActionResult> GetMatchScore(int jobId)
    {
        var result = await _mediator.Send(new GetMatchScoreQuery(UserId, jobId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("match-scores/bulk")]
    public async Task<IActionResult> GetBulkMatchScores([FromBody] List<int> jobIds)
    {
        if (jobIds.Count > 50)
            return BadRequest(new { success = false, message = "Max 50 job IDs per request." });

        var result = await _mediator.Send(new GetBulkMatchScoresQuery(UserId, jobIds));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("companies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanies()
    {
        var result = await _mediator.Send(new GetCompaniesQuery());
        return StatusCode(result.StatusCode, result);
    }
}