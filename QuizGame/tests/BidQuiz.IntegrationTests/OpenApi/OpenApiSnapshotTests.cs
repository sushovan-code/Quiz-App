using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BidQuiz.IntegrationTests.OpenApi;

/// <summary>
/// The web client's types are generated from src/web/bidquiz-web/openapi.json. This fails when the
/// server's contract changes and that snapshot wasn't updated. To update it, run this test with
/// UPDATE_OPENAPI_SNAPSHOT=1, then run <c>npm run generate:api</c> in the web project.
/// </summary>
public sealed class OpenApiSnapshotTests : IAsyncLifetime
{
    private const string UpdateVariable = "UPDATE_OPENAPI_SNAPSHOT";
    private const string DocumentPath = "/openapi/v1.json";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"bidquiz-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("ConnectionStrings:Game", $"Data Source={_dbPath};Pooling=False");
            host.UseSetting("Game:AdminPassword", "test-admin");
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        File.Delete(_dbPath);
    }

    [Fact]
    public async Task OpenApiDocument_MatchesTheClientSnapshot()
    {
        using var http = _factory.CreateClient();
        var actual = WithoutServers(JsonNode.Parse(await http.GetStringAsync(DocumentPath)));
        var snapshotPath = SnapshotPath();

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(snapshotPath, actual!.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n");
        }

        var expected = WithoutServers(JsonNode.Parse(await File.ReadAllTextAsync(snapshotPath)));
        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"openapi.json is out of date. Run the tests with {UpdateVariable}=1, then npm run generate:api.");
    }

    /// <summary>The server URL depends on where the app runs, so it isn't part of the contract.</summary>
    private static JsonNode? WithoutServers(JsonNode? document)
    {
        document?.AsObject().Remove("servers");
        return document;
    }

    private static string SnapshotPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BidQuiz.slnx")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException("BidQuiz.slnx not found above the test output folder.")
            : Path.Combine(directory.FullName, "src", "web", "bidquiz-web", "openapi.json");
    }
}
