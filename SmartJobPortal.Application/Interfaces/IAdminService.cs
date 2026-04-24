using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Interfaces;

public interface IAdminService
{
    // Dashboard
    Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync();

    // User management
    Task<ApiResponse<List<UserListResponse>>> GetAllUsersAsync(
        string? roleFilter, bool? isActive);
    Task<ApiResponse<UserListResponse>> GetUserByIdAsync(int userId);
    Task<ApiResponse<string>> BlockUserAsync(int userId);
    Task<ApiResponse<string>> UnblockUserAsync(int userId);

    // Recruiter approvals
    Task<ApiResponse<List<RecruiterApprovalResponse>>> GetPendingRecruitersAsync();
    Task<ApiResponse<List<RecruiterApprovalResponse>>> GetAllRecruitersAsync();
    Task<ApiResponse<string>> ApproveRecruiterAsync(int userId);
    Task<ApiResponse<string>> RejectRecruiterAsync(int userId);

    // Job monitoring
    Task<ApiResponse<List<RecentJobActivity>>> GetAllJobsAsync();
    Task<ApiResponse<string>> DeactivateJobAsync(int jobId);
}