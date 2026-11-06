using BidQuiz.Domain.Scoring;

namespace BidQuiz.Domain.Bids;

/// <summary>A team's answer and bid on one question. A team has at most one bid per question.</summary>
public sealed class Bid
{
    private Bid()
    {
    }

    public int Id { get; private set; }

    public int TeamId { get; private set; }

    public int QuestionId { get; private set; }

    public string Answer { get; private set; } = "";

    public int Amount { get; private set; }

    public DateTime SubmittedUtc { get; private set; }

    public Verdict? Verdict { get; private set; }

    /// <summary>
    /// What the verdict actually changed. Balances stop at zero, so the bid alone can't tell how
    /// much to give back when the host changes a verdict.
    /// </summary>
    public int BalanceDelta { get; private set; }

    public int WalletDelta { get; private set; }

    public static Bid Place(int teamId, int questionId, string answer, int amount, DateTime submittedUtc) => new()
    {
        TeamId = teamId,
        QuestionId = questionId,
        Answer = answer,
        Amount = amount,
        SubmittedUtc = submittedUtc
    };

    /// <summary>A resubmission: the last one wins.</summary>
    public void Revise(string answer, int amount, DateTime submittedUtc)
    {
        Answer = answer;
        Amount = amount;
        SubmittedUtc = submittedUtc;
    }

    /// <summary>The team's balances as if this bid had never been judged, so nothing is double-counted.</summary>
    public Balances BalancesBeforeVerdict(Balances current) =>
        new(current.BidBalance - BalanceDelta, current.Wallet - WalletDelta);

    public void RecordVerdict(Verdict verdict, Balances before, Balances after)
    {
        Verdict = verdict;
        BalanceDelta = after.BidBalance - before.BidBalance;
        WalletDelta = after.Wallet - before.Wallet;
    }
}
