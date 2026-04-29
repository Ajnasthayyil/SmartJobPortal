using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Recruiter")]
public class RecruiterController : ControllerBase
{
    private readonly IRecruiterService _recruiterService;
    private readonly IResumeService _resumeService;

    public RecruiterController(IRecruiterService recruiterService, IResumeService resumeService)
    {
        _recruiterService = recruiterService;
        _resumeService = resumeService;
    }

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token."));

    //  Profile 

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _recruiterService.GetProfileAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile(
        [FromBody] RecruiterProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _recruiterService.UpsertProfileAsync(UserId, request);
        return StatusCode(result.StatusCode, result);
    }

    //  Jobs 

    [HttpPost("jobs")]
    public async Task<IActionResult> PostJob([FromBody] PostJobRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _recruiterService.PostJobAsync(UserId, request);
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
    public async Task<IActionResult> UpdateJob(
        int jobId, [FromBody] UpdateJobRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _recruiterService.UpdateJobAsync(UserId, jobId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("jobs/{jobId:int}")]
    public async Task<IActionResult> DeleteJob(int jobId)
    {
        var result = await _recruiterService.DeleteJobAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    //  Applicants 

    /// View all applicants for a job with match scores
    [HttpGet("jobs/{jobId:int}/applicants")]
    public async Task<IActionResult> GetApplicants(int jobId)
    {
        var result = await _recruiterService.GetApplicantsAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    /// View applicants ranked by AI match score (highest first)
    [HttpGet("jobs/{jobId:int}/ranking")]
    public async Task<IActionResult> GetRankedApplicants(int jobId)
    {
        var result = await _recruiterService.GetRankedApplicantsAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    //  Application Status 

    [HttpPut("applications/{applicationId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int applicationId, [FromBody] UpdateStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _recruiterService
            .UpdateApplicationStatusAsync(UserId, applicationId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("candidates/{candidateUserId:int}/resume")]
    public async Task<IActionResult> DownloadResume(int candidateUserId)
    {
        var file = await _resumeService.GetResumeFileAsync(candidateUserId);
        if (file == null)
            return NotFound(new { message = "Resume not found" });

        return File(file.Value.bytes, file.Value.contentType, file.Value.fileName);
    }
}