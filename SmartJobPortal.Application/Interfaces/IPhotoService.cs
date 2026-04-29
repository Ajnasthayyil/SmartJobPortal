using Microsoft.AspNetCore.Http;

namespace SmartJobPortal.Application.Interfaces;

public interface IPhotoService
{
    Task<string> UploadPhotoAsync(IFormFile file);
    Task DeletePhotoAsync(string publicId);
}
