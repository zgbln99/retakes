using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the built-in instadefuse feature.
/// Based on B3none/cs2-instadefuse (GPL-3.0) — see NOTICE.
/// </summary>
public class InstadefuseSettings
{
    /// <summary>
    /// Master toggle for the instadefuse feature. Can also be flipped at runtime
    /// from the admin panel.
    /// </summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Distance (in units) from the planted bomb within which a fire/inferno is
    /// considered a threat that blocks an instant defuse.
    /// </summary>
    [JsonPropertyName("InfernoThreatDistance")]
    public float InfernoThreatDistance { get; set; } = 250.0f;

    /// <summary>
    /// When true, the instadefuse only triggers once every terrorist is dead
    /// (the classic retakes behaviour). When false it will also trigger while
    /// terrorists are alive (not recommended).
    /// </summary>
    [JsonPropertyName("RequireAllTerroristsDead")]
    public bool RequireAllTerroristsDead { get; set; } = true;
}
