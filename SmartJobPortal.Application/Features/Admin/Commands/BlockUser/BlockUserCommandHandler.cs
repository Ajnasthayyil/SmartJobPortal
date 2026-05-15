using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Commands.BlockUser;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, ApiResponse<string>>
{
    private readonly IAdminRepository _adminRepo;

    public BlockUserCommandHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<string>> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminRepo.BlockUserAsync(request.UserId);
        if (!success)
            return ApiResponse<string>.NotFound("User not found.");

        return ApiResponse<string>.Ok("User blocked successfully.");
    }
}
