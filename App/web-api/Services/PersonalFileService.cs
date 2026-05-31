using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class PersonalFileService
{
    private readonly VoenkomDbContext _context;

    public PersonalFileService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<(List<PersonalFileDto> Items, int Total)> GetAllAsync(string? search = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.PersonalFiles.AsQueryable();

        query = ApplyFilters(query, search, status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p => MapToDto(p)).ToList();

        return (dtos, total);
    }

    public async Task<PersonalFileDto?> GetByIdAsync(int id)
    {
        var file = await _context.PersonalFiles
            .FirstOrDefaultAsync(p => p.Id == id);

        return file == null ? null : MapToDto(file);
    }

    public async Task<PersonalFileDto?> GetByUserIdAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.PersonalFile)
            .ThenInclude(p => p!.AssignedEmployee)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.PersonalFile == null ? null : MapToDto(user.PersonalFile);
    }

    public async Task<(bool Success, string Message, PersonalFileDto? File)> CreateAsync(CreatePersonalFileDto dto)
    {
        var existsByPassport = await _context.PersonalFiles
            .AnyAsync(p => p.PassportSeries == dto.PassportSeries && 
                         p.PassportNumber == dto.PassportNumber);
        
        if (existsByPassport)
            return (false, "Личное дело с таким паспортом уже существует", null);

        var birthDateUtc = DateTime.SpecifyKind(dto.BirthDate, DateTimeKind.Utc);
        var existsByName = await _context.PersonalFiles
            .AnyAsync(p => p.LastName == dto.LastName && 
                         p.FirstName == dto.FirstName && 
                         p.BirthDate == birthDateUtc);
        
        if (existsByName)
            return (false, "Личное дело с такими ФИО и датой рождения уже существует", null);

        var birthDate = DateTime.SpecifyKind(dto.BirthDate, DateTimeKind.Utc);
        var passportIssueDate = DateTime.SpecifyKind(dto.PassportIssueDate, DateTimeKind.Utc);

        var file = new PersonalFile
        {
            LastName = dto.LastName,
            FirstName = dto.FirstName,
            Patronymic = dto.Patronymic,
            BirthDate = birthDate,
            Gender = dto.Gender,
            BirthPlace = dto.BirthPlace,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            PassportSeries = dto.PassportSeries,
            PassportNumber = dto.PassportNumber,
            PassportIssueDate = passportIssueDate,
            PassportIssueAuthority = dto.PassportIssueAuthority,
            INN = dto.INN,
            SNILS = dto.SNILS,
            Education = dto.Education,
            Profession = dto.Profession,
            WorkPlace = dto.WorkPlace,
            Position = dto.Position,
            MilitaryRank = dto.MilitaryRank,
            MilitaryTicketSeries = dto.MilitaryTicketSeries,
            MilitaryTicketNumber = dto.MilitaryTicketNumber,
            FitnessCategory = dto.FitnessCategory,
            SpecialNotes = dto.SpecialNotes,
            AssignedEmployeeId = dto.AssignedEmployeeId,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PersonalFiles.Add(file);
        await _context.SaveChangesAsync();

        return (true, "Личное дело создано", await GetByIdAsync(file.Id));
    }

    public async Task<(bool Success, string Message, PersonalFileDto? File)> UpdateAsync(int id, CreatePersonalFileDto dto)
    {
        var file = await _context.PersonalFiles.FindAsync(id);
        if (file == null)
            return (false, "Личное дело не найдено", null);

        file.LastName = dto.LastName;
        file.FirstName = dto.FirstName;
        file.Patronymic = dto.Patronymic;
        file.BirthDate = DateTime.SpecifyKind(dto.BirthDate, DateTimeKind.Utc);
        file.Gender = dto.Gender;
        file.BirthPlace = dto.BirthPlace;
        file.Address = dto.Address;
        file.Phone = dto.Phone;
        file.Email = dto.Email;
        file.PassportSeries = dto.PassportSeries;
        file.PassportNumber = dto.PassportNumber;
        file.PassportIssueDate = DateTime.SpecifyKind(dto.PassportIssueDate, DateTimeKind.Utc);
        file.PassportIssueAuthority = dto.PassportIssueAuthority;
        file.INN = dto.INN;
        file.SNILS = dto.SNILS;
        file.Education = dto.Education;
        file.Profession = dto.Profession;
        file.WorkPlace = dto.WorkPlace;
        file.Position = dto.Position;
        file.MilitaryRank = dto.MilitaryRank;
        file.FitnessCategory = dto.FitnessCategory;
        file.SpecialNotes = dto.SpecialNotes;
        file.AssignedEmployeeId = dto.AssignedEmployeeId;
        file.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Личное дело обновлено", await GetByIdAsync(id));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var file = await _context.PersonalFiles.FindAsync(id);
        if (file == null)
            return (false, "Личное дело не найдено");

        _context.PersonalFiles.Remove(file);
        await _context.SaveChangesAsync();

        return (true, "Личное дело удалено");
    }

    public async Task<(bool Success, string Message)> ArchiveAsync(int id)
    {
        var file = await _context.PersonalFiles.FindAsync(id);
        if (file == null)
            return (false, "Личное дело не найдено");

        if (file.Status == "archived")
            return (false, "Личное дело уже в архиве");

        file.Status = "archived";
        file.ArchivedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return (true, "Личное дело заархивировано. Удаление через 5 дней.");
    }

    public async Task<int> DeleteExpiredArchivesAsync()
    {
        var expiredFiles = await _context.PersonalFiles
            .Where(p => p.Status == "archived" && p.ArchivedAt != null && p.ArchivedAt <= DateTime.UtcNow.AddDays(-5))
            .ToListAsync();

        _context.PersonalFiles.RemoveRange(expiredFiles);
        await _context.SaveChangesAsync();

        return expiredFiles.Count;
    }

    private IQueryable<PersonalFile> ApplyFilters(IQueryable<PersonalFile> query, string? search, string? status)
    {
        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(p =>
                p.LastName.ToLower().Contains(search) ||
                p.FirstName.ToLower().Contains(search) ||
                (p.Patronymic != null && p.Patronymic.ToLower().Contains(search)) ||
                p.PassportNumber.Contains(search) ||
                (p.SNILS != null && p.SNILS.Contains(search)));
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        return query;
    }

    private static PersonalFileDto MapToDto(PersonalFile p) => new()
    {
        Id = p.Id,
        LastName = p.LastName,
        FirstName = p.FirstName,
        Patronymic = p.Patronymic,
        BirthDate = p.BirthDate,
        Gender = p.Gender,
        BirthPlace = p.BirthPlace,
        Address = p.Address,
        Phone = p.Phone,
        Email = p.Email,
        PassportSeries = p.PassportSeries,
        PassportNumber = p.PassportNumber,
        PassportIssueDate = p.PassportIssueDate,
        PassportIssueAuthority = p.PassportIssueAuthority,
        INN = p.INN,
        SNILS = p.SNILS,
        Education = p.Education,
        Profession = p.Profession,
        WorkPlace = p.WorkPlace,
        Position = p.Position,
        MilitaryRank = p.MilitaryRank,
        MilitaryTicketSeries = p.MilitaryTicketSeries,
        MilitaryTicketNumber = p.MilitaryTicketNumber,
        FitnessCategory = p.FitnessCategory,
        SpecialNotes = p.SpecialNotes,
        Status = p.Status,
        AssignedEmployeeName = p.AssignedEmployee?.Name,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
