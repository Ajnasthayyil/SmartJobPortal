using MediatR;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IUserRepository repo, IConfiguration config, IJwtService jwtService)
    {
        _repo = repo;
        _config = config;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var storedToken = await _repo.GetRefreshToken(command.RefreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "Session expired. Login required." }
            );
        }

        var user = await _repo.GetByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "User not found. Login required." }
            );
        }

        var newAccessToken = _jwtService.GenerateJwtToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        await _repo.RevokeRefreshToken(command.RefreshToken);

        await _repo.SaveRefreshToken(
            user.UserId,
            newRefreshToken,
            DateTime.UtcNow.AddDays(
                Convert.ToDouble(_config["Jwt:RefreshTokenExpiryDays"])
            )
        );

        return ApiResponse<AuthResponse>.SuccessResponse(
            new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            },
            "Token refreshed"
        );
    }
}
