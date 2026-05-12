using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Candidate.Queries.GetCompanies;

public record GetCompaniesQuery() : IRequest<ApiResponse<List<CompanyResponse>>>;
