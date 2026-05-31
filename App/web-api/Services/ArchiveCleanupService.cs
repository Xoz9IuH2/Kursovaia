using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web_api.Services;

public class ArchiveCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ArchiveCleanupService> _logger;

    public ArchiveCleanupService(IServiceScopeFactory scopeFactory, ILogger<ArchiveCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PersonalFileService>();
                var deleted = await service.DeleteExpiredArchivesAsync();
                
                if (deleted > 0)
                    _logger.LogInformation("Удалено {Count} архивных дел", deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке архивов");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}