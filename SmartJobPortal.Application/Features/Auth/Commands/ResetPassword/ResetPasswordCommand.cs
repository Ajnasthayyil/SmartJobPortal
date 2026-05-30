using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Features.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<ApiResponse<string>>;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
    {
        private readonly IUserRepository _userRepo;

        public ResetPasswordCommandHandler(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<ApiResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (request.Request.NewPassword != request.Request.ConfirmPassword)
            {
                return ApiResponse<string>.Fail("New password and confirmation password do not match.");
            }

            var user = await _userRepo.GetByEmailAsync(request.Request.Email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("No account found with that email address.");
            }

            if (string.IsNullOrEmpty(user.ResetToken) || user.ResetToken != request.Request.Otp)
            {
                return ApiResponse<string>.Fail("Invalid verification code. Please check your email and try again.");
            }

            if (user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                return ApiResponse<string>.Fail("Verification code has expired. Please request a new password reset.");
            }

            // Securely hash the new password using BCrypt
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Request.NewPassword);

            // Update user password and clear token columns in database
            await _userRepo.UpdatePasswordAsync(user.UserId, newPasswordHash);

            return ApiResponse<string>.Ok(null, "Password has been successfully reset. You can now log in with your new password.");
        }
    }
}
