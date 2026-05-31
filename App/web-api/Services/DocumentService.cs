using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;
using web_api.Models;

namespace web_api.Services;

public class DocumentService
{
    private readonly VoenkomDbContext _context;

    public DocumentService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<List<DocumentDto>> GetByPersonalFileIdAsync(int personalFileId)
    {
        return await _context.Documents
            .Where(d => d.PersonalFileId == personalFileId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => MapToDto(d))
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, DocumentDto? Document)> UploadAsync(int userId, int personalFileId, IFormFile file, string documentType)
    {
        if (file.Length > 10 * 1024 * 1024)
            return (false, "Размер файла не должен превышать 10 МБ", null);

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var extension = Path.GetExtension(file.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
            return (false, "Недопустимый формат файла. Разрешены: PDF, JPG, PNG, DOC, DOCX", null);

        var uploadsFolder = Path.Combine("uploads", "documents", personalFileId.ToString());
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var document = new Document
        {
            PersonalFileId = personalFileId,
            FileName = file.FileName,
            FilePath = $"/uploads/documents/{personalFileId}/{uniqueFileName}",
            FileType = extension.TrimStart('.'),
            FileSize = file.Length,
            DocumentType = documentType,
            UploadedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return (true, "Документ успешно загружен", MapToDto(document));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int userId, int personalFileId, int documentId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.PersonalFileId == personalFileId);

        if (document == null)
            return (false, "Документ не найден");

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "documents",
            personalFileId.ToString(), Path.GetFileName(document.FilePath));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return (true, "Документ удалён");
    }

    private DocumentDto MapToDto(Document d) => new()
    {
        Id = d.Id,
        FileName = d.FileName,
        FilePath = d.FilePath,
        FileType = d.FileType,
        FileSize = d.FileSize,
        DocumentType = d.DocumentType,
        CreatedAt = d.CreatedAt
    };
}
