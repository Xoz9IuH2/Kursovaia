using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly StatisticsService _statisticsService;
    private readonly AuditService _auditService;
    private readonly GeoLocationService _geoLocationService;
    private readonly ReportService _reportService;
    private readonly VoenkomDbContext _context;

    public EmployeeController(
        AuthService authService,
        StatisticsService statisticsService,
        AuditService auditService,
        GeoLocationService geoLocationService,
        ReportService reportService,
        VoenkomDbContext context)
    {
        _authService = authService;
        _statisticsService = statisticsService;
        _auditService = auditService;
        _geoLocationService = geoLocationService;
        _reportService = reportService;
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _statisticsService.GetDashboardAsync();
        return Ok(stats);
    }

    [HttpGet("statistics/monthly")]
    public async Task<IActionResult> GetMonthlyStatistics([FromQuery] int year, [FromQuery] int month)
    {
        var stats = await _statisticsService.GetMonthlyStatisticsAsync(year, month);
        return Ok(stats);
    }

    [HttpGet("statistics/summons-by-status")]
    public async Task<IActionResult> GetSummonsByStatus()
    {
        var stats = await _statisticsService.GetSummonsByStatusAsync();
        return Ok(stats);
    }

    [HttpGet("statistics/applications-by-status")]
    public async Task<IActionResult> GetApplicationsByStatus()
    {
        var stats = await _statisticsService.GetApplicationsByStatusAsync();
        return Ok(stats);
    }

    [HttpGet("statistics/applications-by-type")]
    public async Task<IActionResult> GetApplicationsByType()
    {
        var stats = await _statisticsService.GetApplicationsByTypeAsync();
        return Ok(stats);
    }

    [HttpGet("statistics/fitness-categories")]
    public async Task<IActionResult> GetFitnessCategories()
    {
        var stats = await _statisticsService.GetFitnessCategoriesAsync();
        return Ok(stats);
    }

    [HttpGet("statistics/education-levels")]
    public async Task<IActionResult> GetEducationLevels()
    {
        var stats = await _statisticsService.GetEducationLevelsAsync();
        return Ok(stats);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        var result = await _authService.CreateEmployeeAsync(dto);
        return result.Success ? Ok(new { message = result.Message, tempPassword = result.TempPassword }) : BadRequest(new { message = result.Message });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _authService.DeleteUserAsync(id, currentUserId);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? action,
        [FromQuery] int? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (items, total) = await _auditService.GetAllAsync(action, userId, from, to, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("audit/actions")]
    public async Task<IActionResult> GetAuditActions()
    {
        var actions = await _auditService.GetDistinctActionsAsync();
        return Ok(actions);
    }

    [HttpGet("geolocation")]
    public async Task<IActionResult> GetGeoLocations([FromQuery] string? groupName)
    {
        var locations = await _geoLocationService.GetAllAsync(groupName);
        return Ok(locations);
    }

    [HttpGet("geolocation/groups")]
    public async Task<IActionResult> GetGeoLocationGroups()
    {
        var groups = await _geoLocationService.GetDistinctGroupNamesAsync();
        return Ok(groups);
    }

    [HttpGet("geolocation/{groupName}/latest")]
    public async Task<IActionResult> GetLatestGeoLocation(string groupName)
    {
        var location = await _geoLocationService.GetLatestByGroupAsync(groupName);
        return location == null ? NotFound() : Ok(location);
    }

    [HttpPost("geolocation")]
    public async Task<IActionResult> CreateGeoLocation([FromBody] CreateGeoLocationDto dto)
    {
        var result = await _geoLocationService.CreateAsync(dto);
        return result.Success ? Ok(new { message = result.Message, location = result.Location }) : BadRequest(new { message = result.Message });
    }

    [HttpDelete("geolocation/cleanup")]
    public async Task<IActionResult> CleanupGeoLocations([FromQuery] int hoursOld = 24)
    {
        var result = await _geoLocationService.DeleteOldAsync(hoursOld);
        return Ok(new { message = result.Message });
    }

    [HttpGet("reports/summons")]
    public async Task<IActionResult> GetSummonsReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var data = await _reportService.GenerateSummonsReportAsync(from, to);
        return File(data, "text/csv", $"summons_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
    }

    [HttpGet("reports/applications")]
    public async Task<IActionResult> GetApplicationsReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var data = await _reportService.GenerateApplicationsReportAsync(from, to);
        return File(data, "text/csv", $"applications_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
    }

    [HttpGet("reports/evaders")]
    public async Task<IActionResult> GetEvadersReport()
    {
        var data = await _reportService.GenerateEvadersReportAsync();
        return File(data, "text/csv", $"evaders_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("reports/personal-files")]
    public async Task<IActionResult> GetPersonalFilesReport()
    {
        var data = await _reportService.GeneratePersonalFilesReportAsync();
        return File(data, "text/csv", $"personal_files_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalFiles = await _context.PersonalFiles.CountAsync();
        var totalSummons = await _context.Summons.CountAsync();
        var pendingSummons = await _context.Summons.CountAsync(s => s.Status == "pending");
        var deliveredSummons = await _context.Summons.CountAsync(s => s.Status == "delivered");
        var arrivedSummons = await _context.Summons.CountAsync(s => s.Status == "arrived" || s.Status == "completed");
        var noShowSummons = await _context.Summons.CountAsync(s => s.Status == "no-show");
        var pendingApplications = await _context.Applications.CountAsync(a => a.Status == "pending");

        return Ok(new
        {
            totalFiles,
            totalSummons,
            pendingSummons,
            fulfilledSummons = arrivedSummons,
            missedSummons = noShowSummons,
            pendingApplications,
            deliveredSummons
        });
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetAllDocuments()
    {
        var docs = await _context.Documents
            .Include(d => d.PersonalFile)
            .Include(d => d.UploadedBy)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                id = d.Id,
                name = d.FileName,
                file_type = d.DocumentType,
                file_path = d.FilePath,
                status = d.Status,
                rejection_reason = d.RejectionReason,
                uploaded_at = d.CreatedAt,
                uploaded_by_name = d.UploadedBy != null ? d.UploadedBy.Name : null,
                last_name = d.PersonalFile != null ? d.PersonalFile.LastName : null,
                first_name = d.PersonalFile != null ? d.PersonalFile.FirstName : null,
                middle_name = d.PersonalFile != null ? d.PersonalFile.Patronymic : null
            })
            .ToListAsync();

        return Ok(docs);
    }

    [HttpGet("documents/pending")]
    public async Task<IActionResult> GetPendingDocuments()
    {
        var docs = await _context.Documents
            .Include(d => d.PersonalFile)
            .Include(d => d.UploadedBy)
            .Where(d => d.Status == "pending")
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                id = d.Id,
                name = d.FileName,
                file_type = d.DocumentType,
                file_path = d.FilePath,
                status = d.Status,
                uploaded_at = d.CreatedAt,
                uploaded_by_name = d.UploadedBy != null ? d.UploadedBy.Name : null,
                last_name = d.PersonalFile != null ? d.PersonalFile.LastName : null,
                first_name = d.PersonalFile != null ? d.PersonalFile.FirstName : null,
                middle_name = d.PersonalFile != null ? d.PersonalFile.Patronymic : null
            })
            .ToListAsync();

        return Ok(docs);
    }

    [HttpPost("documents/{id}/verify")]
    public async Task<IActionResult> VerifyDocument(int id, [FromBody] VerifyDocumentDto dto)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null)
            return NotFound(new { message = "Документ не найден" });

        var status = dto.Status == "verified" ? "approved" : dto.Status;
        if (status != "approved" && status != "rejected")
            return BadRequest(new { message = "Недопустимый статус" });

        doc.Status = status;
        doc.RejectionReason = status == "rejected" ? dto.RejectionReason : null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Документ " + (status == "approved" ? "верифицирован" : "отклонён") });
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendarEvents()
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

    [HttpPost("calendar")]
    public async Task<IActionResult> CreateCalendarEvent([FromBody] CreateCalendarEventDto dto)
    {
        var evt = new Models.CalendarEvent
        {
            Title = dto.Title ?? dto.EventType ?? "",
            Description = dto.Description ?? "",
            EventDate = dto.EventDate,
            StartTime = dto.StartTime ?? "09:00",
            EndTime = dto.EndTime ?? "10:00",
            Location = dto.Location ?? "",
            EventType = dto.EventType ?? "",
            CreatedById = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!),
            IsAvailable = true,
            MaxSlots = 30,
            BookedSlots = 0
        };

        _context.CalendarEvents.Add(evt);
        await _context.SaveChangesAsync();

        return Ok(new { id = evt.Id, message = "Мероприятие создано" });
    }

    [HttpPost("register-citizen")]
    public async Task<IActionResult> RegisterCitizen([FromBody] CreateCitizenDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterCitizenAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            tempPassword = result.TempPassword
        });
    }
}

public class VerifyDocumentDto
{
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}
