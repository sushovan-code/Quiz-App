using BidQuiz.Server.Data;
using BidQuiz.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BidQuiz.Server.Game;

/// <summary>
/// Owns the game. All state lives in the database so a restart mid-question is safe; only the
/// cached deadline is in memory. Every mutation runs under the game gate.
/// </summary>
public sealed class GameService(
    IDbContextFactory<GameDbContext> dbFactory,
    IGameNotifier notifier,
    GameGate gate,
    Scoreboard scoreboard,
    IScoringPolicy scoring,
    QuestionDeck deck,
    IOptions<GameOptions> gameOptions,
    TimeProvider time) : IGameService
{
    private const int MaxAnswerLength = 500;

    private readonly GameOptions _game = gameOptions.Value;
    private DateTime? _deadline;

    private DateTime UtcNow => time.GetUtcNow().UtcDateTime;

    /// <summary>Restores the deadline after a restart. Call once before serving requests.</summary>
    public async Task InitializeAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.GameStates.SingleAsync();
        _deadline = state.Status == GameStatus.Accepting ? state.EndsAtUtc : null;
    }

    public async Task<TeamSnapshot> GetTeamSnapshotAsync(int teamId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.GameStates.SingleAsync();
        var isOpen = state.Status == GameStatus.Accepting;

        var question = isOpen ? await db.Questions.FindAsync(state.CurrentQuestionId) : null;
        var myBid = isOpen
            ? await db.Bids
                .Where(b => b.TeamId == teamId && b.QuestionId == state.CurrentQuestionId)
                .Select(b => new MyBidDto(b.Answer, b.Amount))
                .SingleOrDefaultAsync()
            : null;

        return new TeamSnapshot(
            teamId,
            state.Status,
            question is null ? null : ToDto(question),
            isOpen ? state.EndsAtUtc : null,
            UtcNow,
            myBid,
            await scoreboard.ReadAsync(db));
    }

    public async Task<AdminSnapshot> GetAdminSnapshotAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.GameStates.SingleAsync();
        var question = await db.Questions.FindAsync(state.CurrentQuestionId);
        var revealed = state.Status == GameStatus.Revealed;

        return new AdminSnapshot(
            state.Status,
            question is null ? null : ToDto(question),
            state.Status == GameStatus.Accepting ? state.EndsAtUtc : null,
            UtcNow,
            await db.Bids.CountAsync(b => b.QuestionId == state.CurrentQuestionId),
            await scoreboard.CountActiveAsync(db),
            await deck.ReadBoardAsync(db),
            await scoreboard.ReadAsync(db),
            revealed ? await GetBidsAsync(db, state.CurrentQuestionId) : null,
            revealed && state.AnswerRevealed ? question?.CorrectAnswer : null);
    }

    public async Task StartQuestionAsync(int questionId)
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var state = await db.GameStates.SingleAsync();
            if (state.Status == GameStatus.Accepting)
                throw new GameRuleException("A question is already running.");

            var question = await db.Questions.FindAsync(questionId)
                ?? throw new GameRuleException("Unknown question.");
            if (question.Order is null)
                throw new GameRuleException("That question is not in this game.");
            if (question.Asked)
                throw new GameRuleException("That question has already been asked.");

            var now = UtcNow;
            var endsAt = now.AddSeconds(_game.QuestionSeconds);
            question.Asked = true;
            state.CurrentQuestionId = question.Id;
            state.EndsAtUtc = endsAt;
            state.Status = GameStatus.Accepting;
            state.AnswerRevealed = false;
            await db.SaveChangesAsync();
            _deadline = endsAt;

            await notifier.QuestionStartedAsync(ToDto(question), endsAt, now);
            await notifier.AnswerCountChangedAsync(0, await scoreboard.CountActiveAsync(db));
        }
    }

    public async Task<MyBidDto> SubmitBidAsync(int teamId, string answer, int amount)
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var state = await db.GameStates.SingleAsync();
            var now = UtcNow;

            // Checks the clock rather than the status: the deadline tick may not have fired yet.
            if (state.Status != GameStatus.Accepting || state.EndsAtUtc is not { } endsAt || now >= endsAt)
                throw new GameRuleException("Time is up. Your bid was not accepted.");

            answer = answer.Trim();
            if (answer.Length == 0)
                throw new GameRuleException("Please enter an answer.");
            if (answer.Length > MaxAnswerLength)
                throw new GameRuleException($"Answers are limited to {MaxAnswerLength} characters.");

            var team = await db.Teams.FindAsync(teamId) ?? throw new GameRuleException("Unknown team.");
            if (scoring.ValidateBid(amount, team.BidBalance) is { } error)
                throw new GameRuleException(error);

            var questionId = state.CurrentQuestionId!.Value;
            var bid = await db.Bids.SingleOrDefaultAsync(b => b.TeamId == teamId && b.QuestionId == questionId);
            var isFirstSubmission = bid is null;
            if (bid is null)
            {
                bid = new Bid { TeamId = teamId, QuestionId = questionId, Answer = answer };
                db.Bids.Add(bid);
            }

            bid.Answer = answer;
            bid.Amount = amount;
            bid.SubmittedUtc = now;
            await db.SaveChangesAsync();

            if (isFirstSubmission)
            {
                await notifier.AnswerCountChangedAsync(
                    await db.Bids.CountAsync(b => b.QuestionId == questionId),
                    await scoreboard.CountActiveAsync(db));
            }

            return new MyBidDto(bid.Answer, bid.Amount);
        }
    }

    /// <summary>Closes the question once its deadline passes. Driven by <see cref="DeadlineService"/>.</summary>
    public async Task TickAsync()
    {
        if (_deadline is not { } deadline || UtcNow < deadline)
            return;

        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var state = await db.GameStates.SingleAsync();
            _deadline = null;
            if (state.Status != GameStatus.Accepting)
                return;

            state.Status = GameStatus.Closed;
            await db.SaveChangesAsync();
            await notifier.TimeUpAsync();
        }
    }

    public async Task RevealBidsAsync()
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var state = await db.GameStates.SingleAsync();
            if (state.Status == GameStatus.Idle)
                throw new GameRuleException("No question has been asked yet.");

            var closedEarly = state.Status == GameStatus.Accepting;
            state.Status = GameStatus.Revealed;
            await db.SaveChangesAsync();
            _deadline = null;

            if (closedEarly)
                await notifier.TimeUpAsync();
            await notifier.BidsRevealedAsync(await GetBidsAsync(db, state.CurrentQuestionId));
        }
    }

    public async Task RevealAnswerAsync()
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var state = await db.GameStates.SingleAsync();
            if (state.Status != GameStatus.Revealed)
                throw new GameRuleException("Reveal the bids first.");

            var question = await db.Questions.FindAsync(state.CurrentQuestionId)
                ?? throw new GameRuleException("Unknown question.");
            state.AnswerRevealed = true;
            await db.SaveChangesAsync();
            await notifier.AnswerRevealedAsync(question.CorrectAnswer);
        }
    }

    public async Task<BidDto> JudgeAsync(int bidId, Verdict verdict)
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var state = await db.GameStates.SingleAsync();
            var bid = await db.Bids.Include(b => b.Team).SingleOrDefaultAsync(b => b.Id == bidId)
                ?? throw new GameRuleException("Unknown bid.");
            if (state.Status != GameStatus.Revealed || bid.QuestionId != state.CurrentQuestionId)
                throw new GameRuleException("That bid is not open for judging.");

            var highest = await db.Bids.Where(b => b.QuestionId == bid.QuestionId).MaxAsync(b => b.Amount);
            var team = bid.Team!;

            // Undo the last verdict by what it really changed, so nothing is double-counted.
            var before = new Balances(team.BidBalance - bid.BalanceDelta, team.Wallet - bid.WalletDelta);
            var after = scoring.Settle(before, verdict, bid.Amount, bid.Amount == highest);

            bid.Verdict = verdict;
            bid.BalanceDelta = after.BidBalance - before.BidBalance;
            bid.WalletDelta = after.Wallet - before.Wallet;
            team.BidBalance = after.BidBalance;
            team.Wallet = after.Wallet;
            await db.SaveChangesAsync();

            await notifier.ScoresUpdatedAsync(await scoreboard.ReadAsync(db));
            return ToDto(bid);
        }
    }

    /// <summary>Clears the scores and deals a fresh set of questions for the next game.</summary>
    public async Task ResetGameAsync()
    {
        using (await gate.EnterAsync())
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var opening = scoring.Opening;
            await db.Bids.ExecuteDeleteAsync();
            await db.Teams.ExecuteUpdateAsync(s => s
                .SetProperty(t => t.BidBalance, opening.BidBalance)
                .SetProperty(t => t.Wallet, opening.Wallet));

            var state = await db.GameStates.SingleAsync();
            state.CurrentQuestionId = null;
            state.EndsAtUtc = null;
            state.Status = GameStatus.Idle;
            state.AnswerRevealed = false;
            await deck.DealAsync(db, state.QuestionsPerGame);
            await db.SaveChangesAsync();
            _deadline = null;

            await notifier.GameResetAsync();
            await notifier.QuestionBoardChangedAsync(await deck.ReadBoardAsync(db));
            await notifier.ScoresUpdatedAsync(await scoreboard.ReadAsync(db));
        }
    }

    private static async Task<IReadOnlyList<BidDto>> GetBidsAsync(GameDbContext db, int? questionId)
    {
        var bids = await db.Bids.AsNoTracking()
            .Include(b => b.Team)
            .Where(b => b.QuestionId == questionId)
            .ToListAsync();
        return bids.OrderByDescending(b => b.Amount).ThenBy(b => b.SubmittedUtc).Select(ToDto).ToList();
    }

    private static QuestionDto ToDto(Question q) => new(q.Id, q.Order ?? 0, q.Category, q.Text, q.ImageUrl);

    private static BidDto ToDto(Bid b) =>
        new(b.Id, b.TeamId, b.Team!.Name, b.Team.Color, b.Answer, b.Amount, b.Verdict);
}
