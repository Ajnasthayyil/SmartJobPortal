using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Admin.Commands.BlockUser;

public record BlockUserCommand(int UserId) : IRequest<ApiResponse<string>>;
