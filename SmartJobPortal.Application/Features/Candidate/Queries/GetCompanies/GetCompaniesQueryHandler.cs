using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Application.Features.Candidate.Queries.GetCompanies;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, ApiResponse<List<CompanyResponse>>>
{
    private readonly IJobRepository _jobRepo;

    public GetCompaniesQueryHandler(IJobRepository jobRepo)
    {
        _jobRepo = jobRepo;
    }

    public async Task<ApiResponse<List<CompanyResponse>>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = await _jobRepo.GetCompaniesAsync();
        return ApiResponse<List<CompanyResponse>>.Ok(companies);
    }
}
