using System.ComponentModel.DataAnnotations;

namespace BidQuiz.Api.Network;

/// <summary>How phones reach the show. Bound to the <c>Game</c> configuration section, next to the game settings.</summary>
public sealed class NetworkOptions
{
    public const string SectionName = "Game";

    public const int DefaultPort = 5000;

    public const int MaxPort = 65535;

    /// <summary>Overrides the URL in the QR code, for example https://quiz.example.com.</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>Local port, used when no binding is configured.</summary>
    [Range(1, MaxPort)]
    public int Port { get; set; } = DefaultPort;
}
