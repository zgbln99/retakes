using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Boss / Juggernaut mode: one player becomes the boss with bonus stats and the
/// ability to see enemies (glow). The boss is NOT announced in chat (surprise),
/// but is recognizable on sight — a coloured tint / bigger model / extra HP — so
/// it is a fair game mode, not a hidden advantage.
/// </summary>
public class BossSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("Health")]
    public int Health { get; set; } = 400;

    [JsonPropertyName("SizeScale")]
    public float SizeScale { get; set; } = 1.3f;

    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 1.1f;

    /// <summary>Extra damage the boss deals per hit.</summary>
    [JsonPropertyName("ExtraDamage")]
    public int ExtraDamage { get; set; } = 15;

    /// <summary>Give the boss glow on enemies (see them) — best effort in CS2.</summary>
    [JsonPropertyName("SeeEnemies")]
    public bool SeeEnemies { get; set; } = true;

    /// <summary>Boss render tint RGB so it is recognizable on sight.</summary>
    [JsonPropertyName("TintR")] public int TintR { get; set; } = 255;
    [JsonPropertyName("TintG")] public int TintG { get; set; } = 60;
    [JsonPropertyName("TintB")] public int TintB { get; set; } = 60;
}
