namespace web_api.DTOs;

public class GeoLocationDto
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CreateGeoLocationDto
{
    public string GroupName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
}
