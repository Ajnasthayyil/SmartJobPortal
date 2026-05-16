using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        IFormFile file)
    {
        var result =
            await _cloudinary.UploadImageAsync(file);

        if (result == null)
            return BadRequest("Upload failed");

        return Ok(result);
    }
}