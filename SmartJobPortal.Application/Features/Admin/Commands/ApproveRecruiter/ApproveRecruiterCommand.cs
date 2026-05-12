using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Admin.Commands.ApproveRecruiter;

public record ApproveRecruiterCommand(int UserId) : IRequest<ApiResponse<string>>;
