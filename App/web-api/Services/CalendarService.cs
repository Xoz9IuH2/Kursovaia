using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class CalendarService
{
    private readonly VoenkomDbContext _context;

    public CalendarService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<List<CalendarEventDto>> GetAllAsync(DateTime? date = null, string? eventType = null)
    {
        var query = _context.CalendarEvents.AsQueryable();

        if (date.HasValue)
            query = query.Where(c => c.EventDate.Date == date.Value.Date);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(c => c.EventType == eventType);

        var items = await query
            .OrderBy(c => c.EventDate)
            .ThenBy(c => c.StartTime)
            .ToListAsync();

        return items.Select(c => MapToDto(c)).ToList();
    }

    public async Task<CalendarEventDto?> GetByIdAsync(int id)
    {
        var evt = await _context.CalendarEvents.FindAsync(id);
        return evt == null ? null : MapToDto(evt);
    }

    public async Task<(bool Success, string Message, CalendarEventDto? Event)> CreateAsync(int userId, CreateCalendarEventDto dto)
    {
        var evt = new CalendarEvent
        {
            Title = dto.Title,
            Description = dto.Description,
            EventDate = DateTime.SpecifyKind(dto.EventDate, DateTimeKind.Utc),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Location = dto.Location,
            EventType = dto.EventType,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CalendarEvents.Add(evt);
        await _context.SaveChangesAsync();

        return (true, "Событие создано", await GetByIdAsync(evt.Id));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var evt = await _context.CalendarEvents.FindAsync(id);
        if (evt == null)
            return (false, "Событие не найдено");

        _context.CalendarEvents.Remove(evt);
        await _context.SaveChangesAsync();

        return (true, "Событие удалено");
    }

    private static CalendarEventDto MapToDto(CalendarEvent c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        EventDate = c.EventDate,
        StartTime = c.StartTime,
        EndTime = c.EndTime,
        Location = c.Location,
        EventType = c.EventType,
        CreatedAt = c.CreatedAt
    };
}
