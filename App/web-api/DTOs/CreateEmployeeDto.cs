using System.ComponentModel.DataAnnotations;

namespace web_api.DTOs;

public class CreateEmployeeDto
{
    [Required(ErrorMessage = "Логин обязателен")]
    public string Login { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Имя обязательно")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Роль обязательна")]
    public string Role { get; set; } = string.Empty;
    
    public string? Email { get; set; }
    public string? Phone { get; set; }
    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}
