using MediatR;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Features.Auth.Commands.ForgotPassword;

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

        var emailBody = $@"
            <h3>Password Reset Request</h3>
            <p>Hi {user.FullName},</p>
            <p>You recently requested to reset your password for your SmartJobPortal account. Please use the following 6-digit verification code:</p>
            <h2 style='color: #10B981; font-size: 32px; letter-spacing: 5px; padding: 10px; background: #f3f4f6; display: inline-block; border-radius: 8px;'>{otp}</h2>
            <p>If you did not request a password reset, please ignore this email or reply to let us know. This verification code is only valid for the next 15 minutes.</p>
            <p>Thanks,<br>The SmartJobPortal Team</p>
        ";

        await _emailService.SendEmailAsync(user.Email, "Password Reset Verification Code - SmartJobPortal", emailBody);

        // We no longer return the OTP in the response, to ensure it must be read from the email.
        return ApiResponse<string>.Ok(null, "Verification code sent successfully to your email.");
    }
}
