using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Admin.Commands.UpdateProfile;

public class UpdateAdminProfileCommandHandler : IRequestHandler<UpdateAdminProfileCommand, ApiResponse<AdminProfileResponse>>
{
    private readonly IUserRepository _userRepo;
    private readonly ICacheService _cache;

    public UpdateAdminProfileCommandHandler(IUserRepository userRepo, ICacheService cache)
    {
        _userRepo = userRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<AdminProfileResponse>> Handle(UpdateAdminProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId);
        if (user == null)
            return ApiResponse<AdminProfileResponse>.NotFound("Admin not found.");

        await _userRepo.UpdateProfileAsync(command.UserId, command.Request.FullName, command.Request.PhoneNumber);

        await _cache.RemoveAsync($"user:{command.UserId}");
        await _cache.RemoveAsync($"user:email:{user.Email}");

        var updatedUser = await _userRepo.GetByIdAsync(command.UserId);
        if (updatedUser == null)
            return ApiResponse<AdminProfileResponse>.NotFound("Admin not found.");

        return ApiResponse<AdminProfileResponse>.Ok(new AdminProfileResponse
        {
            UserId = updatedUser.UserId,
            FullName = updatedUser.FullName,
            Email = updatedUser.Email,
            PhoneNumber = updatedUser.PhoneNumber,
            ProfilePictureUrl = updatedUser.ProfilePictureUrl,
            CreatedAt = updatedUser.CreatedAt
        }, "Admin profile updated successfully.");
    }
}
