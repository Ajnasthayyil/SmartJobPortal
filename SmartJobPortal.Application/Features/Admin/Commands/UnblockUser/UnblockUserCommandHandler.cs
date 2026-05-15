using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Commands.UnblockUser;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, ApiResponse<string>>
{
    private readonly IAdminRepository _adminRepo;

    public UnblockUserCommandHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<string>> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminRepo.UnblockUserAsync(request.UserId);
        if (!success)
            return ApiResponse<string>.NotFound("User not found.");

        return ApiResponse<string>.Ok("User unblocked successfully.");
    }
}
