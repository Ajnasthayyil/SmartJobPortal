using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;

namespace SmartJobPortal.Application.Features.Auth.Commands.GoogleLogin;

public record GoogleLoginCommand(GoogleLoginRequest Request) : IRequest<ApiResponse<AuthResponse>>;
