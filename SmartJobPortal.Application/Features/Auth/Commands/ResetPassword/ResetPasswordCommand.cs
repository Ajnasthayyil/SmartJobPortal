using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(
    ResetPasswordRequest Request
) : IRequest<ApiResponse<string>>;

public class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
{
    private readonly IUserRepository _userRepo;

    public ResetPasswordCommandHandler(
        IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ApiResponse<string>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Request.NewPassword != request.Request.ConfirmPassword)
        {
            return ApiResponse<string>.Fail("Passwords do not match.");
        }

        var user = await _userRepo.GetByEmailAsync(request.Request.Email);

        if (user == null || user.ResetToken != request.Request.Otp || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            return ApiResponse<string>.Fail(
                "Invalid or expired verification code.");
        }

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(
            request.Request.NewPassword);

        await _userRepo.UpdatePasswordAsync(
            user.UserId,
            newPasswordHash);

        return ApiResponse<string>.Ok(
            null,
            "Password has been reset successfully.");
    }
}