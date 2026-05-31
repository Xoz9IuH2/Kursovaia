using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class AppointmentService
{
    private readonly VoenkomDbContext _context;

    public AppointmentService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<(List<AppointmentDto> Items, int Total)> GetAllAsync(string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Appointments
            .Include(a => a.PersonalFile)
            .Include(a => a.AssignedEmployee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items.Select(MapToDto).ToList(), total);
    }

    public async Task<List<AppointmentDto>> GetByPersonalFileIdAsync(int personalFileId)
    {
        var items = await _context.Appointments
            .Where(a => a.PersonalFileId == personalFileId)
            .Include(a => a.AssignedEmployee)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.PersonalFile)
            .Include(a => a.AssignedEmployee)
            .FirstOrDefaultAsync(a => a.Id == id);

        return appointment == null ? null : MapToDto(appointment);
    }

    public async Task<(bool Success, string Message)> CreateAsync(int personalFileId, CreateAppointmentDto dto)
    {
        int employeeId = dto.AssignedEmployeeId ?? 0;
        if (employeeId == 0)
        {
            var anyEmployee = await _context.Users
                .Where(u => u.Role == "employee" && u.IsActive)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();
            if (anyEmployee == null)
                return (false, "Нет доступных сотрудников");
            employeeId = anyEmployee.Id;
        }

        var employee = await _context.Users.FindAsync(employeeId);
        if (employee == null || employee.Role != "employee")
            return (false, "Сотрудник не найден");

        var appointment = new Appointment
        {
            PersonalFileId = personalFileId,
            AppointmentDate = DateTime.SpecifyKind(dto.AppointmentDate, DateTimeKind.Utc),
            Time = dto.Time,
            Purpose = dto.Purpose,
            Notes = dto.Notes,
            AssignedEmployeeId = employeeId,
            Status = "scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);

        await _context.SaveChangesAsync();

        return (true, "Запись на приём успешно создана");
    }

    public async Task<(bool Success, string Message)> CompleteAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return (false, "Запись не найдена");

        appointment.Status = "completed";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Приём завершён");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return (false, "Запись не найдена");

        appointment.Status = "cancelled";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Запись отменена");
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        PersonalFileId = a.PersonalFileId,
        AppointmentDate = a.AppointmentDate,
        Time = a.Time,
        Purpose = a.Purpose,
        Notes = a.Notes,
        Status = a.Status,
        UserName = a.PersonalFile != null
            ? $"{a.PersonalFile.LastName} {a.PersonalFile.FirstName} {a.PersonalFile.Patronymic}"
            : "",
        EmployeeName = a.AssignedEmployee?.Name ?? "",
        CreatedAt = a.CreatedAt
    };
}
