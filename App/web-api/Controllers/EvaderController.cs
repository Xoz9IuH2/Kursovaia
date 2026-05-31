using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "employee")]
public class EvaderController : ControllerBase
{
    private readonly EvaderService _service;

    public EvaderController(EvaderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var evaders = await _service.GetAllAsync(status);
        return Ok(evaders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var evader = await _service.GetByIdAsync(id);
        return evader == null ? NotFound() : Ok(evader);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEvaderDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _service.CreateAsync(userId, dto.PersonalFileId, dto.Description, dto.Reason);
        return result.Success ? Ok(new { message = result.Message, evader = result.Evader }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(int id, [FromBody] CloseEvaderDto dto)
    {
        var result = await _service.CloseAsync(id, dto.Resolution);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }
}
