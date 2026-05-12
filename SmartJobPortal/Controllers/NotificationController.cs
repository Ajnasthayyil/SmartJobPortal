using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.NotificationDTOs;
using SmartJobPortal.Application.Features.Notification.Commands.CreateNotification;
using SmartJobPortal.Application.Features.Notification.Commands.MarkAllAsRead;
using SmartJobPortal.Application.Features.Notification.Commands.MarkAsRead;
using SmartJobPortal.Application.Features.Notification.Queries.GetUnreadCount;
using SmartJobPortal.Application.Features.Notification.Queries.GetUserNotifications;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator)
        => _mediator = mediator;

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetUserNotificationsQuery(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var result = await _mediator.Send(new MarkAsReadCommand(UserId, notificationId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _mediator.Send(new MarkAllAsReadCommand(UserId));
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        var result = await _mediator.Send(new CreateNotificationCommand(
            request.TargetUserId, 
            request.Title, 
            request.Message, 
            "Alert"));
        return StatusCode(result.StatusCode, result);
    }
}