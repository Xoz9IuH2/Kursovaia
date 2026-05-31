using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class GeoLocationService
{
    private readonly VoenkomDbContext _context;

    public GeoLocationService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<List<GeoLocationDto>> GetAllAsync(string? groupName = null)
    {
        var query = _context.GeoLocations.AsQueryable();

        if (!string.IsNullOrEmpty(groupName))
            query = query.Where(g => g.GroupName == groupName);

        return await query
            .OrderByDescending(g => g.Timestamp)
            .Select(g => MapToDto(g))
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctGroupNamesAsync()
    {
        return await _context.GeoLocations
            .Select(g => g.GroupName)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();
    }

    public async Task<GeoLocationDto?> GetLatestByGroupAsync(string groupName)
    {
        return await _context.GeoLocations
            .Where(g => g.GroupName == groupName)
            .OrderByDescending(g => g.Timestamp)
            .Select(g => MapToDto(g))
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message, GeoLocationDto? Location)> CreateAsync(CreateGeoLocationDto dto)
    {
        var location = new GeoLocation
        {
            GroupName = dto.GroupName,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Description = dto.Description,
            Timestamp = DateTime.UtcNow
        };

        _context.GeoLocations.Add(location);
        await _context.SaveChangesAsync();

        return (true, "Геолокация сохранена", MapToDto(location));
    }

    public async Task<(bool Success, string Message)> DeleteOldAsync(int hoursOld = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hoursOld);
        var oldLocations = await _context.GeoLocations
            .Where(g => g.Timestamp < cutoff)
            .ToListAsync();

        _context.GeoLocations.RemoveRange(oldLocations);
        await _context.SaveChangesAsync();

        return (true, $"Удалено {oldLocations.Count} записей");
    }

    private GeoLocationDto MapToDto(GeoLocation g) => new()
    {
        Id = g.Id,
        GroupName = g.GroupName,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        Description = g.Description,
        Timestamp = g.Timestamp
    };
}
