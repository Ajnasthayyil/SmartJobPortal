using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Chatbot.DTOs;
using System.Collections.Generic;

namespace SmartJobPortal.Application.Features.Chatbot.Queries.GetChildNodes;

public record GetChildNodesQuery(int NodeId) : IRequest<ApiResponse<IEnumerable<ChatbotNodeDto>>>;
