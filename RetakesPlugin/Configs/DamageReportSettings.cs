using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// FaceIt-style end-of-round damage report: for each player, how much damage they
/// dealt to whom, how many hits, and the victim's remaining HP.
/// </summary>
public class DamageReportSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>Also report damage taken from each attacker (the "to me" side).</summary>
    [JsonPropertyName("ShowDamageTaken")]
    public bool ShowDamageTaken { get; set; } = true;
}
