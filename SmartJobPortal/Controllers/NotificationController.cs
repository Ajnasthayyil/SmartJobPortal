using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Application.DTOs.NotificationDTOs;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationController(INotificationService service)
        => _service = service;

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetUserNotificationsAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _service.GetUnreadCountAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var result = await _service.MarkAsReadAsync(UserId, notificationId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _service.MarkAllAsReadAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        var result = await _service.CreateAsync(request.TargetUserId, request.Title, request.Message, "Alert");
        return StatusCode(result.StatusCode, result);
    }
}