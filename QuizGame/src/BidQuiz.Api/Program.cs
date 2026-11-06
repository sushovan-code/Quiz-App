using System.Text.Json.Serialization;
using BidQuiz.Api;
using BidQuiz.Api.Background;
using BidQuiz.Api.Endpoints;
using BidQuiz.Api.Hubs;
using BidQuiz.Api.Network;
using BidQuiz.Api.OpenApi;
using BidQuiz.Api.Realtime;
using BidQuiz.Application;
using BidQuiz.Application.Abstractions.Realtime;
using BidQuiz.Application.Game.InitializeGame;
using BidQuiz.Application.Options;
using BidQuiz.Infrastructure;
using BidQuiz.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Bad settings stop the app at startup rather than surfacing mid-show.
builder.Services.AddValidatedOptions<ScoringOptions>(ScoringOptions.SectionName);
builder.Services.AddValidatedOptions<GameOptions>(GameOptions.SectionName);
builder.Services.AddValidatedOptions<NetworkOptions>(NetworkOptions.SectionName);
builder.Services.AddValidatedOptions<DatabaseOptions>(DatabaseOptions.SectionName);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();
builder.Services.AddHostedService<QuestionDeadlineWorker>();

// HTTP and hub JSON agree: enums as names, numbers as numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<HubContractDocumentTransformer>();
    options.AddSchemaTransformer<AllPropertiesRequiredSchemaTransformer>();
});

builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var binding = ShowBinding.Configure(builder);

var app = builder.Build();

await app.Services.GetRequiredService<InitializeGameHandler>().HandleAsync(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapClientEndpoints();
app.MapHub<GameHub>(GameHub.Path);
app.MapDefaultEndpoints();
app.MapFallbackToFile("index.html");

binding.LogUrlsOnStart(app);

await app.RunAsync();

/// <summary>Lets the integration tests start the real app.</summary>
public partial class Program;
