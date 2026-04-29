using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;
using System.Security.Claims;

namespace SmartJobPortal.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IPhotoService _photoService;
    private readonly IUserRepository _userRepository;

    public ProfileController(IPhotoService photoService, IUserRepository userRepository)
    {
        _photoService = photoService;
        _userRepository = userRepository;
    }

    [HttpPost("upload-photo")]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(ApiResponse<string>.Unauthorized("User not found in token"));

        int userId = int.Parse(userIdClaim.Value);

        try
        {
            var resultUrl = await _photoService.UploadPhotoAsync(file);

            if (string.IsNullOrEmpty(resultUrl))
                return BadRequest(ApiResponse<string>.Fail("Failed to upload photo"));

            await _userRepository.UpdateProfilePictureAsync(userId, resultUrl);

            return Ok(ApiResponse<object>.SuccessResponse(new { url = resultUrl }, "Profile picture updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
        }
    }
}
