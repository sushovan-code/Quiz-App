namespace BidQuiz.Api.Domain;

public enum Verdict
{
    Up = 1,
    Down = 2
}

public enum GameStatus
{
    Idle = 0,
    Accepting = 1,
    Closed = 2,
    Revealed = 3
}
