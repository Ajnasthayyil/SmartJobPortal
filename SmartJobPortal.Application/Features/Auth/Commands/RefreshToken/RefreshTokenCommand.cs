using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;

namespace SmartJobPortal.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ApiResponse<AuthResponse>>;
