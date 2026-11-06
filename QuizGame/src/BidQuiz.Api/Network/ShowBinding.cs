using System.Net;

namespace BidQuiz.Api.Network;

/// <summary>
/// Locally the show listens on every interface so phones on the venue Wi-Fi can join. App Service,
/// Aspire and an explicit ASPNETCORE_URLS keep their own binding.
/// </summary>
public sealed partial class ShowBinding
{
    private readonly int _port;
    private readonly bool _isLanShow;

    private ShowBinding(int port, bool isLanShow)
    {
        _port = port;
        _isLanShow = isLanShow;
    }

    public static ShowBinding Configure(WebApplicationBuilder builder)
    {
        var port = builder.Configuration.GetValue($"{NetworkOptions.SectionName}:{nameof(NetworkOptions.Port)}", NetworkOptions.DefaultPort);
        var hasExplicitBinding = !string.IsNullOrEmpty(builder.Configuration["urls"])
            || !string.IsNullOrEmpty(builder.Configuration["http_ports"])
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
        if (!hasExplicitBinding)
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }

        return new ShowBinding(port, !hasExplicitBinding);
    }

    /// <summary>Logs the host and team URLs once the server is listening.</summary>
    public void LogUrlsOnStart(WebApplication app)
    {
        if (!_isLanShow)
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILogger<ShowBinding>>();
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            LogHostScreen(logger, _port);
            if (LanAddress.Find() is { } lan)
            {
                LogTeamJoin(logger, lan, _port);
            }
            else
            {
                LogNoLanAddress(logger);
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Host screen: http://localhost:{Port}/host")]
    private static partial void LogHostScreen(ILogger logger, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "Team join:   http://{LanAddress}:{Port}/play")]
    private static partial void LogTeamJoin(ILogger logger, IPAddress lanAddress, int port);

    [LoggerMessage(Level = LogLevel.Warning, Message = "LAN address not found. Is this machine on the venue Wi-Fi?")]
    private static partial void LogNoLanAddress(ILogger logger);
}
