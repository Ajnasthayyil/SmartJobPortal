using MediatR;
using Microsoft.AspNetCore.Http;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Candidate;

namespace SmartJobPortal.Application.Features.Resume.Commands.UploadResume;

public record UploadResumeCommand(int UserId, IFormFile File) : IRequest<ApiResponse<ResumeParseResponse>>;
