using Dapper;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly IDbConnectionFactory _factory;

    public PostRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<int> CreateAsync(Post post)
    {
        using var connection =
            _factory.CreateConnection();

        var sql = """
            INSERT INTO Posts
            (
                UserId,
                Content,
                ImageUrl,
                LikesCount,
                CommentsCount,
                CreatedAt
            )
            VALUES
            (
                @UserId,
                @Content,
                @ImageUrl,
                0,
                0,
                @CreatedAt
            );

            SELECT CAST(SCOPE_IDENTITY() as int);
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            post);
    }

    public async Task<List<FeedPostDto>> GetFeedAsync(
        int page,
        int pageSize)
    {
        using var connection =
            _factory.CreateConnection();

        var skip = (page - 1) * pageSize;

        var sql = """
            SELECT
                p.PostId,
                p.UserId,
                u.FullName AS UserName,
                u.ProfilePictureUrl AS UserProfilePicture,
                p.Content,
                p.ImageUrl,
                p.LikesCount,
                p.CommentsCount,
                p.CreatedAt
            FROM Posts p
            INNER JOIN Users u
                ON p.UserId = u.UserId
            ORDER BY p.CreatedAt DESC
            OFFSET @Skip ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        var posts = await connection.QueryAsync<FeedPostDto>(
            sql,
            new
            {
                Skip = skip,
                PageSize = pageSize
            });

        return posts.ToList();
    }
}