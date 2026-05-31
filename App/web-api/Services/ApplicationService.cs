using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class ApplicationService
{
    private readonly VoenkomDbContext _context;

    public ApplicationService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<(List<ApplicationDto> Items, int Total)> GetAllAsync(string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Applications
            .Include(a => a.PersonalFile)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ApplicationDtoMapper.Map(a))
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<ApplicationDto>> GetByPersonalFileIdAsync(int personalFileId)
    {
        var apps = await _context.Applications
            .Where(a => a.PersonalFileId == personalFileId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return apps.Select(a => ApplicationDtoMapper.Map(a)).ToList();
    }

    public async Task<ApplicationDto?> GetByIdAsync(int id)
    {
        var application = await _context.Applications
            .Include(a => a.PersonalFile)
            .FirstOrDefaultAsync(a => a.Id == id);

        return application == null ? null : ApplicationDtoMapper.Map(application);
    }

    public async Task<(bool Success, string Message)> CreateAsync(int userId, int personalFileId, CreateApplicationDto dto)
    {
        var application = new Application
        {
            PersonalFileId = personalFileId,
            Type = dto.Type,
            Title = dto.Title,
            Content = dto.Content,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        var history = new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = "pending",
            Comment = "Заявление подано",
            CreatedAt = DateTime.UtcNow
        };
        _context.ApplicationStatusHistories.Add(history);
        await _context.SaveChangesAsync();

        return (true, "Заявление успешно отправлено");
    }

    public async Task<(bool Success, string Message)> ReviewAsync(int userId, int id, ApplicationReviewDto dto)
    {
        var application = await _context.Applications
            .Include(a => a.PersonalFile)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
            return (false, "Заявление не найдено");

        application.Status = dto.Status;
        application.RejectionReason = dto.Status == "rejected" ? dto.RejectionReason : null;
        application.ReviewedById = userId;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        var history = new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = dto.Status,
            Comment = dto.Comment,
            ChangedById = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ApplicationStatusHistories.Add(history);

        await _context.SaveChangesAsync();

        return (true, "Заявление обработано");
    }

    public async Task<List<ApplicationStatusHistoryDto>> GetHistoryAsync(int personalFileId, int applicationId)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.PersonalFileId == personalFileId);

        if (application == null)
            return new List<ApplicationStatusHistoryDto>();

        return await _context.ApplicationStatusHistories
            .Where(h => h.ApplicationId == applicationId)
            .Include(h => h.ChangedBy)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new ApplicationStatusHistoryDto
            {
                Id = h.Id,
                Status = h.Status,
                Comment = h.Comment,
                ChangedByName = h.ChangedBy != null ? h.ChangedBy.Name : null,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    private ApplicationDto MapToDto(Application a) => new()
    {
        Id = a.Id,
        Type = a.Type,
        Title = a.Title,
        Content = a.Content,
        Status = a.Status,
        RejectionReason = a.RejectionReason,
        ReviewedAt = a.ReviewedAt,
        CreatedAt = a.CreatedAt
    };
}
