using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for symmetric fun modes (knives only, deagle only, HE war, scouts,
/// low gravity). The active mode is chosen from the admin panel.
/// </summary>
public class FunModeSettings
{
    /// <summary>Master toggle for the fun-mode feature (the panel submenu).</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gravity value used by the Low Gravity mode (normal is 800).</summary>
    [JsonPropertyName("LowGravityValue")]
    public int LowGravityValue { get; set; } = 300;
}
