using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class AuditService
{
    private readonly VoenkomDbContext _context;

    public AuditService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<(List<AuditLogDto> Items, int Total)> GetAllAsync(string? action = null, int? userId = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<string>> GetDistinctActionsAsync()
    {
        return await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    public async Task LogAsync(int? userId, string action, string? details = null, string? ipAddress = null, string? tableName = null, int? recordId = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            Details = details,
            IpAddress = ipAddress ?? "unknown",
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    private AuditLogDto MapToDto(AuditLog a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        UserName = null,
        UserRole = null,
        Action = a.Action,
        TableName = a.TableName,
        RecordId = a.RecordId,
        Details = a.Details,
        IpAddress = a.IpAddress,
        CreatedAt = a.CreatedAt
    };
}
