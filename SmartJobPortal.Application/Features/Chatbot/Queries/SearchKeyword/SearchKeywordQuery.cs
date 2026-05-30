using MediatR;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.Features.Chatbot.DTOs;

namespace SmartJobPortal.Application.Features.Chatbot.Queries.SearchKeyword;

public record SearchKeywordQuery(string Keyword) : IRequest<ApiResponse<ChatbotNodeDto>>;
