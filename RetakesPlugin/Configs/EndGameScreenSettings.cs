using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the end-of-match summary screen (center HTML): MVP, top fragger,
/// best ADR, most clutches and the next map.
/// </summary>
public class EndGameScreenSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>How long the screen stays on the players' HUD, in seconds.</summary>
    [JsonPropertyName("DurationSeconds")]
    public float DurationSeconds { get; set; } = 12.0f;

    [JsonPropertyName("ShowBestPlayer")]
    public bool ShowBestPlayer { get; set; } = true;

    [JsonPropertyName("ShowTopFragger")]
    public bool ShowTopFragger { get; set; } = true;

    [JsonPropertyName("ShowBestAdr")]
    public bool ShowBestAdr { get; set; } = true;

    [JsonPropertyName("ShowMostClutches")]
    public bool ShowMostClutches { get; set; } = true;

    [JsonPropertyName("ShowNextMap")]
    public bool ShowNextMap { get; set; } = true;
}
