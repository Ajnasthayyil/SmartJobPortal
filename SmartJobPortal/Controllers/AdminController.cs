using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
   private readonly IAdminService _adminService;

   public AdminController(IAdminService adminService)
   {
       _adminService = adminService;
   }

   // Dashboard 

   [HttpGet("dashboard")]
   public async Task<IActionResult> GetDashboard()
   {
       var result = await _adminService.GetDashboardAsync();
       return StatusCode(result.StatusCode, result);
   }

   //  User management

   [HttpGet("users")]
   public async Task<IActionResult> GetAllUsers(
       [FromQuery] string? role,
       [FromQuery] bool? isActive)
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

   //  Recruiter approvals 

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
       var result = await _adminService.ApproveRecruiterAsync(userId);
       return StatusCode(result.StatusCode, result);
   }

   [HttpPut("recruiters/{userId:int}/reject")]
   public async Task<IActionResult> RejectRecruiter(int userId)
   {
       var result = await _adminService.RejectRecruiterAsync(userId);
       return StatusCode(result.StatusCode, result);
   }

   //  Job monitoring 

   [HttpGet("jobs")]
   public async Task<IActionResult> GetAllJobs()
   {
       var result = await _adminService.GetAllJobsAsync();
       return StatusCode(result.StatusCode, result);
   }

   [HttpPut("jobs/{jobId:int}/deactivate")]
   public async Task<IActionResult> DeactivateJob(int jobId)
   {
       var result = await _adminService.DeactivateJobAsync(jobId);
       return StatusCode(result.StatusCode, result);
   }
}