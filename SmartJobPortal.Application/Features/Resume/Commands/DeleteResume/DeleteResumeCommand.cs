using MediatR;
using SmartJobPortal.Application.Common;

namespace SmartJobPortal.Application.Features.Resume.Commands.DeleteResume;

public record DeleteResumeCommand(int UserId) : IRequest<ApiResponse<bool>>;
