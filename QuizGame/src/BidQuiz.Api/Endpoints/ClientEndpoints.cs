using BidQuiz.Api.Network;
using BidQuiz.Application.Options;
using BidQuiz.Application.Setup;
using BidQuiz.Application.Setup.ListTeamOptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace BidQuiz.Api.Endpoints;

/// <summary>The HTTP API, version 1. Everything else goes over the hub.</summary>
public static class ClientEndpoints
{
    public const string BasePath = "/api/v1";

    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup(BasePath).WithTags("Client");

        api.MapGet("/teams", ListTeamsAsync)
            .WithName("ListTeams")
            .WithSummary("The teams a device can log in as.");

        api.MapGet("/config", GetConfig)
            .WithName("GetConfig")
            .WithSummary("Where phones join, the bid step and the time per question.");

        // Without this, an unknown API path would fall through to the SPA and return HTML.
        app.Map("/api/{**path}", () => TypedResults.NotFound()).ExcludeFromDescription();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<TeamOptionResponse>>> ListTeamsAsync(
        ListTeamOptionsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));

    private static Ok<ClientConfigResponse> GetConfig(
        HttpContext http,
        IOptions<NetworkOptions> network,
        IOptions<GameOptions> game,
        IOptions<ScoringOptions> scoring) =>
        TypedResults.Ok(new ClientConfigResponse(
            JoinUrl.For(http.Request, network.Value),
            scoring.Value.BidStep,
            game.Value.QuestionSeconds));
}
