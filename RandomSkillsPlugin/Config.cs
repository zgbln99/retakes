using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace RandomSkillsPlugin;

public class RandomSkillsConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>Announce each player's skill on the center HUD at round start.</summary>
    [JsonPropertyName("AnnounceOnHud")]
    public bool AnnounceOnHud { get; set; } = true;

    /// <summary>Cooldown (seconds) for active (E-key) skills.</summary>
    [JsonPropertyName("ActiveCooldownSeconds")]
    public float ActiveCooldownSeconds { get; set; } = 30.0f;

    /// <summary>
    /// Skill weights — higher = more likely. Set a weight to 0 to disable that skill.
    /// Keys must match the skill ids: speed, tank, health, damage, teleport, lowgrav, invis.
    /// </summary>
    [JsonPropertyName("Weights")]
    public Dictionary<string, int> Weights { get; set; } = new()
    {
        ["speed"] = 20,
        ["tank"] = 15,
        ["health"] = 20,
        ["damage"] = 15,
        ["teleport"] = 10,
        ["lowgrav"] = 10,
        ["invis"] = 10
    };

    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 1.5f;

    [JsonPropertyName("TankHealth")]
    public int TankHealth { get; set; } = 500;

    [JsonPropertyName("TankSpeedMultiplier")]
    public float TankSpeedMultiplier { get; set; } = 0.5f;

    [JsonPropertyName("HealthMin")]
    public int HealthMin { get; set; } = 105;

    [JsonPropertyName("HealthMax")]
    public int HealthMax { get; set; } = 300;

    [JsonPropertyName("ExtraDamage")]
    public int ExtraDamage { get; set; } = 30;

    [JsonPropertyName("LowGravityScale")]
    public float LowGravityScale { get; set; } = 0.4f;

    /// <summary>Invisibility alpha 0-255 (0 = fully invisible, 255 = opaque).</summary>
    [JsonPropertyName("InvisAlpha")]
    public int InvisAlpha { get; set; } = 60;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;
}
