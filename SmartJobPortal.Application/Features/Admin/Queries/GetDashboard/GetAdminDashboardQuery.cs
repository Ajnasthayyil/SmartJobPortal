using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Admin;

namespace SmartJobPortal.Application.Features.Admin.Queries.GetDashboard;

public record GetAdminDashboardQuery() : IRequest<ApiResponse<AdminDashboardResponse>>;
