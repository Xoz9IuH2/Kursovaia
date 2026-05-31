using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationController : ControllerBase
{
    private readonly ApplicationService _service;
    private readonly PersonalFileService _personalFileService;

    public ApplicationController(ApplicationService service, PersonalFileService personalFileService)
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
        var app = await _service.GetByIdAsync(id);
        return app == null ? NotFound() : Ok(app);
    }

    [HttpGet("my")]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return NotFound();

        var apps = await _service.GetByPersonalFileIdAsync(file.Id);
        return Ok(apps);
    }

    [HttpPost]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return BadRequest(new { message = "Личное дело не найдено" });

        var result = await _service.CreateAsync(userId, file.Id, dto);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/review")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Review(int id, [FromBody] ApplicationReviewDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _service.ReviewAsync(userId, id, dto);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpGet("{id}/history")]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return NotFound();

        var history = await _service.GetHistoryAsync(file.Id, id);
        return Ok(history);
    }
}
