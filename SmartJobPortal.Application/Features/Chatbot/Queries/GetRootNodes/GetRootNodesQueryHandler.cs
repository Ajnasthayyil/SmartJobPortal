using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Chatbot.DTOs;
using SmartJobPortal.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Features.Chatbot.Queries.GetRootNodes;

public class GetRootNodesQueryHandler : IRequestHandler<GetRootNodesQuery, ApiResponse<IEnumerable<ChatbotNodeDto>>>
{
    private readonly IChatbotService _chatbotService;

    public GetRootNodesQueryHandler(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    public async Task<ApiResponse<IEnumerable<ChatbotNodeDto>>> Handle(GetRootNodesQuery request, CancellationToken cancellationToken)
    {
        var rootNodes = await _chatbotService.GetRootNodesAsync();
        return ApiResponse<IEnumerable<ChatbotNodeDto>>.Ok(rootNodes);
    }
}
