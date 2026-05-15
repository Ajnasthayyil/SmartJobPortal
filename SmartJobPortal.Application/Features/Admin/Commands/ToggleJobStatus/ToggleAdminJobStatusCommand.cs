using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Admin.Commands.ToggleJobStatus;

public record ToggleAdminJobStatusCommand(int JobId) : IRequest<ApiResponse<string>>;
