namespace BidQuiz.Api.Network;

/// <summary>The URL in the QR code that teams scan.</summary>
public static class JoinUrl
{
    private const string PlayPath = "/play";

    public static string For(HttpRequest request, NetworkOptions network)
    {
        var baseUrl = network.PublicBaseUrl.TrimEnd('/');
        if (baseUrl.Length == 0)
        {
            // A host opened on localhost must still hand phones an address they can reach.
            var isLocal = request.Host.Host is "localhost" or "127.0.0.1" or "[::1]";
            baseUrl = isLocal && LanAddress.Find() is { } lan
                ? $"{request.Scheme}://{lan}:{request.Host.Port ?? network.Port}"
                : $"{request.Scheme}://{request.Host}";
        }

        return baseUrl + PlayPath;
    }
}
