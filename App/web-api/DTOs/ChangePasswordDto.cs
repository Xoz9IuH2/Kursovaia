using System.ComponentModel.DataAnnotations;

namespace web_api.DTOs;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Текущий пароль обязателен")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Новый пароль обязателен")]
    [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
    public string NewPassword { get; set; } = string.Empty;
}
