using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.LoginAsync(dto, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            token = result.Token,
            user = result.User
        });
    }

    /// <summary>
    /// Первый вход (смена временного пароля)
    /// </summary>
    [HttpPost("first-login")]
    [AllowAnonymous]
    public async Task<IActionResult> FirstLogin([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.FirstLoginAsync(dto, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            token = result.Token,
            user = result.User
        });
    }

    /// <summary>
    /// Смена пароля
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _authService.ChangePasswordAsync(userId, dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Получить данные текущего пользователя
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Получить всех пользователей (только для сотрудников)
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Создать сотрудника
    /// </summary>
    [HttpPost("users")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.CreateEmployeeAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetAllUsers), new
        {
            message = result.Message,
            tempPassword = result.TempPassword
        });
    }

    /// <summary>
    /// Зарегистрировать гражданина
    /// </summary>
    [HttpPost("register-citizen")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> RegisterCitizen([FromBody] CreateCitizenDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterCitizenAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            tempPassword = result.TempPassword
        });
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    [HttpDelete("users/{id}")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _authService.DeleteUserAsync(id, currentUserId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }
}
