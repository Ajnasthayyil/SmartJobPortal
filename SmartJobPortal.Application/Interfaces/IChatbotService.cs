using System.Collections.Generic;
using System.Threading.Tasks;
using SmartJobPortal.Application.Features.Chatbot.DTOs;

namespace SmartJobPortal.Application.Interfaces;

public interface IChatbotService
{
    Task<IEnumerable<ChatbotNodeDto>> GetRootNodesAsync();
    Task<IEnumerable<ChatbotNodeDto>> GetChildNodesAsync(int parentId);
    Task<ChatbotNodeDto?> SearchKeywordAsync(string keyword);
}
