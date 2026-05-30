using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartJobPortal.Application.Features.Chatbot.DTOs;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services;

public class ChatbotService : IChatbotService
{
    private readonly IChatbotRepository _repository;

    public ChatbotService(IChatbotRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ChatbotNodeDto>> GetRootNodesAsync()
    {
        var nodes = await _repository.GetRootNodesAsync();
        return nodes.Select(n => new ChatbotNodeDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            ParentId = n.ParentId,
            RouteUrl = n.RouteUrl,
            DisplayOrder = n.DisplayOrder
        });
    }

    public async Task<IEnumerable<ChatbotNodeDto>> GetChildNodesAsync(int parentId)
    {
        var nodes = await _repository.GetChildrenAsync(parentId);
        return nodes.Select(n => new ChatbotNodeDto
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            ParentId = n.ParentId,
            RouteUrl = n.RouteUrl,
            DisplayOrder = n.DisplayOrder
        });
    }

    public async Task<ChatbotNodeDto?> SearchKeywordAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return null;

        var node = await _repository.SearchKeywordAsync(keyword.Trim());
        if (node == null)
            return null;

        return new ChatbotNodeDto
        {
            Id = node.Id,
            Title = node.Title,
            Message = node.Message,
            ParentId = node.ParentId,
            RouteUrl = node.RouteUrl,
            DisplayOrder = node.DisplayOrder
        };
    }
}
