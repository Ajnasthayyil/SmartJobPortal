using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Interfaces;

public interface IMatchScoreService
{
    Task<ApiResponse<MatchScoreResponse>> GetOrCalculateAsync(int userId, int jobId);
    Task<ApiResponse<List<MatchScoreResponse>>> GetBulkAsync(int userId, List<int> jobIds);
}