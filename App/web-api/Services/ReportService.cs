using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.DTOs;

namespace web_api.Services;

public class ReportService
{
    private readonly VoenkomDbContext _context;

    public ReportService(VoenkomDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateSummonsReportAsync(DateTime from, DateTime to)
    {
        var summons = await _context.Summons
            .Include(s => s.PersonalFile)
            .Where(s => s.CreatedAt >= from && s.CreatedAt <= to)
            .OrderBy(s => s.SummonDate)
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,ФИО,Дата повестки,Время,Место,Причина,Статус");
        foreach (var s in summons)
        {
            var fio = s.PersonalFile != null 
                ? $"{s.PersonalFile.LastName} {s.PersonalFile.FirstName} {s.PersonalFile.Patronymic}" 
                : "—";
            csv.AppendLine($"{s.Id},{fio},{s.SummonDate:yyyy-MM-dd},{s.Time},{s.Location},{s.Reason},{s.Status}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> GenerateApplicationsReportAsync(DateTime from, DateTime to)
    {
        var apps = await _context.Applications
            .Include(a => a.PersonalFile)
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,ФИО,Тип,Название,Статус,Дата подачи,Дата рассмотрения");
        foreach (var a in apps)
        {
            var fio = a.PersonalFile != null 
                ? $"{a.PersonalFile.LastName} {a.PersonalFile.FirstName}" 
                : "—";
            csv.AppendLine($"{a.Id},{fio},{a.Type},{a.Title},{a.Status},{a.CreatedAt:yyyy-MM-dd},{a.ReviewedAt:yyyy-MM-dd}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> GenerateEvadersReportAsync()
    {
        var evaders = await _context.Evaders
            .Include(e => e.PersonalFile)
            .Where(e => e.Status == "active")
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,ФИО,№ протокола,Дата протокола,Причина,Статус");
        foreach (var e in evaders)
        {
            var fio = e.PersonalFile != null 
                ? $"{e.PersonalFile.LastName} {e.PersonalFile.FirstName}" 
                : "—";
            csv.AppendLine($"{e.Id},{fio},{e.ProtocolNumber},{e.ProtocolDate:yyyy-MM-dd},{e.Reason},{e.Status}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> GeneratePersonalFilesReportAsync()
    {
        var files = await _context.PersonalFiles
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,ФИО,Дата рождения,Категория годности,Образование,Место работы");
        foreach (var p in files)
        {
            var fio = $"{p.LastName} {p.FirstName} {p.Patronymic}";
            csv.AppendLine($"{p.Id},{fio},{p.BirthDate:yyyy-MM-dd},{p.FitnessCategory},{p.Education},{p.WorkPlace}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }
}
