using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the built-in weapon allocator: random allocation plus optional
/// per-player preferences chosen with the !guns menu.
/// </summary>
public class WeaponSettings
{
    /// <summary>Master toggle for the built-in allocator. Also flippable from the admin panel.</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>When true, players can pick a preferred rifle with !guns. When false everything is random.</summary>
    [JsonPropertyName("AllowPreferences")]
    public bool AllowPreferences { get; set; } = true;

    [JsonPropertyName("GiveArmor")]
    public bool GiveArmor { get; set; } = true;

    [JsonPropertyName("GiveHelmet")]
    public bool GiveHelmet { get; set; } = true;

    [JsonPropertyName("GiveDefuserToCt")]
    public bool GiveDefuserToCt { get; set; } = true;

    [JsonPropertyName("GiveGrenades")]
    public bool GiveGrenades { get; set; } = true;

    /// <summary>Minimum number of grenades a player receives (random between Min and Max).</summary>
    [JsonPropertyName("MinGrenades")]
    public int MinGrenades { get; set; } = 1;

    /// <summary>Maximum number of grenades a player receives (random between Min and Max).</summary>
    [JsonPropertyName("MaxGrenades")]
    public int MaxGrenades { get; set; } = 3;

    /// <summary>
    /// Extra grenades added on top of the random count when the player is the only
    /// one alive on their team (the "lone wolf" bonus). Set to 0 to disable.
    /// </summary>
    [JsonPropertyName("LonePlayerExtraGrenades")]
    public int LonePlayerExtraGrenades { get; set; } = 2;

    /// <summary>Hard cap on total grenades regardless of bonuses (CS2 carries up to 4 by default).</summary>
    [JsonPropertyName("GrenadeHardCap")]
    public int GrenadeHardCap { get; set; } = 4;

    /// <summary>Allow the AWP to appear in random allocation / be picked.</summary>
    [JsonPropertyName("AllowSnipers")]
    public bool AllowSnipers { get; set; } = true;

    /// <summary>Probability (0..1) of randomly receiving a sniper when no rifle preference is set.</summary>
    [JsonPropertyName("SniperChance")]
    public double SniperChance { get; set; } = 0.12;

    [JsonPropertyName("TerroristRifles")]
    public List<string> TerroristRifles { get; set; } = new()
    {
        "weapon_ak47", "weapon_galilar", "weapon_sg556"
    };

    [JsonPropertyName("CounterTerroristRifles")]
    public List<string> CounterTerroristRifles { get; set; } = new()
    {
        "weapon_m4a1", "weapon_m4a1_silencer", "weapon_famas", "weapon_aug"
    };

    [JsonPropertyName("Snipers")]
    public List<string> Snipers { get; set; } = new()
    {
        "weapon_awp", "weapon_ssg08"
    };

    [JsonPropertyName("TerroristPistols")]
    public List<string> TerroristPistols { get; set; } = new()
    {
        "weapon_glock", "weapon_tec9", "weapon_p250", "weapon_deagle"
    };

    [JsonPropertyName("CounterTerroristPistols")]
    public List<string> CounterTerroristPistols { get; set; } = new()
    {
        "weapon_usp_silencer", "weapon_hkp2000", "weapon_p250", "weapon_fiveseven", "weapon_deagle"
    };

    [JsonPropertyName("TerroristGrenades")]
    public List<string> TerroristGrenades { get; set; } = new()
    {
        "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov"
    };

    [JsonPropertyName("CounterTerroristGrenades")]
    public List<string> CounterTerroristGrenades { get; set; } = new()
    {
        "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_incgrenade"
    };
}
