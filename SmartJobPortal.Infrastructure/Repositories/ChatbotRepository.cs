using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class ChatbotRepository : IChatbotRepository
{
    private readonly IDbConnectionFactory _factory;

    public ChatbotRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<ChatbotNode>> GetRootNodesAsync()
    {
        try
        {
            using var connection = _factory.CreateConnection();
            var sql = @"SELECT Id, Title, Message, ParentId, RouteUrl, DisplayOrder, IsActive, CreatedAt
                        FROM ChatbotNodes
                        WHERE ParentId IS NULL AND IsActive = 1
                        ORDER BY DisplayOrder ASC";
            return await connection.QueryAsync<ChatbotNode>(sql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database Error] GetRootNodesAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<ChatbotNode>> GetChildrenAsync(int parentId)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            var sql = @"SELECT Id, Title, Message, ParentId, RouteUrl, DisplayOrder, IsActive, CreatedAt
                        FROM ChatbotNodes
                        WHERE ParentId = @ParentId AND IsActive = 1
                        ORDER BY DisplayOrder ASC";
            return await connection.QueryAsync<ChatbotNode>(sql, new { ParentId = parentId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database Error] GetChildrenAsync failed for ParentId {parentId}: {ex.Message}");
            throw;
        }
    }

    public async Task<ChatbotNode?> SearchKeywordAsync(string keyword)
    {
        try
        {
            using var connection = _factory.CreateConnection();
            var sql = @"SELECT TOP 1 Id, Title, Message, ParentId, RouteUrl, DisplayOrder, IsActive, CreatedAt
                        FROM ChatbotNodes
                        WHERE IsActive = 1 AND (Title LIKE '%' + @Keyword + '%' OR Message LIKE '%' + @Keyword + '%')
                        ORDER BY 
                            CASE 
                                WHEN Title LIKE '%' + @Keyword + '%' THEN 1 
                                ELSE 2 
                            END, 
                            DisplayOrder ASC";
            return await connection.QueryFirstOrDefaultAsync<ChatbotNode>(sql, new { Keyword = keyword });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database Error] SearchKeywordAsync failed for '{keyword}': {ex.Message}");
            throw;
        }
    }
}
