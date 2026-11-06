namespace BidQuiz.Api.Game;

/// <summary>Lets one change to the game run at a time, across every service that writes.</summary>
public sealed class GameGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> EnterAsync()
    {
        await _semaphore.WaitAsync();
        return new Release(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Release(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
