using SmartJobPortal.Application.DTOs.Feed;
using SmartJobPortal.Application.Features.Feed.DTOs;
using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IPostRepository
{
    Task<int> CreateAsync(Post post);

    Task<List<FeedPostDto>> GetFeedAsync(
        int page,
        int pageSize);

    Task AddMediaAsync(
    List<PostMedia> media);

    Task ReactToPostAsync(
    int postId,
    int userId,
    string reactionType);

    Task<List<PostReactionDto>>
    GetReactionsAsync(int postId);
}