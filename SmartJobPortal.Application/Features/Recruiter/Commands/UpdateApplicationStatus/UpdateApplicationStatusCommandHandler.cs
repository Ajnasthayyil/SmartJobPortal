using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Recruiter;
using SmartJobPortal.Application.Features.Notification.Commands.CreateNotification;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.Services;

namespace SmartJobPortal.Application.Features.Recruiter.Commands.UpdateApplicationStatus;

public class UpdateApplicationStatusCommandHandler : IRequestHandler<UpdateApplicationStatusCommand, ApiResponse<string>>
{
    private readonly IRecruiterRepository _recruiterRepo;
    private readonly IRecruiterJobRepository _jobRepo;
    private readonly IMediator _mediator;

    private static readonly Dictionary<string, List<string>> AllowedTransitions = new()
    {
        ["Applied"] = new() { "UnderReview", "Rejected" },
        ["UnderReview"] = new() { "Shortlisted", "Rejected" },
        ["Shortlisted"] = new() { "Interview", "Rejected" },
        ["Interview"] = new() { "Offered", "Rejected" },
        ["Offered"] = new() { "Rejected" },
        ["Rejected"] = new()
    };

    public UpdateApplicationStatusCommandHandler(
        IRecruiterRepository recruiterRepo,
        IRecruiterJobRepository jobRepo,
        IMediator mediator)
    {
        _recruiterRepo = recruiterRepo;
        _jobRepo = jobRepo;
        _mediator = mediator;
    }

    public async Task<ApiResponse<string>> Handle(UpdateApplicationStatusCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var recruiter = await _recruiterRepo.GetByUserIdAsync(command.UserId);
        if (recruiter == null) return ApiResponse<string>.Fail("Access denied.", 403);

        var current = await _jobRepo.GetApplicationStatusAsync(command.ApplicationId);
        if (string.IsNullOrEmpty(current)) return ApiResponse<string>.NotFound("Application not found.");

        if (!AllowedTransitions.ContainsKey(current) || !AllowedTransitions[current].Contains(request.Status))
            return ApiResponse<string>.Fail($"Invalid transition from {current} to {request.Status}.", 400);

        var updated = await _jobRepo.UpdateApplicationStatusAsync(command.ApplicationId, recruiter.RecruiterId, request.Status);
        if (!updated) return ApiResponse<string>.Fail("Failed to update status.", 500);

        // Notify candidate
        var details = await _jobRepo.GetApplicationDetailsAsync(command.ApplicationId);
        if (details.HasValue)
        {
            var (title, message, type) = NotificationTemplates.ApplicationStatusChanged(
                request.Status, details.Value.JobTitle, details.Value.CompanyName);
            
            await _mediator.Send(new CreateNotificationCommand(
                details.Value.CandidateUserId, 
                title, 
                message, 
                type, 
                details.Value.JobTitle, 
                details.Value.CompanyName));
        }

        return ApiResponse<string>.Ok($"Status updated to {request.Status}.");
    }
}
