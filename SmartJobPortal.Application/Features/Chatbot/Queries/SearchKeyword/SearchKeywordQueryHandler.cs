using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Chatbot.DTOs;
using SmartJobPortal.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SmartJobPortal.Application.Features.Chatbot.Queries.SearchKeyword;

public class SearchKeywordQueryHandler : IRequestHandler<SearchKeywordQuery, ApiResponse<ChatbotNodeDto>>
{
    private readonly IChatbotService _chatbotService;

    public SearchKeywordQueryHandler(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    public async Task<ApiResponse<ChatbotNodeDto>> Handle(SearchKeywordQuery request, CancellationToken cancellationToken)
    {
        var node = await _chatbotService.SearchKeywordAsync(request.Keyword);
        if (node == null)
            return ApiResponse<ChatbotNodeDto>.NotFound("No matching support topic found.");

        return ApiResponse<ChatbotNodeDto>.Ok(node);
    }
}
