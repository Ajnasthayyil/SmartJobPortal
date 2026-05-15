using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, ApiResponse<List<UserListResponse>>>
{
    private readonly IAdminRepository _adminRepo;

    public GetAllUsersQueryHandler(IAdminRepository adminRepo)
    {
        _adminRepo = adminRepo;
    }

    public async Task<ApiResponse<List<UserListResponse>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _adminRepo.GetAllUsersAsync(request.RoleFilter, request.IsActive);
        return ApiResponse<List<UserListResponse>>.Ok(users);
    }
}
