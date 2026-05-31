using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Services;

namespace web_api.Controllers;

[ApiController]
[Route("api/summons")]
public class SummonController : ControllerBase
{
    private readonly SummonService _service;
    private readonly PersonalFileService _personalFileService;
    private readonly VoenkomDbContext _context;

    public SummonController(SummonService service, PersonalFileService personalFileService, VoenkomDbContext context)
    {
        _service = service;
        _personalFileService = personalFileService;
        _context = context;
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
        var summon = await _service.GetByIdAsync(id);
        return summon == null ? NotFound() : Ok(summon);
    }

    [HttpGet("my")]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return NotFound();
        
        var summons = await _service.GetByPersonalFileIdAsync(file.Id);
        return Ok(summons);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSummonDto dto)
    {
        if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) || userId == 0)
        {
            var defaultUser = await _context.Users.Where(u => u.Role == "employee" || u.Role == "admin").FirstOrDefaultAsync();
            userId = defaultUser?.Id ?? 0;
        }
        var result = await _service.CreateAsync(userId, dto);
        return result.Success ? Ok(new { message = result.Message, summon = result.Summon }) : BadRequest(new { message = result.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSummonDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.Success ? Ok(new { message = result.Message, summon = result.Summon }) : BadRequest(new { message = result.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/read")]
    [Authorize(Roles = "citizen")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var file = await _personalFileService.GetByUserIdAsync(userId);
        if (file == null) return NotFound();

        var result = await _service.MarkAsReadAsync(file.Id, id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/arrived")]
    public async Task<IActionResult> MarkArrived(int id)
    {
        var result = await _service.MarkArrivedAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }
}
