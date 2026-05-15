using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Admin.Commands.RejectRecruiter;

public record RejectRecruiterCommand(int UserId) : IRequest<ApiResponse<string>>;
