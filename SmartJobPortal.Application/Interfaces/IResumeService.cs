using Microsoft.AspNetCore.Http;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IResumeService
{
    Task<ApiResponse<ResumeParseResponse>> UploadAndParseAsync(int userId, IFormFile file);
}