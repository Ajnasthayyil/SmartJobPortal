using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Features.Feed.Commands.CreateComment;
using SmartJobPortal.Application.Features.Feed.Commands.CreatePost;
using SmartJobPortal.Application.Features.Feed.Commands.DeleteComment;
using SmartJobPortal.Application.Features.Feed.Commands.DeletePost;
using SmartJobPortal.Application.Features.Feed.Commands.EditComment;
using SmartJobPortal.Application.Features.Feed.Commands.EditPost;
using SmartJobPortal.Application.Features.Feed.Commands.ReactPost;
using SmartJobPortal.Application.Features.Feed.Queries.GetComments;
using SmartJobPortal.Application.Features.Feed.Queries.GetFeed;
using System.Security.Claims;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/feed")]
public class FeedController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost(
        CreatePostCommand command)
    {
        command.UserId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetFeed(
        int page = 1,
        int pageSize = 10)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = string.IsNullOrEmpty(userIdClaim) ? null : int.Parse(userIdClaim);

        var query = new GetFeedQuery
        {
            Page = page,
            PageSize = pageSize,
            CurrentUserId = userId
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("{postId}/react")]
    public async Task<IActionResult> React(
    int postId,
    ReactPostCommand command)
    {
        command.PostId = postId;

        command.UserId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        var result =
            await _mediator.Send(command);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> AddComment(
    int postId,
    CreateCommentCommand command)
    {
        command.PostId = postId;

        command.UserId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        var result =
            await _mediator.Send(command);

        return Ok(result);
    }

    [HttpGet("{postId}/comments")]
    public async Task<IActionResult> GetComments(
        int postId)
    {
        var result =
            await _mediator.Send(
                new GetCommentsQuery
                {
                    PostId = postId
                });

        return Ok(result);
    }

    [HttpGet("{postId}/reactions")]
    public async Task<IActionResult> GetReactions(int postId)
    {
        var result = await _mediator.Send(new SmartJobPortal.Application
            .Features.Feed.Queries.GetPostReactions.GetPostReactionsQuery { PostId = postId });
        return Ok(result);
    }

    [Authorize]
    [HttpPut("posts/{postId}")]
    public async Task<IActionResult> EditPost(
    int postId,
    [FromBody] EditPostCommand command)
    {
        command.PostId = postId;

        command.UserId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        var result =
            await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> EditComment(
        int commentId,
        [FromBody] EditCommentCommand command)
    {
        command.CommentId = commentId;

        command.UserId = int.Parse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!);

        var result =
            await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var command = new DeletePostCommand
        {
            PostId = postId,
            UserId = int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!)
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var command = new DeleteCommentCommand
        {
            CommentId = commentId,
            UserId = int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!)
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}