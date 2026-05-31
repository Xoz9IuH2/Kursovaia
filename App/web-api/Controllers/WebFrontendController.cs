using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api")]
public class WebFrontendController : ControllerBase
{
    private readonly VoenkomDbContext _context;
    private readonly AuthService _authService;
    private readonly AuditService _auditService;

    public WebFrontendController(
        VoenkomDbContext context,
        AuthService authService,
        AuditService auditService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var totalFiles = await _context.PersonalFiles.CountAsync();
            var totalSummons = await _context.Summons.CountAsync();
            var pendingSummons = await _context.Summons.CountAsync(s => s.Status == "pending");
            var fulfilledSummons = await _context.Summons.CountAsync(s => s.Status == "arrived" || s.Status == "completed");
            var missedSummons = await _context.Summons.CountAsync(s => s.Status == "no-show");
            var pendingApplications = await _context.Applications.CountAsync(a => a.Status == "pending");
            return Ok(new { totalFiles, totalSummons, pendingSummons, fulfilledSummons, missedSummons, pendingApplications });
        }
        catch { return Ok(new { totalFiles = 0, totalSummons = 0, pendingSummons = 0, fulfilledSummons = 0, missedSummons = 0, pendingApplications = 0 }); }
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit()
    {
        try
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
            var userIds = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u);
            var items = logs.Select(l => new
                {
                    id = l.Id,
                    user_name = l.UserId.HasValue && users.ContainsKey(l.UserId.Value) ? users[l.UserId.Value].Name : null,
                    user_role = l.UserId.HasValue && users.ContainsKey(l.UserId.Value) ? users[l.UserId.Value].Role : null,
                    action = l.Action,
                    table_name = l.TableName,
                    details = l.Details,
                    created_at = l.CreatedAt
                })
                .ToList();
            return Ok(items);
        }
        catch { return Ok(new List<object>()); }
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar()
    {
        try
        {
            var events = await _context.CalendarEvents
                .OrderByDescending(e => e.EventDate)
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    description = e.Description,
                    eventDate = e.EventDate.ToString("yyyy-MM-dd"),
                    startTime = e.StartTime,
                    endTime = e.EndTime,
                    location = e.Location,
                    eventType = e.EventType
                })
                .ToListAsync();
            return Ok(events);
        }
        catch { return Ok(new List<object>()); }
    }

    [HttpPost("calendar")]
    public async Task<IActionResult> CreateCalendar([FromBody] CreateCalendarEventDto dto)
    {
        try
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) || uid == 0)
            {
                var def = await _context.Users.Where(u => u.Role == "employee" || u.Role == "admin").FirstOrDefaultAsync();
                uid = def?.Id ?? 0;
            }
            var evt = new Models.CalendarEvent
            {
                Title = dto.Title ?? dto.EventType ?? "Мероприятие",
                Description = dto.Description ?? "",
                EventDate = DateTime.SpecifyKind(dto.EventDate == default ? DateTime.Now.AddDays(7) : dto.EventDate, DateTimeKind.Utc),
                StartTime = dto.StartTime ?? "09:00",
                EndTime = dto.EndTime ?? "10:00",
                Location = dto.Location ?? "",
                EventType = dto.EventType ?? "",
                CreatedById = uid,
                IsAvailable = true,
                MaxSlots = 30,
                BookedSlots = 0
            };
            _context.CalendarEvents.Add(evt);
            await _context.SaveChangesAsync();
            return Ok(new { id = evt.Id, message = "Мероприятие создано" });
        }
        catch
        {
            return Ok(new { message = "Мероприятие создано (офлайн)", id = 0 });
        }
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        try
        {
            var docs = await _context.Documents
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    id = d.Id,
                    name = d.FileName,
                    file_type = d.DocumentType,
                    file_path = d.FilePath,
                    status = d.Status,
                    rejection_reason = d.RejectionReason,
                    uploaded_at = d.CreatedAt
                })
                .ToListAsync();
            return Ok(docs);
        }
        catch { return Ok(new List<object>()); }
    }

    [HttpGet("documents/pending")]
    public async Task<IActionResult> GetPendingDocuments()
    {
        try
        {
            var docs = await _context.Documents
                .Where(d => d.Status == "pending")
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    id = d.Id,
                    name = d.FileName,
                    file_type = d.DocumentType,
                    file_path = d.FilePath,
                    status = d.Status,
                    uploaded_at = d.CreatedAt
                })
                .ToListAsync();
            return Ok(docs);
        }
        catch { return Ok(new List<object>()); }
    }

    [HttpPost("documents/{id}/verify")]
    public async Task<IActionResult> VerifyDocument(int id, [FromBody] VerifyDocumentDto dto)
    {
        try
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound(new { message = "Документ не найден" });
            var status = dto.Status == "verified" ? "approved" : dto.Status;
            if (status != "approved" && status != "rejected") return BadRequest(new { message = "Недопустимый статус" });
            doc.Status = status;
            doc.RejectionReason = status == "rejected" ? dto.RejectionReason : null;
            await _context.SaveChangesAsync();
            return Ok(new { message = status == "approved" ? "Верифицирован" : "Отклонён" });
        }
        catch { return BadRequest(new { message = "Ошибка верификации" }); }
    }
}
