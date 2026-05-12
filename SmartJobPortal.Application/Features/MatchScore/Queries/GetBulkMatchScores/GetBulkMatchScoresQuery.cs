using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.MatchScore.Queries.GetBulkMatchScores;

public record GetBulkMatchScoresQuery(int UserId, List<int> JobIds) : IRequest<ApiResponse<List<MatchScoreResponse>>>;
