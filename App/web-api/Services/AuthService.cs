using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class AuthService
{
    private readonly VoenkomDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(VoenkomDbContext context, JwtService jwtService, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? Token, UserDto? User)> LoginAsync(LoginDto dto, string ipAddress)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);

        if (user == null)
        {
            return (false, "Неверный логин или пароль", null, null);
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes + 1;
            return (false, $"Учетная запись заблокирована. Попробуйте через {remainingMinutes} мин.", null, null);
        }

        if (!BCryptNet.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            
            if (user.FailedLoginAttempts >= 3)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0;
                _logger.LogWarning("Пользователь {Login} заблокирован после 3 неудачных попыток", dto.Login);
            }

            await _context.SaveChangesAsync();
            return (false, "Неверный логин или пароль", null, null);
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _context.SaveChangesAsync();

        await LogAuditAsync(user.Id, "Login", $"Успешный вход с IP: {ipAddress}");

        var token = _jwtService.GenerateToken(user);
        var userDto = MapToUserDto(user);

        return (true, "Вход выполнен успешно", token, userDto);
    }

    public async Task<(bool Success, string Message, string? Token, UserDto? User)> FirstLoginAsync(LoginDto dto, string ipAddress)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);

        if (user == null)
        {
            return (false, "Неверный логин или пароль", null, null);
        }

        if (!BCryptNet.Verify(dto.Password, user.PasswordHash))
        {
            return (false, "Неверный логин или пароль", null, null);
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _context.SaveChangesAsync();

        await LogAuditAsync(user.Id, "FirstLogin", $"Первый вход с IP: {ipAddress}");

        var token = _jwtService.GenerateToken(user);
        var userDto = MapToUserDto(user);
        userDto!.MustChangePassword = user.MustChangePassword;

        return (true, user.MustChangePassword ? "Требуется смена пароля" : "Вход выполнен успешно", token, userDto);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "Пользователь не найден");
        }

        if (!user.MustChangePassword)
        {
            if (!BCryptNet.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return (false, "Неверный текущий пароль");
            }
        }

        var validationResult = ValidatePassword(dto.NewPassword);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.Message);
        }

        user.PasswordHash = BCryptNet.HashPassword(dto.NewPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "ChangePassword", "Пароль изменён");

        return (true, "Пароль изменён");
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();

        return users.Select(MapToUserDto).ToList()!;
    }

    public async Task<List<UserDto>> GetEmployeesAsync()
    {
        var employees = await _context.Users
            .Where(u => u.Role == "employee" && u.IsActive)
            .ToListAsync();

        return employees.Select(MapToUserDto).ToList()!;
    }

    public async Task<(bool Success, string Message, string? TempPassword)> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
        {
            return (false, "Пользователь с таким логином уже существует", null);
        }

        var user = new User
        {
            Login = dto.Login,
            PasswordHash = BCryptNet.HashPassword(dto.Password),
            Role = dto.Role,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            MustChangePassword = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await LogAuditAsync(user.Id, "CreateUser", $"Создан пользователь: {user.Name}, роль: {user.Role}");

        return (true, "Сотрудник успешно создан", null);
    }

    public async Task<(bool Success, string Message, string? TempPassword)> RegisterCitizenAsync(CreateCitizenDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
        {
            return (false, "Пользователь с таким логином уже существует", null);
        }

        var personalFile = await _context.PersonalFiles.FindAsync(dto.PersonalFileId);
        if (personalFile == null)
        {
            return (false, "Личное дело не найдено", null);
        }

        if (personalFile.Status != "active")
        {
            return (false, "Личное дело неактивно", null);
        }

        var tempPassword = GenerateTempPassword();

        var user = new User
        {
            Login = dto.Login,
            PasswordHash = BCryptNet.HashPassword(tempPassword),
            Role = "citizen",
            Name = $"{personalFile.LastName} {personalFile.FirstName} {personalFile.Patronymic}".Trim(),
            Email = dto.Login,
            Phone = personalFile.Phone,
            PersonalFileId = dto.PersonalFileId,
            MustChangePassword = true,
            DateOfBirth = personalFile.BirthDate,
            RegistrationAddress = personalFile.Address,
            ResidenceAddress = personalFile.Address,
            Passport_series = personalFile.PassportSeries,
            Passport_number = personalFile.PassportNumber,
            Passport_issued = personalFile.PassportIssueAuthority,
            Passport_date = personalFile.PassportIssueDate,
            MilitaryTicketNumber = personalFile.MilitaryTicketSeries != null && personalFile.MilitaryTicketNumber != null 
                ? $"{personalFile.MilitaryTicketSeries} №{personalFile.MilitaryTicketNumber}" 
                : null,
            FitnessCategory = personalFile.FitnessCategory,
            AccountStatus = "Состоит на учёте"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var notification = new Notification
        {
            UserId = user.Id,
            Title = "✅ Регистрация",
            Message = $"Добро пожаловать! Вы были зарегистрированы в системе ВОЕНКОМ. Ваш временный пароль: {tempPassword}. При первом входе необходимо сменить пароль.",
            Type = "info",
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await LogAuditAsync(user.Id, "RegisterCitizen", $"Зарегистрирован гражданин: {user.Name}");

        return (true, "Гражданин успешно зарегистрирован", tempPassword);
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(int userId, int currentUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "Пользователь не найден");
        }

        if (user.Role == "admin")
        {
            return (false, "Невозможно удалить администратора");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await LogAuditAsync(currentUserId, "DeleteUser", $"Удален пользователь: {user.Name}");

        return (true, "Пользователь удалён");
    }

    private (bool IsValid, string Message) ValidatePassword(string password)
    {
        if (password.Length < 8)
        {
            return (false, "Пароль должен содержать минимум 8 символов");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Пароль должен содержать хотя бы одну заглавную букву");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Пароль должен содержать хотя бы одну цифру");
        }

        return (true, "");
    }

private string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private UserDto? MapToUserDto(User user)
    {
        if (user == null) return null;
        return new UserDto
        {
            Id = user.Id,
            Login = user.Login,
            Role = user.Role,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            MustChangePassword = user.MustChangePassword,
            fio = user.Name,
            accountStatus = user.AccountStatus ?? "Состоит на учёте"
        };
    }

    private async Task LogAuditAsync(int? userId, string action, string details, string? tableName = null, int? recordId = null)
    {
        var log = new AuditLog
        {
            UserId = userId ?? 0,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            Details = details,
            IpAddress = "",
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}