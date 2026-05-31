using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.Models;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly VoenkomDbContext _context;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(VoenkomDbContext context, ILogger<CalendarController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] int month, [FromQuery] int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var events = await _context.CalendarEvents
            .Where(e => e.EventDate >= startDate && e.EventDate <= endDate && e.IsAvailable)
            .Select(e => new
            {
                id = e.Id,
                title = e.Title,
                date = e.EventDate.ToString("yyyy-MM-dd"),
                startTime = e.StartTime,
                endTime = e.EndTime,
                location = e.Location,
                eventType = e.EventType,
                maxSlots = e.MaxSlots,
                bookedSlots = e.BookedSlots
            })
            .ToListAsync();

        return Ok(new { items = events });
    }

    [HttpGet("slots/{eventId}")]
    public async Task<IActionResult> GetSlots(int eventId)
    {
        var event_ = await _context.CalendarEvents.FindAsync(eventId);
        if (event_ == null)
            return NotFound(new { message = "Событие не найдено" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var slots = new List<object>();
        var times = GenerateTimeSlots(event_.StartTime, event_.EndTime);

        foreach (var time in times)
        {
            var isBooked = await _context.TimeSlots
                .AnyAsync(s => s.CalendarEventId == eventId && s.Time == time && s.UserId == userId && s.Status == "booked");

            slots.Add(new { time = time, isBooked = isBooked });
        }

        return Ok(new { items = slots });
    }

    [HttpPost("book")]
    public async Task<IActionResult> BookSlot([FromBody] BookSlotDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FindAsync(userId);

        var event_ = await _context.CalendarEvents.FindAsync(dto.EventId);
        if (event_ == null || !event_.IsAvailable)
            return BadRequest(new { message = "Слот недоступен" });

        if (event_.EventDate < DateTime.Today)
            return BadRequest(new { message = "Дата уже прошла" });

        var existing = await _context.TimeSlots
            .FirstOrDefaultAsync(s => s.CalendarEventId == dto.EventId && s.Time == dto.Time && s.UserId == userId);

        if (existing != null)
            return BadRequest(new { message = "Вы уже записаны" });

        var slot = new TimeSlot
        {
            CalendarEventId = dto.EventId,
            Time = dto.Time,
            UserId = userId,
            Status = "booked"
        };

        _context.TimeSlots.Add(slot);

        var eventEntity = await _context.CalendarEvents.FindAsync(dto.EventId);
        if (eventEntity != null)
            eventEntity.BookedSlots++;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Запись подтверждена" });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyAppointments()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var appointments = await _context.TimeSlots
            .Include(s => s.CalendarEvent)
            .Where(s => s.UserId == userId && s.Status == "booked")
            .OrderBy(s => s.CalendarEvent!.EventDate)
            .Select(s => new
            {
                id = s.Id,
                eventTitle = s.CalendarEvent!.Title,
                date = s.CalendarEvent.EventDate.ToString("dd.MM.yyyy"),
                time = s.Time,
                location = s.CalendarEvent.Location,
                status = s.Status
            })
            .ToListAsync();

        return Ok(new { items = appointments });
    }

    private List<string> GenerateTimeSlots(string start, string end)
    {
        var slots = new List<string>();
        if (!TimeSpan.TryParse(start, out var startTs) || !TimeSpan.TryParse(end, out var endTs))
            return slots;

        var current = startTs;
        while (current < endTs)
        {
            slots.Add(current.ToString(@"hh\:mm"));
            current = current.Add(TimeSpan.FromMinutes(30));
        }

        return slots;
    }
}

public class BookSlotDto
{
    public int EventId { get; set; }
    public string Time { get; set; } = string.Empty;
}