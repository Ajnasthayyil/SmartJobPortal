using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepo;
    private readonly ICacheService _cache;

    public AdminService(IAdminRepository adminRepo, ICacheService cache)
    {
        _adminRepo = adminRepo;
        _cache = cache;
    }

    //  Dashboard 

    public async Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync()
    {
        const string cacheKey = "admin:dashboard";
        var cached = await _cache.GetAsync<AdminDashboardResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<AdminDashboardResponse>.Ok(cached);

        var stats = await _adminRepo.GetDashboardStatsAsync();

        // Cache for 5 minutes — dashboard data changes frequently
        await _cache.SetAsync(cacheKey, stats, TimeSpan.FromMinutes(5));

        return ApiResponse<AdminDashboardResponse>.Ok(stats);
    }

    //  User management 

    public async Task<ApiResponse<List<UserListResponse>>> GetAllUsersAsync(
        string? roleFilter, bool? isActive)
    {
        var users = await _adminRepo.GetAllUsersAsync(roleFilter, isActive);
        return ApiResponse<List<UserListResponse>>.Ok(users);
    }

    public async Task<ApiResponse<UserListResponse>> GetUserByIdAsync(int userId)
    {
        var user = await _adminRepo.GetUserByIdAsync(userId);
        if (user == null)
            return ApiResponse<UserListResponse>.NotFound("User not found.");

        return ApiResponse<UserListResponse>.Ok(user);
    }

    public async Task<ApiResponse<string>> BlockUserAsync(int userId)
    {
        var user = await _adminRepo.GetUserByIdAsync(userId);
        if (user == null)
            return ApiResponse<string>.NotFound("User not found.");

        if (!user.IsActive)
            return ApiResponse<string>.Fail("User is already blocked.");

        // Prevent blocking an admin account
        if (user.RoleName == "Admin")
            return ApiResponse<string>.Fail("Admin accounts cannot be blocked.", 403);

        await _adminRepo.BlockUserAsync(userId);

        // Bust user cache
        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{user.Email}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok(
            $"User '{user.FullName}' has been blocked successfully.");
    }

    public async Task<ApiResponse<string>> UnblockUserAsync(int userId)
    {
        var user = await _adminRepo.GetUserByIdAsync(userId);
        if (user == null)
            return ApiResponse<string>.NotFound("User not found.");

        if (user.IsActive)
            return ApiResponse<string>.Fail("User is already active.");

        await _adminRepo.UnblockUserAsync(userId);

        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{user.Email}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok(
            $"User '{user.FullName}' has been unblocked successfully.");
    }

    //  Recruiter approvals 

    public async Task<ApiResponse<List<RecruiterApprovalResponse>>>
        GetPendingRecruitersAsync()
    {
        var recruiters = await _adminRepo.GetPendingRecruitersAsync();
        return ApiResponse<List<RecruiterApprovalResponse>>.Ok(recruiters);
    }

    public async Task<ApiResponse<List<RecruiterApprovalResponse>>>
        GetAllRecruitersAsync()
    {
        var recruiters = await _adminRepo.GetAllRecruitersAsync();
        return ApiResponse<List<RecruiterApprovalResponse>>.Ok(recruiters);
    }

    public async Task<ApiResponse<string>> ApproveRecruiterAsync(int userId)
    {
        var recruiter = await _adminRepo.GetRecruiterApprovalByUserIdAsync(userId);

        if (recruiter == null)
            return ApiResponse<string>.NotFound("Recruiter not found.");

        if (recruiter.IsApproved)
            return ApiResponse<string>.Fail("Recruiter is already approved.");

        if (!recruiter.IsActive)
            return ApiResponse<string>.Fail(
                "Cannot approve a blocked account. Unblock the user first.");

        await _adminRepo.ApproveRecruiterAsync(userId);

        // Bust caches
        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{recruiter.Email}");
        await _cache.RemoveAsync($"recruiter:profile:{userId}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok(
            $"Recruiter '{recruiter.FullName}' from '{recruiter.CompanyName}'" +
            $" has been approved. They can now login and post jobs.");
    }

    public async Task<ApiResponse<string>> RejectRecruiterAsync(int userId)
    {
        var recruiter = await _adminRepo.GetRecruiterApprovalByUserIdAsync(userId);

        if (recruiter == null)
            return ApiResponse<string>.NotFound("Recruiter not found.");

        if (!recruiter.IsActive)
            return ApiResponse<string>.Fail("User account is already blocked.");

        await _adminRepo.RejectRecruiterAsync(userId);

        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{recruiter.Email}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok(
            $"Recruiter '{recruiter.FullName}' has been rejected and blocked.");
    }

    //  Job monitoring 

    public async Task<ApiResponse<List<RecentJobActivity>>> GetAllJobsAsync()
    {
        var jobs = await _adminRepo.GetAllJobsAsync();
        return ApiResponse<List<RecentJobActivity>>.Ok(jobs);
    }

    public async Task<ApiResponse<string>> DeactivateJobAsync(int jobId)
    {
        var deactivated = await _adminRepo.DeactivateJobAsync(jobId);
        if (!deactivated)
            return ApiResponse<string>.NotFound("Job not found.");

        await _cache.RemoveAsync($"job:{jobId}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok($"Job #{jobId} has been deactivated.");
    }
}