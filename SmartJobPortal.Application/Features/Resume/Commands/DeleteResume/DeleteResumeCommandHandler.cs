using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Resume.Commands.DeleteResume;

public class DeleteResumeCommandHandler : IRequestHandler<DeleteResumeCommand, ApiResponse<bool>>
{
    private readonly ICandidateRepository _candidateRepo;
    private readonly ICacheService _cache;

    public DeleteResumeCommandHandler(ICandidateRepository candidateRepo, ICacheService cache)
    {
        _candidateRepo = candidateRepo;
        _cache = cache;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var candidate = await _candidateRepo.GetByUserIdAsync(userId);
        if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFilePath))
            return ApiResponse<bool>.Fail("No resume found.");

        try { if (File.Exists(candidate.ResumeFilePath)) File.Delete(candidate.ResumeFilePath); } catch { }

        candidate.ResumeFilePath = string.Empty;
        candidate.ResumeOriginalName = string.Empty;
        candidate.ResumeUploadedAt = null;
        await _candidateRepo.UpsertAsync(candidate);
        await _cache.RemoveAsync($"candidate:profile:{userId}");
        return ApiResponse<bool>.Ok(true, "Deleted.");
    }
}
