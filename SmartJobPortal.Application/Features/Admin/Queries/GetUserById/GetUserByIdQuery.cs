using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetUserById;

public record GetUserByIdQuery(int UserId) : IRequest<ApiResponse<UserListResponse>>;
