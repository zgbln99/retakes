using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Training position-preview mode. When enabled, player positions are highlighted
/// so a player can learn/rehearse entries onto opponent positions. Crucially this
/// is a CONSENT-BASED, transparent mode: while it is active EVERY player is shown
/// a clear on-screen notice that positions are visible, so no one is deceived.
/// It is meant for practice (e.g. against bots or with a willing sparring partner),
/// NOT for a competitive match against players who don't know.
/// </summary>
public class TrainingSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>Center-screen notice shown to everyone while preview is active.</summary>
    [JsonPropertyName("Notice")]
    public string Notice { get; set; } = "TRENING: pozycje graczy są widoczne dla wszystkich";

    /// <summary>Highlight color RGB for previewed players.</summary>
    [JsonPropertyName("HighlightR")] public int HighlightR { get; set; } = 255;
    [JsonPropertyName("HighlightG")] public int HighlightG { get; set; } = 255;
    [JsonPropertyName("HighlightB")] public int HighlightB { get; set; } = 0;
}
