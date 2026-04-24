using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IMatchScoreRepository
{
    Task<MatchScore?> GetAsync(int candidateId, int jobId);
    Task UpsertAsync(MatchScore score);
}