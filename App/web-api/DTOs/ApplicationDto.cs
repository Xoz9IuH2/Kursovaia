namespace web_api.DTOs;

public class ApplicationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserName { get; set; }
}

public static class ApplicationDtoMapper
{
    public static ApplicationDto Map(Models.Application a) => new()
    {
        Id = a.Id,
        Type = a.Type,
        Title = a.Title,
        Content = a.Content,
        Status = a.Status,
        RejectionReason = a.RejectionReason,
        ReviewedAt = a.ReviewedAt,
        CreatedAt = a.CreatedAt,
        UserName = a.PersonalFile != null
            ? $"{a.PersonalFile.LastName} {a.PersonalFile.FirstName} {a.PersonalFile.Patronymic}"
            : null
    };
}
