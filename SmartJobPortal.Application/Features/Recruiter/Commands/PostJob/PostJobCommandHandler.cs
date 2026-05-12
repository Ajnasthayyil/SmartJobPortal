using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.PostJob;

public class PostJobCommandHandler : IRequestHandler<PostJobCommand, ApiResponse<JobResponse>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly ICandidateRepository _candidateRepo;

    public PostJobCommandHandler(
        IRecruiterRepository recruiterRepo,
        IRecruiterJobRepository jobRepo,
        ICandidateRepository candidateRepo)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _candidateRepo = candidateRepo;
    }

    public async Task<ApiResponse<JobResponse>> Handle(PostJobCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var userId = command.UserId;

        try
        {
            var recruiter = await _recruiterRepo.GetByUserIdAsync(userId);
            if (recruiter == null || !recruiter.IsApproved)
                return ApiResponse<JobResponse>.Fail("Only approved recruiters can post jobs.", 403);

            var job = new SmartJobPortal.Domain.Entities.Job
            {
                RecruiterId = recruiter.RecruiterId,
                Title = request.Title,
                Description = request.Description,
                Location = request.Location,
                JobType = request.JobType,
                MinSalary = request.MinSalary,
                MaxSalary = request.MaxSalary,
                MinExperienceYears = request.MinExperienceYears,
                IsActive = true,
                PostedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(30),
                UpdatedAt = DateTime.Now
            };

            var jobId = await _jobRepo.CreateJobAsync(job);
            job.JobId = jobId;

            var skillIds = new List<int>();
            if (request.RequiredSkills != null && request.RequiredSkills.Any())
            {
                foreach (var s in request.RequiredSkills)
                {
                    var name = s.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var skillId = await _candidateRepo.GetSkillIdByNameAsync(name)
                                  ?? await _candidateRepo.CreateSkillAsync(name);
                    skillIds.Add(skillId);
                }

                if (skillIds.Any())
                {
                    await _jobRepo.ReplaceJobSkillsAsync(jobId, skillIds);
                }
            }

            var finalSkills = await _jobRepo.GetJobSkillsAsync(jobId);
            return ApiResponse<JobResponse>.Ok(BuildJobResponse(job, finalSkills, 0), "Job posted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<JobResponse>.Fail($"Failed to post job: {ex.Message}", 500);
        }
    }

    private JobResponse BuildJobResponse(SmartJobPortal.Domain.Entities.Job job, List<string> skills, int totalApps) =>
        new()
        {
            JobId = job.JobId,
            RecruiterId = job.RecruiterId,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            JobType = job.JobType,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            MinExperienceYears = job.MinExperienceYears,
            IsActive = job.IsActive,
            PostedAt = job.PostedAt,
            ExpiresAt = job.ExpiresAt,
            RequiredSkills = skills,
            TotalApplicants = totalApps
        };
}
