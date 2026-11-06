using BidQuiz.Application.Abstractions.Persistence;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Common;
using BidQuiz.Application.Options;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using BidQuiz.Domain.Common;
using Microsoft.Extensions.Options;

namespace BidQuiz.Application.Game.StartQuestion;

public sealed class StartQuestionHandler(
    IUnitOfWorkFactory unitOfWork,
    GameGate gate,
    QuestionDeadline deadline,
    Scoreboard scoreboard,
    IGameNotifier notifier,
    IOptions<GameOptions> options,
    TimeProvider time)
{
    public async Task<Result> HandleAsync(StartQuestion command, CancellationToken cancellationToken)
    {
        using var lease = await gate.EnterAsync(cancellationToken);
        await using var work = await unitOfWork.BeginAsync(cancellationToken);
        var state = await work.Games.GetAsync(cancellationToken);
        var question = await work.Questions.FindAsync(command.QuestionId, cancellationToken);

        var now = time.GetUtcNow().UtcDateTime;
        var endsAt = now.AddSeconds(options.Value.QuestionSeconds);
        var started = state.StartQuestion(question, endsAt);
        if (started.IsFailure)
        {
            return started;
        }

        await work.SaveChangesAsync(cancellationToken);
        deadline.Set(endsAt);

        await notifier.QuestionStartedAsync(QuestionResponse.From(question!), endsAt, now, AfterCommit.Token);
        await notifier.AnswerCountChangedAsync(
            0,
            await scoreboard.CountBiddersAsync(work, AfterCommit.Token),
            AfterCommit.Token);
        return Result.Success();
    }
}
