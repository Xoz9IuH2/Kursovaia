using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonalFileController : ControllerBase
{
    private readonly PersonalFileService _service;

    public PersonalFileController(PersonalFileService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _service.GetAllAsync(search, status, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var file = await _service.GetByIdAsync(id);
        return file == null ? NotFound() : Ok(file);
    }

    [HttpGet("my")]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _service.GetByUserIdAsync(userId);
        return file == null ? NotFound() : Ok(file);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePersonalFileDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.Success ? Ok(new { message = result.Message, file = result.File }) : BadRequest(new { message = result.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePersonalFileDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.Success ? Ok(new { message = result.Message, file = result.File }) : BadRequest(new { message = result.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var result = await _service.ArchiveAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }
}
