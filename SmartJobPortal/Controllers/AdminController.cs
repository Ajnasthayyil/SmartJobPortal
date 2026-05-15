using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Features.Admin.Commands.ApproveRecruiter;
using SmartJobPortal.Application.Features.Admin.Commands.BlockUser;
using SmartJobPortal.Application.Features.Admin.Commands.RejectRecruiter;
using SmartJobPortal.Application.Features.Admin.Commands.ToggleJobStatus;
using SmartJobPortal.Application.Features.Admin.Commands.UnblockUser;
using SmartJobPortal.Application.Features.Admin.Commands.UpdateProfile;
using SmartJobPortal.Application.Features.Admin.Queries.GetAllJobs;
using SmartJobPortal.Application.Features.Admin.Queries.GetAllUsers;
using SmartJobPortal.Application.Features.Admin.Queries.GetDashboard;
using SmartJobPortal.Application.Features.Admin.Queries.GetPendingRecruiters;
using SmartJobPortal.Application.Features.Admin.Queries.GetProfile;
using SmartJobPortal.Application.Features.Admin.Queries.GetUserById;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
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
        var result = await _mediator.Send(new GetAllUsersQuery(role, isActive));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("users/{userId:int}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("users/{userId:int}/block")]
    public async Task<IActionResult> BlockUser(int userId)
    {
        var result = await _mediator.Send(new BlockUserCommand(userId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("users/{userId:int}/unblock")]
    public async Task<IActionResult> UnblockUser(int userId)
    {
        var result = await _mediator.Send(new UnblockUserCommand(userId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("recruiters/pending")]
    public async Task<IActionResult> GetPendingRecruiters()
    {
        var result = await _mediator.Send(new GetPendingRecruitersQuery());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("recruiters")]
    public async Task<IActionResult> GetAllRecruiters()
    {
        var result = await _mediator.Send(new GetAllUsersQuery("Recruiter", null));
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
        var result = await _mediator.Send(new RejectRecruiterCommand(userId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs()
    {
        var result = await _mediator.Send(new GetAllJobsQuery());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("jobs/{jobId:int}/toggle-status")]
    public async Task<IActionResult> ToggleJobStatus(int jobId)
    {
        var result = await _mediator.Send(new ToggleAdminJobStatusCommand(jobId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetAdminProfileQuery(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileRequest request)
    {
        var result = await _mediator.Send(new UpdateAdminProfileCommand(UserId, request));
        return StatusCode(result.StatusCode, result);
    }
}