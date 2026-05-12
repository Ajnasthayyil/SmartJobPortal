using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Recruiter.Queries.GetProfile;

public class GetRecruiterProfileQueryHandler : IRequestHandler<GetRecruiterProfileQuery, ApiResponse<RecruiterProfileResponse>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;

    public GetRecruiterProfileQueryHandler(
        IRecruiterRepository recruiterRepo,
        IUserRepository userRepo,
        ICacheService cache)
    {
        _recruiterRepo = recruiterRepo;
        _userRepo = userRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<RecruiterProfileResponse>> Handle(GetRecruiterProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var cacheKey = $"recruiter:profile:{userId}";
        var cached = await _cache.GetAsync<RecruiterProfileResponse>(cacheKey);
        if (cached != null) return ApiResponse<RecruiterProfileResponse>.Ok(cached);

        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return ApiResponse<RecruiterProfileResponse>.NotFound("User not found.");

        var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
        if (recruiter == null)
        {
            return ApiResponse<RecruiterProfileResponse>.Ok(new RecruiterProfileResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email
            });
        }

        var totalJobs = await _recruiterRepo.GetTotalJobsPostedAsync(recruiter.RecruiterId);
        var response = BuildProfileResponse(recruiter, user, totalJobs);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
        return ApiResponse<RecruiterProfileResponse>.Ok(response);
    }

    private RecruiterProfileResponse BuildProfileResponse(SmartJobPortal.Domain.Entities.Recruiter r, SmartJobPortal.Domain.Entities.User u, int totalJobs) =>
        new()
        {
            RecruiterId = r.RecruiterId,
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            CompanyName = r.CompanyName,
            Website = r.Website,
            Industry = r.Industry,
            Description = r.Description,
            Location = r.Location,
            CreatedAt = r.CreatedAt,
            TotalJobsPosted = totalJobs
        };
}
