namespace BidQuiz.Server.Game;

public sealed class DeadlineService(IGameService game, ILogger<DeadlineService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await game.TickAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Deadline tick failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}
