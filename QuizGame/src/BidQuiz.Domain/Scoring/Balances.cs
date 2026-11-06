namespace BidQuiz.Domain.Scoring;

/// <summary>A team's two amounts: what it can still bid, and what it has won.</summary>
public readonly record struct Balances(int BidBalance, int Wallet);
