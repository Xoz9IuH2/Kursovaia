using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class EvaderService
{
    private readonly VoenkomDbContext _context;

    public EvaderService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<List<EvaderDto>> GetAllAsync(string? status = null)
    {
        var query = _context.Evaders
            .Include(e => e.PersonalFile)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => MapToDto(e))
            .ToListAsync();
    }

    public async Task<EvaderDto?> GetByIdAsync(int id)
    {
        var evader = await _context.Evaders
            .Include(e => e.PersonalFile)
            .FirstOrDefaultAsync(e => e.Id == id);

        return evader == null ? null : MapToDto(evader);
    }

    public async Task<(bool Success, string Message, EvaderDto? Evader)> CreateAsync(int userId, int personalFileId, string description, string reason)
    {
        var personalFile = await _context.PersonalFiles.FindAsync(personalFileId);
        if (personalFile == null)
            return (false, "Личное дело не найдено", null);

        var protocolNumber = $"EV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var evader = new Evader
        {
            PersonalFileId = personalFileId,
            ProtocolNumber = protocolNumber,
            ProtocolDate = DateTime.UtcNow,
            Description = description,
            Reason = reason,
            Status = "active",
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Evaders.Add(evader);
        await _context.SaveChangesAsync();

        return (true, "Дело на уклониста создано", await GetByIdAsync(evader.Id));
    }

    public async Task<(bool Success, string Message)> CloseAsync(int id, string? resolution)
    {
        var evader = await _context.Evaders.FindAsync(id);
        if (evader == null)
            return (false, "Дело не найдено");

        evader.Status = "closed";
        evader.Resolution = resolution;
        evader.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Дело закрыто");
    }

    private EvaderDto MapToDto(Evader e) => new()
    {
        Id = e.Id,
        PersonalFileId = e.PersonalFileId,
        LastName = e.PersonalFile?.LastName,
        FirstName = e.PersonalFile?.FirstName,
        ProtocolNumber = e.ProtocolNumber,
        ProtocolDate = e.ProtocolDate,
        Description = e.Description,
        Reason = e.Reason,
        Status = e.Status,
        Resolution = e.Resolution,
        CreatedAt = e.CreatedAt
    };
}
