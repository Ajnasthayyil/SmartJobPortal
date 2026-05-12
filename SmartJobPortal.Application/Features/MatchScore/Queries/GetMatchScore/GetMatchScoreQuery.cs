using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.MatchScore.Queries.GetMatchScore;

public record GetMatchScoreQuery(int UserId, int JobId) : IRequest<ApiResponse<MatchScoreResponse>>;
