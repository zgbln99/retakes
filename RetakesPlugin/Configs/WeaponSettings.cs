using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the built-in weapon allocator. By default the loadout is fixed:
/// AK-47 for T, M4A1-S for CT, plus grenades — no randomness and no per-player
/// preferences. Random allocation (rifle pools, snipers, random pistols) is still
/// implemented and can be switched back on with <see cref="RandomWeapons"/>.
/// </summary>
public class WeaponSettings
{
    /// <summary>Master toggle for the built-in allocator. Also flippable from the admin panel.</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Master switch for random weapons. When false (the default) every player
    /// gets the same fixed primary for their team — <see cref="TerroristPrimary"/>
    /// / <see cref="CounterTerroristPrimary"/> — and the rifle pools, sniper roll
    /// and !guns preferences are all bypassed. Grenades stay unaffected.
    /// </summary>
    [JsonPropertyName("RandomWeapons")]
    public bool RandomWeapons { get; set; } = false;

    /// <summary>Fixed terrorist primary used when <see cref="RandomWeapons"/> is false.</summary>
    [JsonPropertyName("TerroristPrimary")]
    public string TerroristPrimary { get; set; } = "weapon_ak47";

    /// <summary>Fixed counter-terrorist primary used when <see cref="RandomWeapons"/> is false.</summary>
    [JsonPropertyName("CounterTerroristPrimary")]
    public string CounterTerroristPrimary { get; set; } = "weapon_m4a1_silencer";

    /// <summary>Give a secondary pistol at all. Off by default: rifle + grenades only.</summary>
    [JsonPropertyName("GivePistol")]
    public bool GivePistol { get; set; } = false;

    /// <summary>Fixed terrorist pistol used when <see cref="GivePistol"/> is on and randomness is off.</summary>
    [JsonPropertyName("TerroristPistol")]
    public string TerroristPistol { get; set; } = "weapon_glock";

    /// <summary>Fixed counter-terrorist pistol used when <see cref="GivePistol"/> is on and randomness is off.</summary>
    [JsonPropertyName("CounterTerroristPistol")]
    public string CounterTerroristPistol { get; set; } = "weapon_usp_silencer";

    /// <summary>
    /// When true, players can pick a preferred rifle with !guns. Only has an effect
    /// while <see cref="RandomWeapons"/> is on — with a fixed loadout there is
    /// nothing to choose.
    /// </summary>
    [JsonPropertyName("AllowPreferences")]
    public bool AllowPreferences { get; set; } = false;

    [JsonPropertyName("GiveArmor")]
    public bool GiveArmor { get; set; } = true;

    [JsonPropertyName("GiveHelmet")]
    public bool GiveHelmet { get; set; } = true;

    [JsonPropertyName("GiveDefuserToCt")]
    public bool GiveDefuserToCt { get; set; } = true;

    [JsonPropertyName("GiveGrenades")]
    public bool GiveGrenades { get; set; } = true;

    /// <summary>Minimum number of grenades a player always receives.</summary>
    [JsonPropertyName("MinGrenades")]
    public int MinGrenades { get; set; } = 1;

    /// <summary>Maximum number of grenades a player can receive.</summary>
    [JsonPropertyName("MaxGrenades")]
    public int MaxGrenades { get; set; } = 2;

    /// <summary>
    /// Probability (0..1) of getting each additional grenade above the minimum.
    /// Applied repeatedly with falloff, so high counts are rare. With 0.25 the
    /// distribution is roughly: 1 nade ~75%, 2 nades ~19%, 3 nades ~5%.
    /// </summary>
    [JsonPropertyName("ExtraGrenadeChance")]
    public double ExtraGrenadeChance { get; set; } = 0.25;

    /// <summary>
    /// Extra grenades added on top when the player is the only one alive on their
    /// team (the "lone wolf" bonus). Set to 0 to disable.
    /// </summary>
    [JsonPropertyName("LonePlayerExtraGrenades")]
    public int LonePlayerExtraGrenades { get; set; } = 1;

    /// <summary>Hard cap on total grenades regardless of bonuses (CS2 carries up to 4 by default).</summary>
    [JsonPropertyName("GrenadeHardCap")]
    public int GrenadeHardCap { get; set; } = 3;

    /// <summary>Allow snipers (AWP/SSG) in random allocation / preferences.</summary>
    [JsonPropertyName("AllowSnipers")]
    public bool AllowSnipers { get; set; } = false;

    /// <summary>Allow the SSG 08 (scout) specifically. When false the scout is never given.</summary>
    [JsonPropertyName("AllowScout")]
    public bool AllowScout { get; set; } = false;

    /// <summary>Probability (0..1) of randomly receiving a sniper when no rifle preference is set.</summary>
    [JsonPropertyName("SniperChance")]
    public double SniperChance { get; set; } = 0.0;

    /// <summary>Random-mode rifle pool for T. Only used when <see cref="RandomWeapons"/> is on.</summary>
    [JsonPropertyName("TerroristRifles")]
    public List<string> TerroristRifles { get; set; } = new()
    {
        "weapon_ak47"
    };

    /// <summary>Random-mode rifle pool for CT. Only used when <see cref="RandomWeapons"/> is on.</summary>
    [JsonPropertyName("CounterTerroristRifles")]
    public List<string> CounterTerroristRifles { get; set; } = new()
    {
        "weapon_m4a1_silencer"
    };

    /// <summary>Sniper pool. Empty by default — snipers are off.</summary>
    [JsonPropertyName("Snipers")]
    public List<string> Snipers { get; set; } = new();

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
