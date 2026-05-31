using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services;

/// <summary>
/// Built-in weapon allocator. Replaces the stubbed fallback allocation in the
/// base plugin with real weapon giving: random per round, optionally overridden
/// by each player's !guns preferences, or globally forced to a preset weapon set
/// by an admin. Everything is symmetric and announced — no hidden advantages.
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
        AvailableSets = BuildSets();
    }

    public WeaponSettings Settings => _settings;

    #region Forced weapon set (admin)
    /// <summary>A globally forced weapon set chosen by an admin, or null for normal allocation.</summary>
    public WeaponSet? ForcedSet { get; private set; }

    public IReadOnlyList<WeaponSet> AvailableSets { get; }

    public void SetForcedSet(WeaponSet? set) => ForcedSet = set;

    public WeaponSet? FindSet(string key) =>
        AvailableSets.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    #endregion

    public void Allocate(CCSPlayerController player)
    {
        if (!_settings.IsEnabled) return;
        if (!player.IsValid || !player.PawnIsAlive) return;

        var team = player.Team;
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist) return;

        // NOTE: weapons are already removed by the caller (OnRoundPostStart) before
        // the bomb is handed to the planter, so we must NOT call RemoveWeapons() here
        // or we would strip the bomb back off the planter.

        if (_settings.GiveArmor)
        {
            player.GiveNamedItem(_settings.GiveHelmet ? "item_assaultsuit" : "item_kevlar");
        }

        if (team == CsTeam.CounterTerrorist && _settings.GiveDefuserToCt)
        {
            GiveDefuser(player);
        }

        var primary = ChoosePrimary(player.SteamID, team);
        if (primary != null)
        {
            player.GiveNamedItem(primary);
        }

        var pistol = ChoosePistol(player.SteamID, team);
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
        // An admin-forced set overrides everything (still symmetric for all players).
        if (ForcedSet != null)
        {
            return ForcedSet.Primary(team, _random);
        }

        var pref = _settings.AllowPreferences ? GetPreference(steamId) : null;

        var rifles = team == CsTeam.Terrorist ? _settings.TerroristRifles : _settings.CounterTerroristRifles;
        var riflePref = team == CsTeam.Terrorist ? pref?.TerroristRifle : pref?.CounterTerroristRifle;

        if (_settings.AllowSnipers && pref?.PreferSniper == true && _settings.Snipers.Count > 0)
        {
            return Pick(_settings.Snipers);
        }

        if (riflePref != null && rifles.Contains(riflePref))
        {
            return riflePref;
        }

        if (_settings.AllowSnipers && _settings.Snipers.Count > 0 && _random.NextDouble() < _settings.SniperChance)
        {
            return Pick(_settings.Snipers);
        }

        return rifles.Count > 0 ? Pick(rifles) : null;
    }

    private string? ChoosePistol(ulong steamId, CsTeam team)
    {
        if (ForcedSet != null)
        {
            return ForcedSet.Pistol(team, _random);
        }

        var pistols = team == CsTeam.Terrorist ? _settings.TerroristPistols : _settings.CounterTerroristPistols;
        return pistols.Count > 0 ? Pick(pistols) : null;
    }

    #region Grenades
    private void GiveGrenades(CCSPlayerController player, CsTeam team)
    {
        var pool = team == CsTeam.Terrorist ? _settings.TerroristGrenades : _settings.CounterTerroristGrenades;
        if (pool.Count == 0) return;

        var min = Math.Max(0, _settings.MinGrenades);
        var max = Math.Max(min, _settings.MaxGrenades);

        // Weighted roll: start at the minimum and add each extra grenade only with
        // ExtraGrenadeChance, so most players get the minimum and high counts are rare.
        var count = min;
        var chance = Math.Clamp(_settings.ExtraGrenadeChance, 0.0, 1.0);
        while (count < max && _random.NextDouble() < chance)
        {
            count++;
        }

        // Lone-wolf bonus: the only player alive on their team gets extra utility.
        if (_settings.LonePlayerExtraGrenades > 0 && IsAloneOnTeam(player, team))
        {
            count += _settings.LonePlayerExtraGrenades;
        }

        count = Math.Min(count, Math.Max(1, _settings.GrenadeHardCap));
        GiveRandomGrenades(player, pool, count);
    }

    /// <summary>
    /// Gives up to <paramref name="count"/> grenades picked at random from the pool,
    /// respecting CS2's per-type carry limits (2 flashbangs, 1 of everything else).
    /// </summary>
    private void GiveRandomGrenades(CCSPlayerController player, IReadOnlyList<string> pool, int count)
    {
        var given = new Dictionary<string, int>();
        var total = 0;
        var attempts = 0;
        var maxAttempts = count * 10 + 20;

        while (total < count && attempts < maxAttempts)
        {
            attempts++;
            var grenade = pool[_random.Next(pool.Count)];
            var cap = grenade == "weapon_flashbang" ? 2 : 1;

            given.TryGetValue(grenade, out var have);
            if (have >= cap) continue;

            player.GiveNamedItem(grenade);
            given[grenade] = have + 1;
            total++;
        }
    }

    private static bool IsAloneOnTeam(CCSPlayerController player, CsTeam team)
    {
        var teammates = 0;
        foreach (var other in Utilities.GetPlayers())
        {
            if (!other.IsValid || other.Team != team || !other.PawnIsAlive) continue;
            teammates++;
            if (teammates > 1) return false;
        }

        return teammates <= 1;
    }
    #endregion

    private static void GiveDefuser(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is { IsValid: true } && pawn.ItemServices != null)
        {
            new CCSPlayer_ItemServices(pawn.ItemServices.Handle).HasDefuser = true;
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
            "weapon_deagle" => "Desert Eagle",
            _ => itemName.Replace("weapon_", "").ToUpperInvariant()
        };
    }
    #endregion

    #region Weapon set presets
    private List<WeaponSet> BuildSets()
    {
        string TeamRifle(CsTeam t) => t == CsTeam.Terrorist
            ? PickOr(_settings.TerroristRifles, "weapon_ak47")
            : PickOr(_settings.CounterTerroristRifles, "weapon_m4a1_silencer");

        string TeamPistol(CsTeam t) => t == CsTeam.Terrorist
            ? PickOr(_settings.TerroristPistols, "weapon_glock")
            : PickOr(_settings.CounterTerroristPistols, "weapon_usp_silencer");

        return new List<WeaponSet>
        {
            new("rifles", "Karabiny (AK/M4)", (t, _) => TeamRifle(t), (t, _) => TeamPistol(t)),
            new("pistols", "Tylko pistolety", (_, _) => null, (t, _) => TeamPistol(t)),
            new("deagle", "Deagle", (_, _) => null, (_, _) => "weapon_deagle"),
            new("awp", "AWP", (_, _) => "weapon_awp", (t, _) => TeamPistol(t)),
            new("scout", "Scout (SSG 08)", (_, _) => "weapon_ssg08", (t, _) => TeamPistol(t))
        };
    }

    private string PickOr(IReadOnlyList<string> list, string fallback) =>
        list.Count > 0 ? Pick(list) : fallback;
    #endregion
}
