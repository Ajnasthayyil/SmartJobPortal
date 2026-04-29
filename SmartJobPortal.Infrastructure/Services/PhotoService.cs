using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services;

public class PhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;

    public PhotoService(IConfiguration config)
    {
        var acc = new Account(
            config["CloudinarySettings:CloudName"],
            config["CloudinarySettings:ApiKey"],
            config["CloudinarySettings:ApiSecret"]
        );

        _cloudinary = new Cloudinary(acc);
    }

    public async Task<string> UploadPhotoAsync(IFormFile file)
    {
        if (file.Length > 0)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face"),
                Folder = "smart-job-portal/profile-pics"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }

        return string.Empty;
    }

    public async Task DeletePhotoAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }
}
