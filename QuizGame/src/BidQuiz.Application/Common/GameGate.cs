namespace BidQuiz.Application.Common;

/// <summary>Lets one change to the game run at a time, across every use case that writes.</summary>
public sealed class GameGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Release(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Release(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
