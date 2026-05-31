using Microsoft.EntityFrameworkCore;
using web_api.Data;
using web_api.Models;

namespace web_api.Services;

public class SummonAutoService : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<SummonAutoService> _logger;

    public SummonAutoService(IServiceProvider provider, ILogger<SummonAutoService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto summon service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<VoenkomDbContext>();
                
                var today = DateTime.UtcNow.Date;
                var turning18 = today.AddYears(-18);
                
                var newRecruits = await context.PersonalFiles
                    .Where(p => p.Status == "active")
                    .Where(p => p.BirthDate.Date == turning18)
                    .ToListAsync(stoppingToken);

                var created = 0;
                foreach (var file in newRecruits)
                {
                    var hasExisting = await context.Summons
                        .AnyAsync(s => s.PersonalFileId == file.Id && s.Reason == "Первичный призыв", stoppingToken);
                    
                    if (!hasExisting)
                    {
                        var summon = new Summon
                        {
                            PersonalFileId = file.Id,
                            Title = "Повестка на призыв",
                            SummonDate = DateTime.UtcNow,
                            Reason = "Первичный призыв",
                            Description = "Автоповестка: исполнилось 18 лет",
                            Status = "pending",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        context.Summons.Add(summon);
                        created++;
                        _logger.LogInformation("Auto summon created for " + file.LastName);
                    }
                }
                
                if (created > 0)
                {
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation(created + " auto summons created");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto summon error");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}