// Runs the show locally with the Aspire dashboard: the API, and the Angular dev server proxying to it.
// `dotnet run --project src/BidQuiz.Api` still works on its own for a LAN show.
const int webDevServerPort = 4200;

var builder = DistributedApplication.CreateBuilder(args);

// The default "show" profile binds every interface for phones; "aspire" keeps the API on localhost.
var api = builder.AddProject<Projects.BidQuiz_Api>("api", launchProfileName: "aspire")
    .WithHttpHealthCheck("/health");

builder.AddJavaScriptApp("web", "../web/bidquiz-web", "start:aspire")
    .WithReference(api)
    .WaitFor(api)
    // Unproxied on the usual Angular port: the browser reaches the dev server directly, as with `npm start`.
    .WithHttpEndpoint(port: webDevServerPort, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
