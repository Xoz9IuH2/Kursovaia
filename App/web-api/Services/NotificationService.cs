using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class NotificationService
{
    private readonly VoenkomDbContext _context;

    public NotificationService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> GetByUserIdAsync(int userId)
    {
        var items = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task<int> CreateAsync(int userId, string title, string message, string type, string? action = null, int? personalFileId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Action = action,
            PersonalFileId = personalFileId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification.Id;
    }

    public async Task<(bool Success, string Message)> MarkAsReadAsync(int userId, int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return (false, "Уведомление не найдено");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(notification.Action) && notification.Action.StartsWith("summon:"))
        {
            var summonIdStr = notification.Action.Split(':')[1];
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

        return (true, "Уведомление отмечено как прочитанное");
    }

    public async Task<(bool Success, string Message)> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(n.Action) && n.Action.StartsWith("summon:"))
            {
                var summonIdStr = n.Action.Split(':')[1];
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
        }

        await _context.SaveChangesAsync();

        return (true, "Все уведомления отмечены как прочитанные");
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.CreatedAt
    };
}
