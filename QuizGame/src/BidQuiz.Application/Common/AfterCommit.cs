namespace BidQuiz.Application.Common;

/// <summary>
/// Once a change is saved, every client must hear about it even if the caller has disconnected,
/// so the work after a commit deliberately ignores the caller's cancellation.
/// </summary>
public static class AfterCommit
{
    public static readonly CancellationToken Token = CancellationToken.None;
}
