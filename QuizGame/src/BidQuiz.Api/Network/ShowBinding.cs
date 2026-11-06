namespace BidQuiz.Api.Network;

/// <summary>
/// Locally the show listens on every interface so phones on the venue Wi-Fi can join. App Service,
/// Aspire and an explicit ASPNETCORE_URLS keep their own binding.
/// </summary>
public sealed class ShowBinding
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

    /// <summary>Prints the host and team URLs once the server is listening.</summary>
    public void PrintUrlsOnStart(WebApplication app)
    {
        if (!_isLanShow)
        {
            return;
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var lan = LanAddress.Find();
            Console.WriteLine();
            Console.WriteLine($"  Host screen:  http://localhost:{_port}/host");
            Console.WriteLine(lan is null
                ? "  LAN address not found. Is this machine on the venue Wi-Fi?"
                : $"  Team join:    http://{lan}:{_port}/play");
            Console.WriteLine();
        });
    }
}
