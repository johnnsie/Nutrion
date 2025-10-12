namespace Nutrion.Orchestrator;

public sealed class DebugWorker : BackgroundService
{
    private readonly ILogger<DebugWorker> _logger;

    public DebugWorker(ILogger<DebugWorker> logger)
    {
        _logger = logger;
        _logger.LogInformation("DebugWorker constructed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DebugWorker started at {Time}", DateTimeOffset.Now);

        var tick = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                tick++;
                _logger.LogInformation("DebugWorker tick {Tick} at {Time}", tick, DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal during shutdown
        }
        finally
        {
            _logger.LogInformation("DebugWorker stopping at {Time}", DateTimeOffset.Now);
        }
    }
}