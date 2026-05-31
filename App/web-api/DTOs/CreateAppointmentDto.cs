namespace web_api.DTOs;

public class CreateAppointmentDto
{
    public DateTime AppointmentDate { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? AssignedEmployeeId { get; set; }
}
