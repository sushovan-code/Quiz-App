using BidQuiz.Application.Game.CloseExpiredQuestion;

namespace BidQuiz.Api.Background;

/// <summary>Checks the open question's deadline a few times a second and closes it when time is up.</summary>
public sealed partial class QuestionDeadlineWorker(
    CloseExpiredQuestionHandler handler,
    ILogger<QuestionDeadlineWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        try
        {
            await handler.HandleAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // One failed tick must not stop the timer; the next tick retries.
            LogTickFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Deadline tick failed.")]
    private static partial void LogTickFailed(ILogger logger, Exception exception);
}
