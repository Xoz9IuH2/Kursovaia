using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly AppointmentService _service;
    private readonly PersonalFileService _personalFileService;

    public AppointmentController(AppointmentService service, PersonalFileService personalFileService)
    {
        _service = service;
        _personalFileService = personalFileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _service.GetAllAsync(status, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var appointment = await _service.GetByIdAsync(id);
        return appointment == null ? NotFound() : Ok(appointment);
    }

    [HttpGet("my")]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return NotFound();

        var appointments = await _service.GetByPersonalFileIdAsync(file.Id);
        return Ok(appointments);
    }

    [HttpPost]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return BadRequest(new { message = "Личное дело не найдено" });

        var result = await _service.CreateAsync(file.Id, dto);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _service.CompleteAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _service.CancelAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }
}
