using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, ApiResponse<AuthResponse>>
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;
    private readonly IJwtService _jwtService;

    public GoogleLoginCommandHandler(IUserRepository repo, IConfiguration config, IJwtService jwtService)
    {
        _repo = repo;
        _config = config;
        _jwtService = jwtService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(command.Request.IdToken);
        }
        catch
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "Invalid Google token" }
            );
        }

        var email = payload.Email;
        var name = payload.Name;

        var user = await _repo.GetByEmailAsync(email);

        if (user == null)
        {
            var roleId = await _repo.GetRoleIdByName("Candidate");

            user = new User
            {
                FullName = name,
                Email = email,
                PasswordHash = "", // not required
                RoleId = roleId
            };

            await _repo.CreateUserAsync(user);
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
            "Google login successful"
        );
    }
}
