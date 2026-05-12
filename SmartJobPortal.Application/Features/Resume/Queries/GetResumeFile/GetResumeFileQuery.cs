using MediatR;

namespace SmartJobPortal.Application.Features.Resume.Queries.GetResumeFile;

public record GetResumeFileQuery(int UserId) : IRequest<(byte[] bytes, string contentType, string fileName)?>;
