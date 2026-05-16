using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Features.Feed.Commands.CreatePost;
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

    [HttpGet]
    public async Task<IActionResult> GetFeed(
        int page = 1,
        int pageSize = 10)
    {
        var query = new GetFeedQuery
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }
}