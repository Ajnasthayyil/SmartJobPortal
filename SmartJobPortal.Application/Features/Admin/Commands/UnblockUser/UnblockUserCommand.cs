using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Admin.Commands.UnblockUser;

public record UnblockUserCommand(int UserId) : IRequest<ApiResponse<string>>;
