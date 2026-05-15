using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Notification.Commands.CreateNotification;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.Common.Utilities;

namespace SmartJobPortal.Application.Features.Admin.Commands.ApproveRecruiter;

public class ApproveRecruiterCommandHandler : IRequestHandler<ApproveRecruiterCommand, ApiResponse<string>>
{
    private readonly IAdminRepository _adminRepo;
    private readonly ICacheService _cache;
    private readonly IMediator _mediator;

    public ApproveRecruiterCommandHandler(IAdminRepository adminRepo, ICacheService cache, IMediator mediator)
    {
        _adminRepo = adminRepo;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<ApiResponse<string>> Handle(ApproveRecruiterCommand request, CancellationToken cancellationToken)
    {
        try 
        {
            var recruiter = await _adminRepo.GetRecruiterApprovalByUserIdAsync(request.UserId);
            if (recruiter == null)
                return ApiResponse<string>.NotFound("Recruiter not found.");

            if (recruiter.IsApproved)
                return ApiResponse<string>.Fail("Recruiter is already approved.");

            await _adminRepo.ApproveRecruiterAsync(request.UserId);

            // Notify
            var (title, message, type) = NotificationTemplates.RecruiterApproved(recruiter.FullName);
            await _mediator.Send(new CreateNotificationCommand(request.UserId, title, message, type));

            // Bust caches
            await _cache.RemoveAsync($"user:{request.UserId}");
            await _cache.RemoveAsync($"user:email:{recruiter.Email}");
            await _cache.RemoveAsync($"recruiter:profile:{request.UserId}");
            await _cache.RemoveAsync("admin:dashboard");

            return ApiResponse<string>.Ok($"Recruiter '{recruiter.FullName}' approved.");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Approval error: {ex.Message}");
        }
    }
}
