namespace BidQuiz.Domain.Teams;

/// <summary>One colour per team. All are readable under ivory text, and none is the theme's maroon.</summary>
public static class TeamPalette
{
    public static readonly IReadOnlyList<string> Colors =
    [
        "#1F5FBF", // Blue
        "#0E8A5F", // Green
        "#C4561A", // Orange
        "#7B3FBF", // Purple
        "#0B8299", // Teal
        "#C2267E", // Magenta
        "#5F7D14", // Olive
        "#D12F2F", // Red
        "#56677A", // Slate
        "#A67C00", // Gold
        "#3D3FB5", // Indigo
        "#0B5D3E", // Pine
        "#2E7CC4", // Sky
        "#6B1F6B", // Plum
        "#8F3A12", // Rust
        "#6B4423", // Brown
        "#3F8A2E", // Leaf
        "#2F5D62", // Deep teal
        "#33363B", // Charcoal
        "#A044B5"  // Orchid
    ];

    public static string FirstFree(IEnumerable<string> taken) => Colors.Except(taken).First();
}
