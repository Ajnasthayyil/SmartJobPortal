using MediatR;

namespace SmartJobPortal.Application.Features.Resume.Queries.HasResume;

public record HasResumeQuery(int UserId) : IRequest<bool>;
