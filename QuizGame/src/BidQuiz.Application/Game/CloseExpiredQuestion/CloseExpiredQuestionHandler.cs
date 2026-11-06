using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;

namespace BidQuiz.Application.Game.CloseExpiredQuestion;

/// <summary>Closes the open question once its deadline passes. Driven by a timer every fraction of a second.</summary>
public sealed class CloseExpiredQuestionHandler(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    QuestionDeadline deadline,
    IGameNotifier notifier,
    TimeProvider time)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (deadline.EndsAtUtc is not { } endsAt || time.GetUtcNow().UtcDateTime < endsAt)
        {
            return;
        }

        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        deadline.Clear();
        if (!state.Close())
        {
            return;
        }

        await work.SaveChangesAsync(cancellationToken);
        await notifier.TimeUpAsync(AfterCommit.Token);
    }
}
