using MediatR;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Resume.Queries.HasResume;

public class HasResumeQueryHandler : IRequestHandler<HasResumeQuery, bool>
{
    private readonly ICandidateRepository _candidateRepo;

    public HasResumeQueryHandler(ICandidateRepository candidateRepo)
    {
        _candidateRepo = candidateRepo;
    }

    public async Task<bool> Handle(HasResumeQuery request, CancellationToken cancellationToken)
    {
        var candidate = await _candidateRepo.GetByUserIdAsync(request.UserId);
        return candidate != null && !string.IsNullOrEmpty(candidate.ResumeFilePath) && File.Exists(candidate.ResumeFilePath);
    }
}
