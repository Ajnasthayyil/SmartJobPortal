using System.Collections.Generic;
using System.Threading.Tasks;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IChatbotRepository
{
    Task<IEnumerable<ChatbotNode>> GetRootNodesAsync();
    Task<IEnumerable<ChatbotNode>> GetChildrenAsync(int parentId);
    Task<ChatbotNode?> SearchKeywordAsync(string keyword);
}
