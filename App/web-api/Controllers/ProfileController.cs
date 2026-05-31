using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.Models;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly VoenkomDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(VoenkomDbContext context, IWebHostEnvironment env, ILogger<ProfileController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users
            .Include(u => u.PersonalFile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        var pf = user.PersonalFile;
        var photoDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "photos");

        return Ok(new
        {
            id = user.Id,
            personalFileId = user.PersonalFileId,
            fio = user.Name,
            dateOfBirth = user.DateOfBirth?.ToString("dd.MM.yyyy"),
            dateOfBirthRaw = user.DateOfBirth,
            registrationAddress = user.RegistrationAddress,
            residenceAddress = user.ResidenceAddress,
            passportSeries = user.Passport_series ?? pf?.PassportSeries,
            passportNumber = user.Passport_number ?? pf?.PassportNumber,
            passportIssued = user.Passport_issued ?? pf?.PassportIssueAuthority,
            passportDate = (user.Passport_date ?? pf?.PassportIssueDate)?.ToString("dd.MM.yyyy"),
            phone = user.Phone,
            email = user.Email ?? pf?.Email,
            militaryTicketNumber = user.MilitaryTicketNumber,
            fitnessCategory = user.FitnessCategory,
            accountStatus = user.AccountStatus ?? "Состоит на учёте",
            passportPhotoPath = user.PassportPhotoPath ?? FindPhotoOnDisk(photoDir, "passport", userId),
            passportPhotoStatus = user.PassportPhotoStatus,
            militaryPhotoPath = user.MilitaryPhotoPath ?? FindPhotoOnDisk(photoDir, "military", userId),
            militaryPhotoStatus = user.MilitaryPhotoStatus
        });
    }

    private static string? FindPhotoOnDisk(string photoDir, string type, int userId)
    {
        try
        {
            var files = Directory.GetFiles(photoDir, $"{type}_{userId}.*");
            return files.Length > 0 ? $"/photos/{Path.GetFileName(files[0])}" : null;
        }
        catch
        {
            return null;
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        if (!string.IsNullOrEmpty(dto.Name))
            user.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Phone))
            user.Phone = dto.Phone;
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.RegistrationAddress))
            user.RegistrationAddress = dto.RegistrationAddress;
        if (!string.IsNullOrEmpty(dto.ResidenceAddress))
            user.ResidenceAddress = dto.ResidenceAddress;
        if (dto.DateOfBirth.HasValue)
            user.DateOfBirth = dto.DateOfBirth;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Профиль обновлён" });
    }

    private static (string? pathField, string? statusField) GetPhotoFields(User user, string type)
    {
        return type switch
        {
            "passport" => (user.PassportPhotoPath, user.PassportPhotoStatus),
            "military" => (user.MilitaryPhotoPath, user.MilitaryPhotoStatus),
            _ => (null, null)
        };
    }

    private static void SetPhotoFields(User user, string type, string path, string status)
    {
        switch (type)
        {
            case "passport":
                user.PassportPhotoPath = path;
                user.PassportPhotoStatus = status;
                break;
            case "military":
                user.MilitaryPhotoPath = path;
                user.MilitaryPhotoStatus = status;
                break;
        }
    }

    [HttpGet("photos/pending")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> GetPendingPhotos()
    {
        var users = await _context.Users
            .Where(u => u.PassportPhotoStatus == "pending" || u.MilitaryPhotoStatus == "pending")
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.PassportPhotoPath,
                u.PassportPhotoStatus,
                u.MilitaryPhotoPath,
                u.MilitaryPhotoStatus,
                u.Passport_series,
                u.Passport_number,
                u.Passport_issued,
                u.Passport_date
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("photo")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string type)
    {
        if (type != "passport" && type != "military")
            return BadRequest(new { message = "Неверный тип документа. Допустимо: passport, military" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var (_, currentStatus) = GetPhotoFields(user, type);
        if (currentStatus == "verified" || currentStatus == "pending")
            return BadRequest(new { message = "Нельзя загрузить фото: текущий статус «" + (currentStatus == "verified" ? "верифицировано" : "ожидает") + "»" });

        if (file.Length == 0)
            return BadRequest(new { message = "Файл пустой" });

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return BadRequest(new { message = "Только JPG и PNG" });

        var photoDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "photos");
        Directory.CreateDirectory(photoDir);

        var fileName = $"{type}_{userId}{ext}";
        var filePath = Path.Combine(photoDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        SetPhotoFields(user, type, $"/photos/{fileName}", "pending");
        await _context.SaveChangesAsync();

        var (newPath, newStatus) = GetPhotoFields(user, type);
        return Ok(new { photoPath = newPath, photoStatus = newStatus });
    }

    [HttpPost("photo/verify/{userId}")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> VerifyPhoto(int userId, [FromBody] PhotoActionDto dto)
    {
        if (dto.Type != "passport" && dto.Type != "military")
            return BadRequest(new { message = "Неверный тип документа" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var (currentPath, _) = GetPhotoFields(user, dto.Type);
        SetPhotoFields(user, dto.Type, currentPath, "verified");
        await _context.SaveChangesAsync();

        return Ok(new { message = "Фото верифицировано" });
    }

    [HttpPost("photo/reject/{userId}")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> RejectPhoto(int userId, [FromBody] PhotoActionDto dto)
    {
        if (dto.Type != "passport" && dto.Type != "military")
            return BadRequest(new { message = "Неверный тип документа" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var (currentPath, _) = GetPhotoFields(user, dto.Type);
        SetPhotoFields(user, dto.Type, currentPath, "rejected");
        await _context.SaveChangesAsync();

        return Ok(new { message = "Фото отклонено" });
    }
}

public class PhotoActionDto
{
    public string Type { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? RegistrationAddress { get; set; }
    public string? ResidenceAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
