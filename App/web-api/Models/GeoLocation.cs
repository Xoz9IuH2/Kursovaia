namespace web_api.Models;

public class GeoLocation
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
