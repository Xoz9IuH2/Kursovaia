namespace web_api.DTOs;

public class AppointmentDto
{
    public int Id { get; set; }
    public int PersonalFileId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
