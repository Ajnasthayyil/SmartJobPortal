using Dapper;
using iText.StyledXmlParser.Jsoup.Select;
using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Features.Feed.DTOs;
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

    public async Task AddMediaAsync(
    List<PostMedia> media)
    {
        using var connection =
            _factory.CreateConnection();

        var sql = """
        INSERT INTO PostMedia
        (
            PostId,
            MediaUrl,
            PublicId,
            MediaType,
            DisplayOrder
        )
        VALUES
        (
            @PostId,
            @MediaUrl,
            @PublicId,
            'Image',
            @DisplayOrder
        )
        """;

        await connection.ExecuteAsync(
            sql,
            media);
    }

    public async Task ReactToPostAsync(
    int postId,
    int userId,
    string reactionType)
    {
        using var connection =
            _factory.CreateConnection();

        var existing =
            await connection.QueryFirstOrDefaultAsync<int?>(
            """
        SELECT PostReactionId
        FROM PostReactions
        WHERE PostId = @PostId
        AND UserId = @UserId
        """,
            new
            {
                PostId = postId,
                UserId = userId
            });

        if (existing.HasValue)
        {
            await connection.ExecuteAsync(
            """
        UPDATE PostReactions
        SET ReactionType = @ReactionType
        WHERE PostReactionId = @Id
        """,
            new
            {
                Id = existing.Value,
                ReactionType = reactionType
            });

            return;
        }

        await connection.ExecuteAsync(
        """
    INSERT INTO PostReactions
    (
        PostId,
        UserId,
        ReactionType,
        CreatedAt
    )
    VALUES
    (
        @PostId,
        @UserId,
        @ReactionType,
        GETUTCDATE()
    )
    """,
        new
        {
            PostId = postId,
            UserId = userId,
            ReactionType = reactionType
        });
    }

    public async Task<List<PostReactionDto>>
    GetReactionsAsync(int postId)
    {
        using var connection =
            _factory.CreateConnection();

        var reactions =
            await connection.QueryAsync<PostReactionDto>(
            """
        SELECT
            ReactionType,
            COUNT(*) AS Count
        FROM PostReactions
        WHERE PostId = @PostId
        GROUP BY ReactionType
        """,
            new { PostId = postId });

        return reactions.ToList();
    }
}