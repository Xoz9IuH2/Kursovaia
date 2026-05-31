using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.Models;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly VoenkomDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(VoenkomDbContext context, IWebHostEnvironment env, ILogger<DocumentController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.PersonalFileId == null)
            return Ok(new List<object>());

        var docs = await _context.Documents
            .Where(d => d.PersonalFileId == user.PersonalFileId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                id = d.Id,
                fileName = d.FileName,
                fileType = d.FileType,
                documentType = d.DocumentType,
                status = d.Status,
                rejectionReason = d.RejectionReason,
                createdAt = d.CreatedAt.ToString("dd.MM.yyyy")
            })
            .ToListAsync();

        return Ok(new { items = docs });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string documentType = "other")
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.PersonalFileId == null)
            return BadRequest(new { message = "Личное дело не найдено" });

        if (file.Length == 0)
            return BadRequest(new { message = "Файл пустой" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "Файл слишком большой (макс. 10 МБ)" });

        var ext = System.IO.Path.GetExtension(file.FileName).ToLower();
        if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            return BadRequest(new { message = "Недопустимый формат" });

        var uploadsPath = System.IO.Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", user.PersonalFileId.ToString()!);
        System.IO.Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = System.IO.Path.Combine(uploadsPath, fileName);

        using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var doc = new Document
        {
            PersonalFileId = user.PersonalFileId.Value,
            FileName = file.FileName,
            FilePath = $"/uploads/{user.PersonalFileId}/{fileName}",
            FileType = ext.TrimStart('.'),
            FileSize = file.Length,
            DocumentType = documentType,
            Status = "pending",
            UploadedById = userId
        };

        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = doc.Id,
            fileName = doc.FileName,
            fileType = doc.FileType,
            status = doc.Status,
            createdAt = doc.CreatedAt.ToString("dd.MM.yyyy")
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        var doc = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.PersonalFileId == user!.PersonalFileId);

        if (doc == null)
            return NotFound(new { message = "Документ не найден" });

        return Ok(new
        {
            id = doc.Id,
            fileName = doc.FileName,
            filePath = doc.FilePath,
            fileType = doc.FileType,
            documentType = doc.DocumentType,
            status = doc.Status,
            rejectionReason = doc.RejectionReason,
            createdAt = doc.CreatedAt.ToString("dd.MM.yyyy")
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        var doc = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.PersonalFileId == user!.PersonalFileId);

        if (doc == null)
            return NotFound(new { message = "Документ не найден" });

        if (doc.Status == "approved")
            return BadRequest(new { message = "Нельзя удалить одобренный документ" });

        var fullPath = System.IO.Path.Combine(_env.WebRootPath ?? "wwwroot", doc.FilePath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Документ удалён" });
    }
}