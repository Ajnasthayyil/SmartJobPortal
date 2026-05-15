using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserListResponse>>
{
    private readonly IAdminRepository _adminRepo;

    public GetUserByIdQueryHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<UserListResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _adminRepo.GetUserByIdAsync(request.UserId);
        if (user == null)
            return ApiResponse<UserListResponse>.NotFound("User not found.");

        return ApiResponse<UserListResponse>.Ok(user);
    }
}
