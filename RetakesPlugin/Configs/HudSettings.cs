using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for on-screen / chat HUD messages: bomb location after a plant and
/// kill streaks / dominations. Purely cosmetic.
/// </summary>
public class HudSettings
{
    /// <summary>Master toggle for all HUD messages. Also flippable from the admin panel.</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>Show a center-screen message with the bombsite when the bomb is planted.</summary>
    [JsonPropertyName("ShowBombSiteOnPlant")]
    public bool ShowBombSiteOnPlant { get; set; } = true;

    /// <summary>Announce kill streaks, dominations and revenge in chat.</summary>
    [JsonPropertyName("ShowKillStreaks")]
    public bool ShowKillStreaks { get; set; } = true;
}
