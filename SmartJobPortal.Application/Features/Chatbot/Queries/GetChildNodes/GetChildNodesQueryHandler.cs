using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Chatbot.DTOs;
using SmartJobPortal.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Features.Chatbot.Queries.GetChildNodes;

public class GetChildNodesQueryHandler : IRequestHandler<GetChildNodesQuery, ApiResponse<IEnumerable<ChatbotNodeDto>>>
{
    private readonly IChatbotService _chatbotService;

    public GetChildNodesQueryHandler(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    public async Task<ApiResponse<IEnumerable<ChatbotNodeDto>>> Handle(GetChildNodesQuery request, CancellationToken cancellationToken)
    {
        var childNodes = await _chatbotService.GetChildNodesAsync(request.NodeId);
        return ApiResponse<IEnumerable<ChatbotNodeDto>>.Ok(childNodes);
    }
}
