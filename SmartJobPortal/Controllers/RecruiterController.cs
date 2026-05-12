using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Features.Recruiter.Commands.PostJob;
using SmartJobPortal.Application.Features.Recruiter.Commands.UpdateApplicationStatus;
using SmartJobPortal.Application.Features.Recruiter.Commands.UpdateProfile;
using SmartJobPortal.Application.Features.Recruiter.Queries.GetProfile;
using SmartJobPortal.Application.Features.Resume.Queries.GetResumeFile;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Recruiter")]
public class RecruiterController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRecruiterService _recruiterService; // Keeping for incremental migration

    public RecruiterController(IMediator mediator, IRecruiterService recruiterService)
    {
        _mediator = mediator;
        _recruiterService = recruiterService;
    }

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token."));

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetRecruiterProfileQuery(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile([FromBody] RecruiterProfileRequest request)
    {
        var result = await _mediator.Send(new UpdateRecruiterProfileCommand(UserId, request));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("jobs")]
    public async Task<IActionResult> PostJob([FromBody] PostJobRequest request)
    {
        var result = await _mediator.Send(new PostJobCommand(UserId, request));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetMyJobs()
    {
        var result = await _recruiterService.GetMyJobsAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{jobId:int}")]
    public async Task<IActionResult> GetJobDetail(int jobId)
    {
        var result = await _recruiterService.GetJobDetailAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("jobs/{jobId:int}")]
    public async Task<IActionResult> UpdateJob(int jobId, [FromBody] UpdateJobRequest request)
    {
        var result = await _recruiterService.UpdateJobAsync(UserId, jobId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("jobs/{jobId:int}")]
    public async Task<IActionResult> DeleteJob(int jobId)
    {
        var result = await _recruiterService.DeleteJobAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("jobs/{jobId:int}/toggle-status")]
    public async Task<IActionResult> ToggleJobStatus(int jobId)
    {
        var result = await _recruiterService.ToggleJobStatusAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{jobId:int}/applicants")]
    public async Task<IActionResult> GetApplicants(int jobId)
    {
        var result = await _recruiterService.GetApplicantsAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs/{jobId:int}/ranking")]
    public async Task<IActionResult> GetRankedApplicants(int jobId)
    {
        var result = await _recruiterService.GetRankedApplicantsAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("applications/{applicationId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int applicationId, [FromBody] UpdateStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateApplicationStatusCommand(UserId, applicationId, request));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("candidates/{candidateUserId:int}/resume")]
    public async Task<IActionResult> DownloadResume(int candidateUserId)
    {
        var file = await _mediator.Send(new GetResumeFileQuery(candidateUserId));
        if (file == null)
            return NotFound(new { message = "Resume not found" });

        return File(file.Value.bytes, file.Value.contentType, file.Value.fileName);
    }

    [HttpGet("candidates/{candidateUserId:int}/profile")]
    public async Task<IActionResult> GetCandidateProfile(int candidateUserId)
    {
        var result = await _recruiterService.GetCandidateProfileAsync(candidateUserId);
        return StatusCode(result.StatusCode, result);
    }
}