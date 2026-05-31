using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.Models;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly VoenkomDbContext _context;
    private readonly ILogger<FeedController> _logger;

    public FeedController(VoenkomDbContext context, ILogger<FeedController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeed()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                action = n.Action,
                isRead = n.IsRead,
                createdAt = n.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                createdAtRaw = n.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            user = new { name = user?.Name?.Split(' ').FirstOrDefault() ?? "Гость" },
            items = notifications
        });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notif == null)
            return NotFound(new { message = "Уведомление не найдено" });

        notif.IsRead = true;
        notif.ReadAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(notif.Action) && notif.Action.StartsWith("summon:"))
        {
            var summonIdStr = notif.Action.Split(':')[1];
            if (int.TryParse(summonIdStr, out var summonId))
            {
                var summon = await _context.Summons.FindAsync(summonId);
                if (summon != null && summon.Status == "sent")
                {
                    summon.Status = "delivered";
                    summon.IsRead = true;
                    summon.ReadAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Прочитано" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, DateTime.UtcNow));

        return Ok(new { message = "Все прочитано" });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(new { count = count });
    }
}