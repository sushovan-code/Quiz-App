namespace BidQuiz.Domain.Common;

/// <summary>The outcome of an operation that can fail in an expected way. Exceptions are kept for faults.</summary>
public class Result
{
    private static readonly Result SuccessResult = new(null);

    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => SuccessResult;

    public static Result Failure(Error error) => new(error);

    public static Result<T> Success<T>(T value) => new(value);

    public static Result<T> Failure<T>(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}
