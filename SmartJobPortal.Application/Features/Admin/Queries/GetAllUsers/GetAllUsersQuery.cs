using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetAllUsers;

public record GetAllUsersQuery(string? RoleFilter, bool? IsActive) : IRequest<ApiResponse<List<UserListResponse>>>;
