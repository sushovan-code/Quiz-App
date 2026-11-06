namespace BidQuiz.Domain.Common;

/// <summary>What sort of expected failure an <see cref="Error"/> is, so each boundary can map it (Problem Details, hub message).</summary>
public enum ErrorKind
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4
}
