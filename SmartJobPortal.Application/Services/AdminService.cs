using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;
    private readonly INotificationService _notificationService;

    public AdminService(
        IAdminRepository adminRepo, 
        IUserRepository userRepo, 
        ICacheService cache,
        INotificationService notificationService)
    {
        _adminRepo = adminRepo;
        _userRepo = userRepo;
        _cache = cache;
        _notificationService = notificationService;
    }

    // ── Dashboard ──────────────────────────────────────────────────

    public async Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync()
    {
        const string cacheKey = "admin:dashboard";
        var cached = await _cache.GetAsync<AdminDashboardResponse>(cacheKey);
        if (cached != null)
            return ApiResponse<AdminDashboardResponse>.Ok(cached);

        var stats = await _adminRepo.GetDashboardStatsAsync();
        await _cache.SetAsync(cacheKey, stats, TimeSpan.FromMinutes(5));

        return ApiResponse<AdminDashboardResponse>.Ok(stats);
    }

    // ── User management ──────────────────────────────────────────

    public async Task<ApiResponse<List<UserListResponse>>> GetAllUsersAsync(string? roleFilter, bool? isActive)
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

        if (user.RoleName == "Admin")
            return ApiResponse<string>.Fail("Admin accounts cannot be blocked.", 403);

        await _adminRepo.BlockUserAsync(userId);

        // ── FIRE NOTIFICATION ──
        var (title, message, type) = NotificationTemplates.AccountBlocked();
        await _notificationService.CreateAsync(userId, title, message, type);

        // Bust cache
        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{user.Email}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok($"User '{user.FullName}' has been blocked successfully.");
    }

    public async Task<ApiResponse<string>> UnblockUserAsync(int userId)
    {
        var user = await _adminRepo.GetUserByIdAsync(userId);
        if (user == null)
            return ApiResponse<string>.NotFound("User not found.");

        if (user.IsActive)
            return ApiResponse<string>.Fail("User is already active.");

        await _adminRepo.UnblockUserAsync(userId);

        // ── FIRE NOTIFICATION ──
        var (title, message, type) = NotificationTemplates.AccountUnblocked();
        await _notificationService.CreateAsync(userId, title, message, type);

        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{user.Email}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok($"User '{user.FullName}' has been unblocked successfully.");
    }

    // ── Recruiter approvals ───────────────────────────────────────

    public async Task<ApiResponse<List<RecruiterApprovalResponse>>> GetPendingRecruitersAsync()
    {
        var recruiters = await _adminRepo.GetPendingRecruitersAsync();
        return ApiResponse<List<RecruiterApprovalResponse>>.Ok(recruiters);
    }

    public async Task<ApiResponse<List<RecruiterApprovalResponse>>> GetAllRecruitersAsync()
    {
        var recruiters = await _adminRepo.GetAllRecruitersAsync();
        return ApiResponse<List<RecruiterApprovalResponse>>.Ok(recruiters);
    }

    public async Task<ApiResponse<string>> ApproveRecruiterAsync(int userId)
    {
        try 
        {
            var recruiter = await _adminRepo.GetRecruiterApprovalByUserIdAsync(userId);
            if (recruiter == null)
                return ApiResponse<string>.NotFound("Recruiter not found.");

            if (recruiter.IsApproved)
                return ApiResponse<string>.Fail("Recruiter is already approved.");

            await _adminRepo.ApproveRecruiterAsync(userId);

            // ── FIRE NOTIFICATION ──
            var (title, message, type) = NotificationTemplates.RecruiterApproved(recruiter.FullName);
            await _notificationService.CreateAsync(userId, title, message, type);

            // Bust caches
            await _cache.RemoveAsync($"user:{userId}");
            await _cache.RemoveAsync($"user:email:{recruiter.Email}");
            await _cache.RemoveAsync($"recruiter:profile:{userId}");
            await _cache.RemoveAsync("admin:dashboard");

            return ApiResponse<string>.Ok($"Recruiter '{recruiter.FullName}' approved.");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Approval error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<string>> RejectRecruiterAsync(int userId)
    {
        var recruiter = await _adminRepo.GetRecruiterApprovalByUserIdAsync(userId);
        if (recruiter == null)
            return ApiResponse<string>.NotFound("Recruiter not found.");

        await _adminRepo.RejectRecruiterAsync(userId);

        // ── FIRE NOTIFICATION ──
        var (title, message, type) = NotificationTemplates.RecruiterRejected();
        await _notificationService.CreateAsync(userId, title, message, type);

        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{recruiter.Email}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok($"Recruiter '{recruiter.FullName}' rejected and blocked.");
    }

    // ── Job monitoring ──────────────────────────────────────────

    public async Task<ApiResponse<List<RecentJobActivity>>> GetAllJobsAsync()
    {
        var jobs = await _adminRepo.GetAllJobsAsync();
        return ApiResponse<List<RecentJobActivity>>.Ok(jobs);
    }

    public async Task<ApiResponse<string>> ToggleJobStatusAsync(int jobId)
    {
        var toggled = await _adminRepo.ToggleJobStatusAsync(jobId);
        if (!toggled)
            return ApiResponse<string>.NotFound("Job not found.");

        await _cache.RemoveAsync($"job:{jobId}");
        await _cache.RemoveAsync("admin:dashboard");

        return ApiResponse<string>.Ok($"Job #{jobId} status toggled.");
    }

    // ── Admin Profile ───────────────────────────────────────────

    public async Task<ApiResponse<AdminProfileResponse>> GetAdminProfileAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<AdminProfileResponse>.NotFound("Admin not found.");

        return ApiResponse<AdminProfileResponse>.Ok(new AdminProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<ApiResponse<AdminProfileResponse>> UpdateAdminProfileAsync(int userId, UpdateAdminProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<AdminProfileResponse>.NotFound("Admin not found.");

        await _userRepo.UpdateProfileAsync(userId, request.FullName, request.PhoneNumber);

        await _cache.RemoveAsync($"user:{userId}");
        await _cache.RemoveAsync($"user:email:{user.Email}");

        var updatedUser = await _userRepo.GetByIdAsync(userId);
        return ApiResponse<AdminProfileResponse>.Ok(new AdminProfileResponse
        {
            UserId = updatedUser.UserId,
            FullName = updatedUser.FullName,
            Email = updatedUser.Email,
            PhoneNumber = updatedUser.PhoneNumber,
            ProfilePictureUrl = updatedUser.ProfilePictureUrl,
            CreatedAt = updatedUser.CreatedAt
        }, "Admin profile updated successfully.");
    }
}