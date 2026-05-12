using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<string>>
{
    private readonly IUserRepository _repo;

    public RegisterCommandHandler(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<string>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var existingUser = await _repo.GetByEmailAsync(request.Email!);

        if (existingUser != null)
        {
            return ApiResponse<string>.FailureResponse(
                new List<string> { "Email is already registered" },
                "Duplicate Email"
            );
        }

        var roleId = await _repo.GetRoleIdByName(request.Role!);

        var user = new User
        {
            FullName = request.FullName!,
            Email = request.Email!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            PhoneNumber = request.PhoneNumber!,
            RoleId = roleId,
            IsActive = true,
            IsApproved = request.Role != "Recruiter", // Recruiters need admin approval
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateUserAsync(user);

        return ApiResponse<string>.SuccessResponse(null, "User registered successfully");
    }
}
