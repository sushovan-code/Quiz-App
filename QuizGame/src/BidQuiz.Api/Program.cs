using System.Text.Json.Serialization;
using BidQuiz.Api.Background;
using BidQuiz.Api.Endpoints;
using BidQuiz.Api.Hubs;
using BidQuiz.Api.Network;
using BidQuiz.Api.Realtime;
using BidQuiz.Application;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Game.InitializeGame;
using BidQuiz.Application.Options;
using BidQuiz.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection(ScoringOptions.SectionName));
builder.Services.Configure<GameOptions>(builder.Configuration.GetSection(GameOptions.SectionName));
builder.Services.Configure<NetworkOptions>(builder.Configuration.GetSection(NetworkOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();
builder.Services.AddHostedService<QuestionDeadlineWorker>();

builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var binding = ShowBinding.Configure(builder);

var app = builder.Build();

await app.Services.GetRequiredService<InitializeGameHandler>().HandleAsync(app.Lifetime.ApplicationStopping);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapClientEndpoints();
app.MapHub<GameHub>(GameHub.Path);
app.MapFallbackToFile("index.html");

binding.PrintUrlsOnStart(app);

await app.RunAsync();

/// <summary>Lets the integration tests start the real app.</summary>
public partial class Program;
