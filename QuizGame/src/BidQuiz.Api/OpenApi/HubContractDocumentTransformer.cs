using BidQuiz.Application.Auth;
using BidQuiz.Application.Bids;
using BidQuiz.Application.Questions;
using BidQuiz.Application.Scores;
using BidQuiz.Application.Setup;
using BidQuiz.Application.Snapshots;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BidQuiz.Api.OpenApi;

/// <summary>
/// Adds the types the SignalR hub sends and receives, so the client generates them from the same
/// document as the HTTP types. The hub itself isn't describable in OpenAPI.
/// </summary>
public sealed class HubContractDocumentTransformer : IOpenApiDocumentTransformer
{
    private static readonly Type[] HubContract =
    [
        typeof(TeamSessionResponse),
        typeof(AdminSessionResponse),
        typeof(MyBidResponse),
        typeof(BidResponse),
        typeof(QuestionResponse),
        typeof(QuestionSummaryResponse),
        typeof(TeamScoreResponse),
        typeof(TeamSnapshotResponse),
        typeof(AdminSnapshotResponse),
        typeof(SetupResponse),
        typeof(TeamSetupResponse),
        typeof(QuestionSetupResponse),
        typeof(SaveTeamRequest),
        typeof(SaveQuestionRequest),
    ];

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var type in HubContract)
        {
            var schema = await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
            document.AddComponent(type.Name, schema);
        }
    }
}
