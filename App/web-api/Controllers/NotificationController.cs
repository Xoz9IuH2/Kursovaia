using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly NotificationService _service;

    public NotificationController(NotificationService service)
    {
        _service = service;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        if (userId == null) return Ok(new List<object>());
        var notifications = await _service.GetByUserIdAsync(userId.Value);
        return Ok(new { items = notifications });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null) return Ok(new { count = 0 });
        var count = await _service.GetUnreadCountAsync(userId.Value);
        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.MarkAsReadAsync(userId.Value, id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var result = await _service.MarkAllAsReadAsync(userId.Value);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }
}
