using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Feed;

namespace SmartJobPortal.Application.Interfaces;

public interface IPostService
{
    Task<ApiResponse<int>> CreatePostAsync(
        int userId,
        CreatePostRequest dto);

    Task<ApiResponse<List<FeedPostDto>>> GetFeedAsync(
        int page,
        int pageSize);
}