using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Commands.UpdateProfile;

public record UpdateAdminProfileCommand(int UserId, UpdateAdminProfileRequest Request) : IRequest<ApiResponse<AdminProfileResponse>>;
