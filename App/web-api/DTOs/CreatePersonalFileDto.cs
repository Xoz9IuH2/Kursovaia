namespace web_api.DTOs;

public class CreatePersonalFileDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string BirthPlace { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PassportSeries { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public DateTime PassportIssueDate { get; set; }
    public string PassportIssueAuthority { get; set; } = string.Empty;
    public string? INN { get; set; }
    public string? SNILS { get; set; }
    public string Education { get; set; } = string.Empty;
    public string? Profession { get; set; }
    public string WorkPlace { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string MilitaryRank { get; set; } = string.Empty;
    public string? MilitaryTicketSeries { get; set; }
    public string? MilitaryTicketNumber { get; set; }
    public string FitnessCategory { get; set; } = string.Empty;
    public string? SpecialNotes { get; set; }
    public int? AssignedEmployeeId { get; set; }
}
