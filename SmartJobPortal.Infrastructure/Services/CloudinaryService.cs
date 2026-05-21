using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var account = new Account(
            config["CloudinarySettings:CloudName"],
            config["CloudinarySettings:ApiKey"],
            config["CloudinarySettings:ApiSecret"]
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<UploadedMediaDto?> UploadImageAsync(
        IFormFile file)
    {
        if (file.Length <= 0)
            return null;

        await using var stream =
            file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(
                file.FileName,
                stream),

            Folder = "talex/feed",

            Transformation = new Transformation()
                .Width(1200)
                .Crop("limit")
                .Quality("auto")
                .FetchFormat("auto")
        };

        var result = await _cloudinary
            .UploadAsync(uploadParams);

        if (result.StatusCode != System.Net.HttpStatusCode.OK)
            return null;

        return new UploadedMediaDto
        {
            Url = result.SecureUrl.ToString(),
            PublicId = result.PublicId
        };
    }
}