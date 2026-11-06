namespace BidQuiz.Domain.Common;

/// <summary>An expected failure. <see cref="Message"/> is safe to show to the player or host.</summary>
public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorKind.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorKind.Unauthorized);
}
