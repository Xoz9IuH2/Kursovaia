using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class SummonService
{
    private readonly VoenkomDbContext _context;
    private readonly NotificationService _notificationService;

    public SummonService(VoenkomDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<(List<SummonDto> Items, int Total)> GetAllAsync(string? search = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Summons
            .Include(s => s.PersonalFile)
            .Include(s => s.CreatedBy)
            .AsQueryable();

        query = ApplyFilters(query, search, status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var s in items)
        {
            try {
                if (!string.IsNullOrEmpty(s.Time) && TimeSpan.TryParse(s.Time, out var ts))
                {
                    var summonDateTime = s.SummonDate.Date + ts;
                    if (s.Status == "delivered" && summonDateTime < now)
                    {
                        s.Status = "no-show";
                    }
                }
            } catch { }
        }
        await _context.SaveChangesAsync();

        var dtos = items.Select(s => MapToDto(s)).ToList();

        return (dtos, total);
    }

    public async Task<List<SummonDto>> GetByPersonalFileIdAsync(int personalFileId)
    {
        var items = await _context.Summons
            .Include(s => s.PersonalFile)
            .Include(s => s.CreatedBy)
            .Where(s => s.PersonalFileId == personalFileId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var s in items)
        {
            try {
                if (!string.IsNullOrEmpty(s.Time) && TimeSpan.TryParse(s.Time, out var ts))
                {
                    var summonDateTime = s.SummonDate.Date + ts;
                    if ((s.Status == "sent" || s.Status == "delivered") && summonDateTime < now)
                    {
                        s.Status = "no-show";
                    }
                }
            } catch { }
        }
        await _context.SaveChangesAsync();

        return items.Select(s => MapToDto(s)).ToList();
    }

    public async Task<SummonDto?> GetByIdAsync(int id)
    {
        var summon = await _context.Summons
            .Include(s => s.PersonalFile)
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == id);

        return summon == null ? null : MapToDto(summon);
    }

    public async Task<(bool Success, string Message, SummonDto? Summon)> CreateAsync(int userId, CreateSummonDto dto)
    {
        var personalFile = await _context.PersonalFiles.FindAsync(dto.PersonalFileId);
        if (personalFile == null)
            return (false, "Личное дело не найдено", null);

        var summon = new Summon
        {
            PersonalFileId = dto.PersonalFileId,
            Title = dto.Title,
            Description = dto.Description,
            SummonDate = DateTime.SpecifyKind(dto.SummonDate, DateTimeKind.Utc),
            Time = dto.Time,
            Location = dto.Location,
            Reason = dto.Reason,
            Status = "sent",
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Summons.Add(summon);
        await _context.SaveChangesAsync();

        return (true, "Повестка создана", await GetByIdAsync(summon.Id));
    }

    public async Task<(bool Success, string Message, SummonDto? Summon)> UpdateAsync(int id, UpdateSummonDto dto)
    {
        var summon = await _context.Summons.FindAsync(id);
        if (summon == null)
            return (false, "Повестка не найдена", null);

        summon.Title = dto.Title;
        summon.Description = dto.Description;
        summon.SummonDate = DateTime.SpecifyKind(dto.SummonDate, DateTimeKind.Utc);
        summon.Time = dto.Time;
        summon.Location = dto.Location;
        summon.Reason = dto.Reason;
        summon.Status = dto.Status;
        summon.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Повестка обновлена", await GetByIdAsync(id));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var summon = await _context.Summons.FindAsync(id);
        if (summon == null)
            return (false, "Повестка не найдена");

        _context.Summons.Remove(summon);
        await _context.SaveChangesAsync();

        return (true, "Повестка удалена");
    }

    public async Task<(bool Success, string Message)> MarkAsReadAsync(int personalFileId, int summonId)
    {
        var summon = await _context.Summons
            .FirstOrDefaultAsync(s => s.Id == summonId && s.PersonalFileId == personalFileId);

        if (summon == null)
            return (false, "Повестка не найдена");

        if (!summon.IsRead)
        {
            summon.IsRead = true;
            summon.ReadAt = DateTime.UtcNow;
            summon.Status = "delivered";
            await _context.SaveChangesAsync();
        }

        return (true, "Повестка отмечена как прочитанная");
    }

    public async Task<(bool Success, string Message)> MarkArrivedAsync(int id)
    {
        var summon = await _context.Summons.FindAsync(id);
        if (summon == null)
            return (false, "Повестка не найдена");

        summon.Status = "arrived";
        summon.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Явка отмечена");
    }

    private IQueryable<Summon> ApplyFilters(IQueryable<Summon> query, string? search, string? status)
    {
        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(s =>
                s.Title.ToLower().Contains(search) ||
                (s.PersonalFile != null && s.PersonalFile.LastName.ToLower().Contains(search)) ||
                (s.PersonalFile != null && s.PersonalFile.FirstName.ToLower().Contains(search)));
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        return query;
    }

    private static SummonDto MapToDto(Summon s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Description = s.Description,
        SummonDate = s.SummonDate,
        Time = s.Time,
        Location = s.Location,
        Reason = s.Reason,
        Status = s.Status,
        IsRead = s.IsRead,
        ReadAt = s.ReadAt,
        CreatedAt = s.CreatedAt,
        PersonalFileId = s.PersonalFileId,
        LastName = s.PersonalFile?.LastName ?? string.Empty,
        FirstName = s.PersonalFile?.FirstName ?? string.Empty,
        Patronymic = s.PersonalFile?.Patronymic ?? string.Empty,
        CreatedByName = s.CreatedBy?.Name ?? string.Empty
    };
}
