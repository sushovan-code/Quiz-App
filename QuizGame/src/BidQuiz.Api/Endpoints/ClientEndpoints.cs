using BidQuiz.Api.Network;
using BidQuiz.Application.Options;
using BidQuiz.Application.Setup.ListTeamOptions;
using Microsoft.Extensions.Options;

namespace BidQuiz.Api.Endpoints;

/// <summary>The two HTTP endpoints. Everything else goes over the hub.</summary>
public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/teams", (ListTeamOptionsHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(cancellationToken));

        app.MapGet("/api/config", (
            HttpContext http,
            IOptions<NetworkOptions> network,
            IOptions<GameOptions> game,
            IOptions<ScoringOptions> scoring) =>
            new ClientConfigResponse(
                JoinUrl.For(http.Request, network.Value),
                scoring.Value.BidStep,
                game.Value.QuestionSeconds));

        return app;
    }
}
