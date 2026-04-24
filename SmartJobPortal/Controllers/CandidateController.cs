using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Candidate")]
public class CandidateController : ControllerBase
{
    private readonly ICandidateService _candidateService;
    private readonly IResumeService _resumeService;
    private readonly IJobSearchService _jobSearchService;
    private readonly IMatchScoreService _matchScoreService;

    public CandidateController(
        ICandidateService candidateService,
        IResumeService resumeService,
        IJobSearchService jobSearchService,
        IMatchScoreService matchScoreService)
    {
        _candidateService = candidateService;
        _resumeService = resumeService;
        _jobSearchService = jobSearchService;
        _matchScoreService = matchScoreService;
    }

    // Extract UserId from JWT "sub" claim
    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token."));

    //  Profile 

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _candidateService.GetProfileAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile([FromBody] CandidateProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _candidateService.UpsertProfileAsync(UserId, request);
        return StatusCode(result.StatusCode, result);
    }

    //  Resume 

    [HttpPost("resume")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file provided." });

        var result = await _resumeService.UploadAndParseAsync(UserId, file);
        return StatusCode(result.StatusCode, result);
    }

    //  Job Search 

    [HttpGet("jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchJobs([FromQuery] JobSearchRequest request)
    {
        var userId = User.Identity?.IsAuthenticated == true ? UserId : 0;
        var result = await _jobSearchService.SearchAsync(userId, request);
        return StatusCode(result.StatusCode, result);
    }

    ///Get single job detail with match score and skill gap
    [HttpGet("jobs/{jobId:int}")]
    public async Task<IActionResult> GetJobDetail(int jobId)
    {
        var result = await _jobSearchService.GetDetailAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    //  Applications 

    [HttpPost("jobs/apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyJobRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _jobSearchService.ApplyAsync(UserId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("applications")]
    public async Task<IActionResult> GetMyApplications()
    {
        var result = await _jobSearchService.GetMyApplicationsAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    //  Match Score / Skill Gap 

    // Get match score + skill gap for a specific job
    [HttpGet("jobs/{jobId:int}/match-score")]
    public async Task<IActionResult> GetMatchScore(int jobId)
    {
        var result = await _matchScoreService.GetOrCalculateAsync(UserId, jobId);
        return StatusCode(result.StatusCode, result);
    }

    // Bulk match scores for up to 50 jobs
    [HttpPost("match-scores/bulk")]
    public async Task<IActionResult> GetBulkMatchScores([FromBody] List<int> jobIds)
    {
        if (jobIds.Count > 50)
            return BadRequest(new { success = false, message = "Max 50 job IDs per request." });

        var result = await _matchScoreService.GetBulkAsync(UserId, jobIds);
        return StatusCode(result.StatusCode, result);
    }
}