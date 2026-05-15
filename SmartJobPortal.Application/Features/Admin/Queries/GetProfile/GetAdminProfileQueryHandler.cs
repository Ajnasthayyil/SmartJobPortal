using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetProfile;

public class GetAdminProfileQueryHandler : IRequestHandler<GetAdminProfileQuery, ApiResponse<AdminProfileResponse>>
{
    private readonly IUserRepository _userRepo;

    public GetAdminProfileQueryHandler(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ApiResponse<AdminProfileResponse>> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId);
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
}
