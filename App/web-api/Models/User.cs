namespace web_api.Models;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int? PersonalFileId { get; set; }
    public PersonalFile? PersonalFile { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string? RegistrationAddress { get; set; }
    public string? ResidenceAddress { get; set; }
    public string? Passport_series { get; set; }
    public string? Passport_number { get; set; }
    public string? Passport_issued { get; set; }
    public DateTime? Passport_date { get; set; }
    public string? MilitaryTicketNumber { get; set; }
    public string? FitnessCategory { get; set; }
    public string? AccountStatus { get; set; }
    public string? PassportPhotoPath { get; set; }
    public string? PassportPhotoStatus { get; set; }
    public string? MilitaryPhotoPath { get; set; }
    public string? MilitaryPhotoStatus { get; set; }
}
