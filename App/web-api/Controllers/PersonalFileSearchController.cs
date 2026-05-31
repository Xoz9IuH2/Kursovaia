using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_api.Data;

namespace web_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonalFileSearchController : ControllerBase
{
    private readonly VoenkomDbContext _context;

    public PersonalFileSearchController(VoenkomDbContext context)
    {
        _context = context;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q) || q.Length < 2)
            return Ok(new List<object>());

        var query = q.ToUpper().Replace(" ", "").Replace("№", "");

        var files = await _context.PersonalFiles
            .Where(p => p.Status == "active")
            .ToListAsync();

        var filtered = files
            .Where(p => {
                var searchStr = $"{p.MilitaryTicketSeries ?? ""}{p.MilitaryTicketNumber ?? ""}{p.LastName ?? ""}{p.FirstName ?? ""}".ToUpper().Replace(" ", "");
                return searchStr.Contains(query);
            })
            .Take(10)
            .Select(p => new
            {
                id = p.Id,
                lastName = p.LastName,
                firstName = p.FirstName,
                patronymic = p.Patronymic,
                birthDate = p.BirthDate.ToString("dd.MM.yyyy"),
                birthPlace = p.BirthPlace,
                address = p.Address,
                phone = p.Phone,
                email = p.Email,
                passportSeries = p.PassportSeries,
                passportNumber = p.PassportNumber,
                militaryTicketSeries = p.MilitaryTicketSeries,
                militaryTicketNumber = p.MilitaryTicketNumber,
                militaryRank = p.MilitaryRank,
                fitnessCategory = p.FitnessCategory,
                status = p.Status
            })
            .ToList();

        return Ok(filtered);
    }
}