using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace BidQuiz.IntegrationTests.Health;

/// <summary>
/// The Aspire AppHost waits on /health before it starts the web client. The endpoints are only
/// mapped in Development, so a deployed show doesn't expose them.
/// </summary>
public sealed class HealthEndpointTests : IAsyncLifetime
{
    private const string Healthy = "Healthy";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"bidquiz-{Guid.NewGuid():N}.db");
    private readonly List<WebApplicationFactory<Program>> _factories = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }

        File.Delete(_dbPath);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task HealthEndpoint_InDevelopment_ReportsHealthy(string path)
    {
        using var http = CreateClient(Environments.Development);

        using var response = await http.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Healthy, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task HealthEndpoint_InProduction_IsNotMapped(string path)
    {
        using var http = CreateClient(Environments.Production);

        using var response = await http.GetAsync(path);

        // The SPA fallback serves index.html for unknown paths, so "not mapped" means no health report.
        Assert.NotEqual(Healthy, await response.Content.ReadAsStringAsync());
    }

    private HttpClient CreateClient(string environment)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            host.UseSetting("ConnectionStrings:Game", $"Data Source={_dbPath};Pooling=False");
            host.UseSetting("Game:AdminPassword", "test-admin");
        });
        _factories.Add(factory);
        return factory.CreateClient();
    }
}
