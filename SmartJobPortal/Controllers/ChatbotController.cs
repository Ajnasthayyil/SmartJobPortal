using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Features.Chatbot.Queries.GetChildNodes;
using SmartJobPortal.Application.Features.Chatbot.Queries.GetRootNodes;
using SmartJobPortal.Application.Features.Chatbot.Queries.SearchKeyword;
using System.Threading.Tasks;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatbotController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("root")]
    public async Task<IActionResult> GetRootNodes()
    {
        var result = await _mediator.Send(new GetRootNodesQuery());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("children/{nodeId:int}")]
    public async Task<IActionResult> GetChildNodes(int nodeId)
    {
        var result = await _mediator.Send(new GetChildNodesQuery(nodeId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchKeyword([FromQuery] string keyword)
    {
        var result = await _mediator.Send(new SearchKeywordQuery(keyword));
        return StatusCode(result.StatusCode, result);
    }
}
