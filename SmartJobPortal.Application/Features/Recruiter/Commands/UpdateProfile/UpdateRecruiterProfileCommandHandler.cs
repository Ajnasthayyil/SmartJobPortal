using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateProfile;

public class UpdateRecruiterProfileCommandHandler : IRequestHandler<UpdateRecruiterProfileCommand, ApiResponse<RecruiterProfileResponse>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;

    public UpdateRecruiterProfileCommandHandler(
        IRecruiterRepository recruiterRepo,
        IUserRepository userRepo,
        ICacheService cache)
    {
        _recruiterRepo = recruiterRepo;
        _userRepo = userRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<RecruiterProfileResponse>> Handle(UpdateRecruiterProfileCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var userId = command.UserId;

        try
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return ApiResponse<RecruiterProfileResponse>.NotFound("User not found.");

            var existing = await _recruiterRepo.GetByUserIdAsync(userId);
            var recruiter = existing ?? new SmartJobPortal.Domain.Entities.Recruiter { UserId = userId };

            recruiter.CompanyName = request.CompanyName;
            recruiter.Website = request.Website;
            recruiter.Industry = request.Industry;
            recruiter.Description = request.Description;
            recruiter.Location = request.Location;
            recruiter.UpdatedAt = DateTime.Now;

            var recruiterId = await _recruiterRepo.UpsertAsync(recruiter);
            recruiter.RecruiterId = recruiterId;

            await _cache.RemoveAsync($"recruiter:profile:{userId}");
            var totalJobs = await _recruiterRepo.GetTotalJobsPostedAsync(recruiterId);
            return ApiResponse<RecruiterProfileResponse>.Ok(BuildProfileResponse(recruiter, user, totalJobs));
        }
        catch (Exception ex)
        {
            return ApiResponse<RecruiterProfileResponse>.Fail($"Database Error: {ex.Message}", 500);
        }
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
