using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services;

/// <summary>
/// Built-in weapon allocator. Replaces the stubbed fallback allocation in the
/// base plugin with real weapon giving: random per round, optionally overridden
/// by each player's !guns preferences. Everything is symmetric and announced —
/// no hidden advantages.
/// </summary>
public class WeaponAllocationService
{
    private readonly WeaponSettings _settings;
    private readonly Random _random;
    private readonly ConcurrentDictionary<ulong, WeaponPreference> _preferences = new();

    public WeaponAllocationService(WeaponSettings settings, Random random)
    {
        _settings = settings;
        _random = random;
    }

    public WeaponSettings Settings => _settings;

    public void Allocate(CCSPlayerController player)
    {
        if (!_settings.IsEnabled) return;
        if (!player.IsValid || !player.PawnIsAlive) return;

        var team = player.Team;
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist) return;

        player.RemoveWeapons();

        if (_settings.GiveArmor)
        {
            player.GiveNamedItem(_settings.GiveHelmet ? "item_assaultsuit" : "item_kevlar");
        }

        if (team == CsTeam.CounterTerrorist && _settings.GiveDefuserToCt)
        {
            player.GiveNamedItem("item_defuser");
        }

        var primary = ChoosePrimary(player.SteamID, team);
        if (primary != null)
        {
            player.GiveNamedItem(primary);
        }

        var pistol = ChoosePistol(team);
        if (pistol != null)
        {
            player.GiveNamedItem(pistol);
        }

        if (_settings.GiveGrenades)
        {
            GiveGrenades(player, team);
        }

        player.GiveNamedItem("weapon_knife");
    }

    private string? ChoosePrimary(ulong steamId, CsTeam team)
    {
        var pref = _settings.AllowPreferences ? GetPreference(steamId) : null;

        var rifles = team == CsTeam.Terrorist ? _settings.TerroristRifles : _settings.CounterTerroristRifles;
        var riflePref = team == CsTeam.Terrorist ? pref?.TerroristRifle : pref?.CounterTerroristRifle;

        // Explicit sniper preference wins.
        if (_settings.AllowSnipers && pref?.PreferSniper == true && _settings.Snipers.Count > 0)
        {
            return Pick(_settings.Snipers);
        }

        // Explicit rifle preference next.
        if (riflePref != null && rifles.Contains(riflePref))
        {
            return riflePref;
        }

        // Otherwise a small random chance of a sniper, else a random rifle.
        if (_settings.AllowSnipers && _settings.Snipers.Count > 0 && _random.NextDouble() < _settings.SniperChance)
        {
            return Pick(_settings.Snipers);
        }

        return rifles.Count > 0 ? Pick(rifles) : null;
    }

    private string? ChoosePistol(CsTeam team)
    {
        var pistols = team == CsTeam.Terrorist ? _settings.TerroristPistols : _settings.CounterTerroristPistols;
        return pistols.Count > 0 ? Pick(pistols) : null;
    }

    private void GiveGrenades(CCSPlayerController player, CsTeam team)
    {
        var pool = team == CsTeam.Terrorist ? _settings.TerroristGrenades : _settings.CounterTerroristGrenades;
        if (pool.Count == 0) return;

        var count = Math.Clamp(_settings.MaxGrenadesPerPlayer, 0, pool.Count);
        foreach (var grenade in pool.OrderBy(_ => _random.Next()).Take(count))
        {
            player.GiveNamedItem(grenade);
        }
    }

    private string Pick(IReadOnlyList<string> list) => list[_random.Next(list.Count)];

    #region Preferences API (used by the !guns menu)
    public WeaponPreference? GetPreference(ulong steamId) =>
        _preferences.TryGetValue(steamId, out var pref) ? pref : null;

    public WeaponPreference GetOrCreatePreference(ulong steamId) =>
        _preferences.GetOrAdd(steamId, _ => new WeaponPreference());

    public void ResetPreference(ulong steamId) => _preferences.TryRemove(steamId, out _);

    public void ClearPreference(ulong steamId) => ResetPreference(steamId);

    /// <summary>Turns "weapon_m4a1_silencer" into a friendlier "M4A1-S".</summary>
    public static string DisplayName(string itemName)
    {
        return itemName switch
        {
            "weapon_ak47" => "AK-47",
            "weapon_m4a1" => "M4A4",
            "weapon_m4a1_silencer" => "M4A1-S",
            "weapon_galilar" => "Galil AR",
            "weapon_famas" => "FAMAS",
            "weapon_sg556" => "SG 553",
            "weapon_aug" => "AUG",
            "weapon_awp" => "AWP",
            "weapon_ssg08" => "SSG 08 (Scout)",
            _ => itemName.Replace("weapon_", "").ToUpperInvariant()
        };
    }
    #endregion
}
