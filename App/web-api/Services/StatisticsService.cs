using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;

namespace web_api.Services;

public class StatisticsService
{
    private readonly VoenkomDbContext _context;

    public StatisticsService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardAsync()
    {
        var totalCitizens = await _context.Users.CountAsync(u => u.Role == "citizen");
        var totalEmployees = await _context.Users.CountAsync(u => u.Role == "employee");
        var activePersonalFiles = await _context.PersonalFiles.CountAsync(p => p.Status == "active");
        var pendingSummons = await _context.Summons.CountAsync(s => s.Status == "pending");
        var pendingApplications = await _context.Applications.CountAsync(a => a.Status == "pending");
        var scheduledAppointments = await _context.Appointments.CountAsync(a => a.Status == "scheduled");
        var activeEvaders = await _context.Evaders.CountAsync(e => e.Status == "active");

        return new DashboardStatsDto
        {
            TotalCitizens = totalCitizens,
            TotalEmployees = totalEmployees,
            ActivePersonalFiles = activePersonalFiles,
            PendingSummons = pendingSummons,
            PendingApplications = pendingApplications,
            ScheduledAppointments = scheduledAppointments,
            ActiveEvaders = activeEvaders
        };
    }

    public async Task<MonthlyStatsDto> GetMonthlyStatisticsAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var summonsCreated = await _context.Summons.CountAsync(s => s.CreatedAt >= startDate && s.CreatedAt < endDate);
        var summonsCompleted = await _context.Summons.CountAsync(s => s.Status == "arrived" && s.UpdatedAt >= startDate && s.UpdatedAt < endDate);
        var applicationsApproved = await _context.Applications.CountAsync(a => a.Status == "approved" && a.ReviewedAt >= startDate && a.ReviewedAt < endDate);
        var applicationsRejected = await _context.Applications.CountAsync(a => a.Status == "rejected" && a.ReviewedAt >= startDate && a.ReviewedAt < endDate);
        var appointmentsCompleted = await _context.Appointments.CountAsync(a => a.Status == "completed" && a.UpdatedAt >= startDate && a.UpdatedAt < endDate);
        var evadersCreated = await _context.Evaders.CountAsync(e => e.CreatedAt >= startDate && e.CreatedAt < endDate);

        return new MonthlyStatsDto
        {
            Year = year,
            Month = month,
            SummonsCreated = summonsCreated,
            SummonsCompleted = summonsCompleted,
            ApplicationsApproved = applicationsApproved,
            ApplicationsRejected = applicationsRejected,
            AppointmentsCompleted = appointmentsCompleted,
            EvadersCreated = evadersCreated
        };
    }

    public async Task<List<CategoryCountDto>> GetSummonsByStatusAsync()
    {
        return await _context.Summons
            .GroupBy(s => s.Status)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToListAsync();
    }

    public async Task<List<CategoryCountDto>> GetApplicationsByStatusAsync()
    {
        return await _context.Applications
            .GroupBy(a => a.Status)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToListAsync();
    }

    public async Task<List<CategoryCountDto>> GetApplicationsByTypeAsync()
    {
        return await _context.Applications
            .GroupBy(a => a.Type)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToListAsync();
    }

    public async Task<List<CategoryCountDto>> GetFitnessCategoriesAsync()
    {
        return await _context.PersonalFiles
            .GroupBy(p => p.FitnessCategory)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToListAsync();
    }

    public async Task<List<CategoryCountDto>> GetEducationLevelsAsync()
    {
        return await _context.PersonalFiles
            .GroupBy(p => p.Education)
            .Select(g => new CategoryCountDto { Category = g.Key, Count = g.Count() })
            .ToListAsync();
    }
}

public class DashboardStatsDto
{
    public int TotalCitizens { get; set; }
    public int TotalEmployees { get; set; }
    public int ActivePersonalFiles { get; set; }
    public int PendingSummons { get; set; }
    public int PendingApplications { get; set; }
    public int ScheduledAppointments { get; set; }
    public int ActiveEvaders { get; set; }
}

public class MonthlyStatsDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int SummonsCreated { get; set; }
    public int SummonsCompleted { get; set; }
    public int ApplicationsApproved { get; set; }
    public int ApplicationsRejected { get; set; }
    public int AppointmentsCompleted { get; set; }
    public int EvadersCreated { get; set; }
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}
