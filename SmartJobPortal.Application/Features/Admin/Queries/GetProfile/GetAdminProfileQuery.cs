using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetProfile;

public record GetAdminProfileQuery(int UserId) : IRequest<ApiResponse<AdminProfileResponse>>;
