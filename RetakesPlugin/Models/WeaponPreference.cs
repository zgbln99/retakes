namespace RetakesPlugin.Models;

/// <summary>
/// A single player's weapon preferences. A null value means "random" for that
/// slot, which is the default.
/// </summary>
public class WeaponPreference
{
    /// <summary>Preferred terrorist primary (e.g. "weapon_ak47"), or null for random.</summary>
    public string? TerroristRifle { get; set; }

    /// <summary>Preferred counter-terrorist primary (e.g. "weapon_m4a1_silencer"), or null for random.</summary>
    public string? CounterTerroristRifle { get; set; }

    /// <summary>When true the player would like a sniper when possible.</summary>
    public bool PreferSniper { get; set; }
}
