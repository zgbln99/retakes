using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for special rounds: Lucky Round (random chance of a strong loadout
/// for everyone) and Pistol Round (every N rounds, pistols only). Both are
/// symmetric — every player is affected equally and the round is announced.
/// </summary>
public class SpecialRoundSettings
{
    [JsonPropertyName("LuckyRound")]
    public LuckyRoundSettings Lucky { get; set; } = new();

    [JsonPropertyName("PistolRound")]
    public PistolRoundSettings Pistol { get; set; } = new();
}

public class LuckyRoundSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>Chance (0..1) that any given round becomes a Lucky Round.</summary>
    [JsonPropertyName("Chance")]
    public double Chance { get; set; } = 0.1;

    [JsonPropertyName("MinPlayers")]
    public int MinPlayers { get; set; } = 2;

    /// <summary>
    /// Named loadouts; one is picked at random and given to everyone. Each entry
    /// is a list of item names (e.g. "weapon_awp", "weapon_deagle").
    /// </summary>
    [JsonPropertyName("Loadouts")]
    public Dictionary<string, List<string>> Loadouts { get; set; } = new()
    {
        ["AWP + Deagle"] = new() { "weapon_awp", "weapon_deagle" },
        ["Pełny ekwipunek"] = new() { "weapon_ak47", "weapon_deagle", "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade" },
        ["Negev"] = new() { "weapon_negev" },
        ["Scout + nóż"] = new() { "weapon_ssg08" },
        ["Same noże"] = new() { }
    };
}

public class PistolRoundSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>A pistol round happens every N rounds (e.g. 5 = rounds 5,10,15...).</summary>
    [JsonPropertyName("EveryXRounds")]
    public int EveryXRounds { get; set; } = 5;

    [JsonPropertyName("MinPlayers")]
    public int MinPlayers { get; set; } = 2;

    /// <summary>"same_for_all" = everyone gets the same pistol; "random_per_player" = each random.</summary>
    [JsonPropertyName("Mode")]
    public string Mode { get; set; } = "random_per_player";

    [JsonPropertyName("Pistols")]
    public List<string> Pistols { get; set; } = new()
    {
        "weapon_glock", "weapon_usp_silencer", "weapon_p250", "weapon_deagle",
        "weapon_fiveseven", "weapon_tec9", "weapon_hkp2000"
    };

    [JsonPropertyName("GiveArmor")]
    public bool GiveArmor { get; set; } = true;

    [JsonPropertyName("GiveHelmet")]
    public bool GiveHelmet { get; set; } = false;

    [JsonPropertyName("GiveDefuseKit")]
    public bool GiveDefuseKit { get; set; } = true;
}
