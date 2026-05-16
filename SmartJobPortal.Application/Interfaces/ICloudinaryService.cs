using Microsoft.AspNetCore.Http;
using SmartJobPortal.Application.DTOs.Feed;

namespace SmartJobPortal.Application.Interfaces;

public interface ICloudinaryService
{
    Task<UploadedMediaDto?> UploadImageAsync(
        IFormFile file);
}