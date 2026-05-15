using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Commands.RejectRecruiter;

public class RejectRecruiterCommandHandler : IRequestHandler<RejectRecruiterCommand, ApiResponse<string>>
{
    private readonly IAdminRepository _adminRepo;

    public RejectRecruiterCommandHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<string>> Handle(RejectRecruiterCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminRepo.RejectRecruiterAsync(request.UserId);
        if (!success)
            return ApiResponse<string>.NotFound("Recruiter not found.");

        return ApiResponse<string>.Ok("Recruiter rejected.");
    }
}
