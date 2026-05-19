using MediatR;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Resume.Queries.GetResumeFile;

public class GetResumeFileQueryHandler : IRequestHandler<GetResumeFileQuery, (byte[] bytes, string contentType, string fileName)?>
{
    private readonly ICandidateRepository _candidateRepo;

    public GetResumeFileQueryHandler(ICandidateRepository candidateRepo)
    {
        _candidateRepo = candidateRepo;
    }

    public async Task<(byte[] bytes, string contentType, string fileName)?> Handle(GetResumeFileQuery request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(request.UserId);
        if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFilePath) || !File.Exists(candidate.ResumeFilePath))
            return null;

        var bytes = await File.ReadAllBytesAsync(candidate.ResumeFilePath);
        var ext = Path.GetExtension(candidate.ResumeFilePath).ToLowerInvariant();
        var contentType = ext == ".pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        return (bytes, contentType, candidate.ResumeOriginalName ?? "resume");
    }
}
