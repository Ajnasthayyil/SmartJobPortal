using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Features.Admin.Commands.ApproveRecruiter;
using SmartJobPortal.Application.Features.Admin.Queries.GetDashboard;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAdminService _adminService; // Keeping for incremental migration

    public AdminController(IMediator mediator, IAdminService adminService)
    {
        _mediator = mediator;
        _adminService = adminService;
    }

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? role, [FromQuery] bool? isActive)
    {
        var result = await _adminService.GetAllUsersAsync(role, isActive);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("users/{userId:int}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var result = await _adminService.GetUserByIdAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("users/{userId:int}/block")]
    public async Task<IActionResult> BlockUser(int userId)
    {
        var result = await _adminService.BlockUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("users/{userId:int}/unblock")]
    public async Task<IActionResult> UnblockUser(int userId)
    {
        var result = await _adminService.UnblockUserAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("recruiters/pending")]
    public async Task<IActionResult> GetPendingRecruiters()
    {
        var result = await _adminService.GetPendingRecruitersAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("recruiters")]
    public async Task<IActionResult> GetAllRecruiters()
    {
        var result = await _adminService.GetAllRecruitersAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("recruiters/{userId:int}/approve")]
    public async Task<IActionResult> ApproveRecruiter(int userId)
    {
        var result = await _mediator.Send(new ApproveRecruiterCommand(userId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("recruiters/{userId:int}/reject")]
    public async Task<IActionResult> RejectRecruiter(int userId)
    {
        var result = await _adminService.RejectRecruiterAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs()
    {
        var result = await _adminService.GetAllJobsAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("jobs/{jobId:int}/toggle-status")]
    public async Task<IActionResult> ToggleJobStatus(int jobId)
    {
        var result = await _adminService.ToggleJobStatusAsync(jobId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _adminService.GetAdminProfileAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileRequest request)
    {
        var result = await _adminService.UpdateAdminProfileAsync(UserId, request);
        return StatusCode(result.StatusCode, result);
    }
}