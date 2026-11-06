using System.Text.Json.Serialization;
using BidQuiz.Server;
using BidQuiz.Server.Data;
using BidQuiz.Server.Domain;
using BidQuiz.Server.Game;
using BidQuiz.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection("Scoring"));
builder.Services.Configure<GameOptions>(builder.Configuration.GetSection("Game"));

// Swap UseSqlite for UseSqlServer here to change database.
builder.Services.AddDbContextFactory<GameDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("Game")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GameGate>();
builder.Services.AddSingleton<Scoreboard>();
builder.Services.AddSingleton<QuestionDeck>();
builder.Services.AddSingleton<IScoringPolicy, HighestBidScoringPolicy>();
builder.Services.AddSingleton<IQuestionPicker, RandomQuestionPicker>();
builder.Services.AddSingleton<IPresence, PresenceTracker>();
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<ISetupService, SetupService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddHostedService<DeadlineService>();

builder.Services.AddSignalR(options => options.AddFilter<GameRuleHubFilter>())
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// App Service and explicit ASPNETCORE_URLS keep their own binding; locally, listen on the LAN.
var port = builder.Configuration.GetValue("Game:Port", 5000);
var hasExplicitBinding = !string.IsNullOrEmpty(builder.Configuration["urls"])
    || !string.IsNullOrEmpty(builder.Configuration["http_ports"])
    || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
if (!hasExplicitBinding)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

await Seeder.InitializeAsync(app.Services);
await app.Services.GetRequiredService<IGameService>().InitializeAsync();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/teams", (ISetupService setup) => setup.ListTeamsAsync());

app.MapGet("/api/config", (HttpContext http, IOptions<GameOptions> game, IOptions<ScoringOptions> scoring) =>
    new ClientConfig(JoinUrl(http.Request, game.Value), scoring.Value.BidStep, game.Value.QuestionSeconds));

app.MapHub<GameHub>("/hubs/game");
app.MapFallbackToFile("index.html");

if (!hasExplicitBinding)
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var lan = LanAddress.Find();
        Console.WriteLine();
        Console.WriteLine($"  Host screen:  http://localhost:{port}/host");
        Console.WriteLine(lan is null
            ? "  LAN address not found. Is this machine on the venue Wi-Fi?"
            : $"  Team join:    http://{lan}:{port}/play");
        Console.WriteLine();
    });
}

app.Run();

static string JoinUrl(HttpRequest request, GameOptions game)
{
    var baseUrl = game.PublicBaseUrl.TrimEnd('/');
    if (baseUrl.Length == 0)
    {
        // A host opened on localhost must still hand phones an address they can reach.
        var isLocal = request.Host.Host is "localhost" or "127.0.0.1" or "[::1]";
        baseUrl = isLocal && LanAddress.Find() is { } lan
            ? $"{request.Scheme}://{lan}:{request.Host.Port ?? game.Port}"
            : $"{request.Scheme}://{request.Host}";
    }

    return $"{baseUrl}/play";
}

public partial class Program;
