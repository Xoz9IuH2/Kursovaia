namespace web_api.DTOs;

public class CreateCalendarEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
}
