using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Features.Feed.DTOs;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IPostRepository
{
    Task<int> CreateAsync(Post post);

    Task<List<FeedPostDto>> GetFeedAsync(
        int page,
        int pageSize,
        int? currentUserId = null);

    Task<FeedPostDto?> GetFeedPostByIdAsync(
        int postId,
        int? currentUserId = null);

    Task AddMediaAsync(
    List<PostMedia> media);

    Task ReactToPostAsync(
    int postId,
    int userId,
    string reactionType);

    Task<List<PostReactionDto>>
    GetReactionsAsync(int postId);

    Task<int> CreateCommentAsync(
    PostComment comment);

    Task<List<CommentDto>> GetCommentsAsync(
        int postId);

    Task<CommentDto?> GetCommentDtoByIdAsync(
        int commentId);

    Task<List<ReactionDto>> GetPostReactionsAsync(
        int postId);


    Task<Post?> GetPostByIdAsync(int postId);

    Task UpdatePostAsync(Post post);

    Task<PostComment?> GetCommentByIdAsync(int commentId);

    Task UpdateCommentAsync(PostComment comment);

    Task DeletePostAsync(int postId);

    Task DeleteCommentAsync(int commentId);
}