using BidQuiz.Domain.Common;
using Microsoft.AspNetCore.SignalR;

namespace BidQuiz.Api.Hubs;

/// <summary>
/// Maps a failed result to a HubException, the one exception type whose message SignalR passes on
/// to the client. Rule messages are written to be shown to players and the host.
/// </summary>
public static class HubResults
{
    public static void ThrowIfFailed(this Result result)
    {
        if (result.Error is { } error)
        {
            throw new HubException(error.Message);
        }
    }

    public static T ValueOrThrow<T>(this Result<T> result)
    {
        result.ThrowIfFailed();
        return result.Value;
    }

    public static HubException ToHubException(this Error error) => new(error.Message);
}
