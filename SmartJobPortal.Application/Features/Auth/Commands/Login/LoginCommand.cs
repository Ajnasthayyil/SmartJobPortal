using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;

namespace SmartJobPortal.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequest Request) : IRequest<ApiResponse<AuthResponse>>;
