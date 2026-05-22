using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.API.Controllers;

public class UploadMediaRequest
{
    public IFormFile File { get; set; } = null!;
}

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
        [FromForm] UploadMediaRequest request)
    {
        if (request == null || request.File == null)
            return BadRequest(ApiResponse<UploadedMediaDto>.Fail("No file uploaded"));

        var result =
            await _cloudinary.UploadImageAsync(request.File);

        if (result == null)
            return BadRequest(ApiResponse<UploadedMediaDto>.Fail("Upload failed"));

        return Ok(ApiResponse<UploadedMediaDto>.SuccessResponse(result, "File uploaded successfully"));
    }
}