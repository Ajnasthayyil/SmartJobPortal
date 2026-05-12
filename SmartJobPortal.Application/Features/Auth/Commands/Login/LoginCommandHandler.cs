using MediatR;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IUserRepository repo, IConfiguration config, IJwtService jwtService)
    {
        _repo = repo;
        _config = config;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        try
        {
            var user = await _repo.GetByEmailAsync(request.Email!);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponse>.FailureResponse(
                    new List<string> { "Invalid email or password" },
                    "Authentication Failed"
                );
            }

            if (user.RoleName == "Recruiter" && !user.IsApproved)
            {
                return ApiResponse<AuthResponse>.FailureResponse(
                    new List<string> { "Your application is under process. The response will inform you soon." },
                    "Account Pending Approval",
                    403
                );
            }

            var accessToken = _jwtService.GenerateJwtToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _repo.SaveRefreshToken(
                user.UserId,
                refreshToken,
                DateTime.UtcNow.AddDays(
                    Convert.ToDouble(_config["Jwt:RefreshTokenExpiryDays"])
                )
            );

            return ApiResponse<AuthResponse>.SuccessResponse(
                new AuthResponse
                {
                    Token = accessToken,
                    RefreshToken = refreshToken
                },
                "Login successful"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { $"An internal error occurred: {ex.Message}" },
                "Server Error",
                500
            );
        }
    }
}
