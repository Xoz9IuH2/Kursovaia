namespace web_api.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool MustChangePassword { get; set; }
    public string? fio { get; set; }
    public string? accountStatus { get; set; }
}
