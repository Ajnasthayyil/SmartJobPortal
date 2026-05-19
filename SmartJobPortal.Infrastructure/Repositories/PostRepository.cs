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
        int pageSize,
        int? currentUserId = null)
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
                p.CreatedAt,
                (SELECT TOP 1 ReactionType FROM PostReactions WHERE PostId = p.PostId AND UserId = @CurrentUserId) AS CurrentUserReaction
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
                PageSize = pageSize,
                CurrentUserId = currentUserId
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

        var existingReaction =
            await connection.QueryFirstOrDefaultAsync<string>(
            """
        SELECT ReactionType
        FROM PostReactions
        WHERE PostId = @PostId
        AND UserId = @UserId
        """,
            new
            {
                PostId = postId,
                UserId = userId
            });

        if (existingReaction != null)
        {
            if (existingReaction == reactionType)
            {
                // Toggle off
                await connection.ExecuteAsync(
                """
                DELETE FROM PostReactions
                WHERE PostId = @PostId AND UserId = @UserId;
                
                UPDATE Posts
                SET LikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId)
                WHERE PostId = @PostId;
                """,
                new { PostId = postId, UserId = userId });
            }
            else
            {
                // Change reaction
                await connection.ExecuteAsync(
                """
                UPDATE PostReactions
                SET ReactionType = @ReactionType
                WHERE PostId = @PostId AND UserId = @UserId;
                
                UPDATE Posts
                SET LikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId)
                WHERE PostId = @PostId;
                """,
                new { PostId = postId, UserId = userId, ReactionType = reactionType });
            }
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
    );
    
    UPDATE Posts
    SET LikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId)
    WHERE PostId = @PostId;
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

    public async Task<int> CreateCommentAsync(
    PostComment comment)
    {
        using var connection =
            _factory.CreateConnection();

        var sql = """
        INSERT INTO PostComments
        (
            PostId,
            UserId,
            ParentCommentId,
            Content,
            CreatedAt,
            IsDeleted
        )
        VALUES
        (
            @PostId,
            @UserId,
            @ParentCommentId,
            @Content,
            @CreatedAt,
            0
        );

        SELECT CAST(SCOPE_IDENTITY() as int);
        """;

        return await connection
            .ExecuteScalarAsync<int>(
                sql,
                comment);
    }


    public async Task<List<CommentDto>>
    GetCommentsAsync(int postId)
    {
        using var connection =
            _factory.CreateConnection();

        var sql = """
        SELECT
            pc.PostCommentId,
            pc.PostId,
            pc.UserId,
            u.FullName AS UserName,
            pc.Content,
            pc.ParentCommentId,
            pc.CreatedAt
        FROM PostComments pc
        INNER JOIN Users u
            ON u.UserId = pc.UserId
        WHERE pc.PostId = @PostId
        AND pc.IsDeleted = 0
        ORDER BY pc.CreatedAt ASC
        """;

        var comments = await connection.QueryAsync<CommentDto>(
            sql,
            new { PostId = postId });

        return comments.ToList();
    }

    public async Task<List<ReactionDto>> GetPostReactionsAsync(int postId)
    {
        using var connection = _factory.CreateConnection();
        var sql = """
            SELECT 
                pr.UserId, 
                u.FullName AS UserName, 
                u.ProfilePictureUrl, 
                pr.ReactionType 
            FROM PostReactions pr
            INNER JOIN Users u ON pr.UserId = u.UserId
            WHERE pr.PostId = @PostId
            ORDER BY pr.CreatedAt DESC
            """;
        
        var reactions = await connection.QueryAsync<ReactionDto>(sql, new { PostId = postId });
        return reactions.ToList();
    }
}