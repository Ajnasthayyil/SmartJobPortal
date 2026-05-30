using MediatR;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Features.Auth.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<ApiResponse<string>>;

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
    {
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public ForgotPasswordCommandHandler(IUserRepository userRepo, IEmailService emailService, IConfiguration config)
        {
            _userRepo = userRepo;
            _emailService = emailService;
            _config = config;
        }

        public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepo.GetByEmailAsync(request.Request.Email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("No account found with that email address.");
            }

            // Generate a secure 6-digit OTP
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // OTP is valid for 15 minutes
            var expiry = DateTime.UtcNow.AddMinutes(15);

            await _userRepo.SetResetTokenAsync(user.Email, otp, expiry);

            // Aesthetically premium HTML email body with smooth gradients and glassmorphism styling
            var emailBody = $@"
                <div style='background-color: #0B0F19; padding: 40px 20px; font-family: ""Outfit"", ""Inter"", system-ui, -apple-system, sans-serif; color: #F3F4F6; text-align: center; border-radius: 16px; max-width: 600px; margin: 0 auto; box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3);'>
                    <div style='display: inline-block; margin-bottom: 24px;'>
                        <div style='background: linear-gradient(135deg, #10B981 0%, #059669 100%); width: 64px; height: 64px; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto;'>
                            <svg xmlns='http://svgshare.com/v.htm' style='width: 32px; height: 32px; fill: white;' viewBox='0 0 24 24'>
                                <path d='M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3.1-9H8.9V6c0-1.71 1.39-3.1 3.1-3.1 1.71 0 3.1 1.39 3.1 3.1v2z'/>
                            </svg>
                        </div>
                    </div>
                    <h2 style='font-size: 28px; font-weight: 700; margin: 0 0 12px 0; color: #FFFFFF; letter-spacing: -0.5px;'>Reset Your Password</h2>
                    <p style='color: #9CA3AF; font-size: 16px; line-height: 1.6; margin: 0 0 32px 0; max-width: 480px; margin-left: auto; margin-right: auto;'>
                        Hi {user.FullName},<br>
                        We received a request to reset your password. Use the verification code below to authorize this change:
                    </p>
                    <div style='background: rgba(255, 255, 255, 0.03); border: 1px solid rgba(255, 255, 255, 0.08); border-radius: 12px; padding: 24px; display: inline-block; min-width: 200px; margin-bottom: 32px;'>
                        <span style='font-size: 36px; font-weight: 800; letter-spacing: 8px; color: #10B981; font-family: monospace;'>{otp}</span>
                    </div>
                    <p style='font-size: 13px; color: #6B7280; margin: 0 0 24px 0;'>
                        This verification code is secure and will expire in <strong style='color: #EF4444;'>15 minutes</strong>.
                    </p>
                    <div style='border-top: 1px solid rgba(255, 255, 255, 0.08); padding-top: 24px; font-size: 12px; color: #4B5563;'>
                        If you did not request this, you can safely ignore this email. Your account security remains intact.<br>
                        &copy; {DateTime.UtcNow.Year} SmartJobPortal Team. All rights reserved.
                    </div>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "Reset Your Password - Verification Code", emailBody);

            return ApiResponse<string>.Ok(null, "Verification code sent successfully to your email.");
        }
    }
}
