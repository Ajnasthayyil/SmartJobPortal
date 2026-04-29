using Microsoft.AspNetCore.Http;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IResumeService
{
    // Upload + AI parsing
    Task<ApiResponse<ResumeParseResponse>> UploadAndParseAsync(int userId, IFormFile file);

    // Download resume file
    Task<(byte[] bytes, string contentType, string fileName)?> GetResumeFileAsync(int userId);

    // ✅ NEW: Check if resume exists
    Task<bool> HasResumeAsync(int userId);

    // ✅ NEW: Delete resume (important for re-upload)
    Task<ApiResponse<bool>> DeleteResumeAsync(int userId);
}