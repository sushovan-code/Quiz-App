namespace BidQuiz.Application.Common;

/// <summary>
/// The open question's deadline, cached so the timer can check it without reading the database
/// every tick. The database stays the source of truth; this is restored from it at startup.
/// </summary>
public sealed class QuestionDeadline
{
    private readonly Lock _lock = new();
    private DateTime? _endsAtUtc;

    public DateTime? EndsAtUtc
    {
        get
        {
            lock (_lock)
            {
                return _endsAtUtc;
            }
        }
    }

    public void Set(DateTime? endsAtUtc)
    {
        lock (_lock)
        {
            _endsAtUtc = endsAtUtc;
        }
    }

    public void Clear() => Set(null);
}
