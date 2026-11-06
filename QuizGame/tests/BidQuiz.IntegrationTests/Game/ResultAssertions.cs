using BidQuiz.Domain.Common;

namespace BidQuiz.IntegrationTests.Game;

/// <summary>Awaits a use case and asserts how it ended.</summary>
internal static class ResultAssertions
{
    public static async Task SucceedsAsync(this Task<Result> call)
    {
        var result = await call;
        Assert.True(result.IsSuccess, result.Error?.Message);
    }

    public static async Task<T> SucceedsAsync<T>(this Task<Result<T>> call)
    {
        var result = await call;
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }

    public static async Task<Error> FailsAsync(this Task<Result> call)
    {
        var result = await call;
        Assert.True(result.IsFailure, "Expected the use case to be rejected.");
        return result.Error!;
    }

    public static async Task<Error> FailsAsync<T>(this Task<Result<T>> call)
    {
        var result = await call;
        Assert.True(result.IsFailure, "Expected the use case to be rejected.");
        return result.Error!;
    }
}
