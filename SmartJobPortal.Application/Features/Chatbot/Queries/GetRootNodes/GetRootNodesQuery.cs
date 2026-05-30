using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Chatbot.DTOs;
using System.Collections.Generic;

namespace SmartJobPortal.Application.Features.Chatbot.Queries.GetRootNodes;

public record GetRootNodesQuery() : IRequest<ApiResponse<IEnumerable<ChatbotNodeDto>>>;
