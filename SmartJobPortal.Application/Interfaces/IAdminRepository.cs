using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Interfaces;

public interface IAdminRepository
{
    // Users
    Task<List<UserListResponse>> GetAllUsersAsync(string? roleFilter, bool? isActive);
    Task<UserListResponse?> GetUserByIdAsync(int userId);
    Task<bool> BlockUserAsync(int userId);
    Task<bool> UnblockUserAsync(int userId);

    // Recruiter approvals
    Task<List<RecruiterApprovalResponse>> GetPendingRecruitersAsync();
    Task<List<RecruiterApprovalResponse>> GetAllRecruitersAsync();
    Task<RecruiterApprovalResponse?> GetRecruiterApprovalByUserIdAsync(int userId);
    Task<bool> ApproveRecruiterAsync(int userId);
    Task<bool> RejectRecruiterAsync(int userId);

    // Jobs monitoring
    Task<List<RecentJobActivity>> GetAllJobsAsync();
    Task<bool> DeactivateJobAsync(int jobId);

    // Dashboard stats
    Task<AdminDashboardResponse> GetDashboardStatsAsync();
}