namespace BidQuiz.Domain.Common;

/// <summary>A <see cref="Result"/> that carries a value when it succeeds.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T value)
        : base(null) => _value = value;

    internal Result(Error error)
        : base(error)
    {
    }

    /// <summary>The value. Only read it after checking <see cref="Result.IsSuccess"/>.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value: {Error!.Code}.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
