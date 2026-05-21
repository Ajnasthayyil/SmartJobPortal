using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly ICloudinaryService _cloudinary;

    public MediaController(
        ICloudinaryService cloudinary)
    {
        _cloudinary = cloudinary;
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file)
    {
        var result =
            await _cloudinary.UploadImageAsync(file);

        if (result == null)
            return BadRequest(ApiResponse<UploadedMediaDto>.Fail("Upload failed"));

        return Ok(ApiResponse<UploadedMediaDto>.SuccessResponse(result, "File uploaded successfully"));
    }
}