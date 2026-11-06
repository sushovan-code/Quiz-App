using BidQuiz.Api.Domain;
using Microsoft.AspNetCore.SignalR;

namespace BidQuiz.Api.Hubs;

/// <summary>SignalR masks ordinary exceptions, so rule messages must leave as a HubException.</summary>
public sealed class GameRuleHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (GameRuleException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
