namespace web_api.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // medical, commission, drafting, meeting
    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsAvailable { get; set; } = true;
    public int MaxSlots { get; set; } = 10;
    public int BookedSlots { get; set; } = 0;
}

public class TimeSlot
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public CalendarEvent? CalendarEvent { get; set; }
    public string Time { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Status { get; set; } = "available"; // available, booked, cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}