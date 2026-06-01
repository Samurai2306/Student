namespace ProductionSystem.Services;

/// <summary>
/// Периодически синхронизирует прогресс активных заказов с плановым временем производства.
/// </summary>
public sealed class WorkOrderProgressBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkOrderProgressBackgroundService> _logger;

    public WorkOrderProgressBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkOrderProgressBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var production = scope.ServiceProvider.GetRequiredService<ProductionService>();
                await production.SyncInProgressOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка автообновления прогресса заказов");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
