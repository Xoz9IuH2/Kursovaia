using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly AuthService _authService;

    public UserController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _authService.GetEmployeesAsync();
        return Ok(employees);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _authService.GetUserByIdAsync(userId);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var (success, message, tempPassword) = await _authService.CreateEmployeeAsync(dto);
        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, tempPassword });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : 0;
        var (success, message) = await _authService.DeleteUserAsync(id, currentUserId);
        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}
