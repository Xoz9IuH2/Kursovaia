namespace web_api.Models;

public class Appointment
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public PersonalFile? PersonalFile { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "scheduled"; // scheduled, completed, cancelled
    public int AssignedEmployeeId { get; set; }
    public User? AssignedEmployee { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
