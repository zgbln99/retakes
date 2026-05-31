using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Decides and applies special rounds — Lucky Round (random chance, everyone
/// gets a random strong loadout) and Pistol Round (every N rounds, pistols only).
/// Symmetric and announced. Hooks into the weapon allocator via its per-round
/// override so it composes with the normal allocation flow.
/// </summary>
public class SpecialRoundsService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly WeaponAllocationService _weapons;
    private readonly SpecialRoundSettings _settings;
    private readonly Random _random;

    private int _roundNumber;
    private List<string>? _luckyLoadout;     // active lucky loadout this round
    private bool _pistolActive;

    // One-shot admin forces for the next round.
    private bool _forceLuckyNext;
    private bool _forcePistolNext;

    public SpecialRoundsService(WeaponAllocationService weapons, SpecialRoundSettings settings, Random random)
    {
        _weapons = weapons;
        _settings = settings;
        _random = random;
    }

    public void ResetCycle() => _roundNumber = 0;

    public void ForceLuckyNextRound() => _forceLuckyNext = true;
    public void ForcePistolNextRound() => _forcePistolNext = true;

    /// <summary>Decide the round type at round start and arm the allocator override.</summary>
    public void OnRoundStart()
    {
        _roundNumber++;
        _luckyLoadout = null;
        _pistolActive = false;
        _weapons.RoundOverride = null;

        var humans = Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot && !p.IsHLTV);

        // Pistol round takes precedence over lucky (and over the forced flags order).
        if (DecidePistol(humans))
        {
            _pistolActive = true;
            _weapons.RoundOverride = ApplyPistol;
            Server.PrintToChatAll($"{Prefix}{ChatColors.Gold}PISTOL ROUND!{ChatColors.White} Tylko pistolety.");
        }
        else if (DecideLucky(humans))
        {
            var loadouts = _settings.Lucky.Loadouts;
            if (loadouts.Count > 0)
            {
                var pick = loadouts.ElementAt(_random.Next(loadouts.Count));
                _luckyLoadout = pick.Value;
                _weapons.RoundOverride = ApplyLucky;
                Server.PrintToChatAll($"{Prefix}{ChatColors.Gold}LUCKY ROUND!{ChatColors.White} Zestaw: {ChatColors.Green}{pick.Key}");
            }
        }

        _forceLuckyNext = false;
        _forcePistolNext = false;
    }

    public void OnRoundEnd()
    {
        // Clear the override so normal rounds resume.
        _weapons.RoundOverride = null;
        _luckyLoadout = null;
        _pistolActive = false;
    }

    private bool DecidePistol(int humans)
    {
        if (_forcePistolNext) return true;
        if (!_settings.Pistol.Enabled) return false;
        if (humans < _settings.Pistol.MinPlayers) return false;
        var every = Math.Max(1, _settings.Pistol.EveryXRounds);
        return _roundNumber % every == 0;
    }

    private bool DecideLucky(int humans)
    {
        if (_forceLuckyNext) return true;
        if (!_settings.Lucky.Enabled) return false;
        if (humans < _settings.Lucky.MinPlayers) return false;
        return _random.NextDouble() < _settings.Lucky.Chance;
    }

    #region Apply (called per player by the allocator override)
    private bool ApplyLucky(CCSPlayerController player)
    {
        if (_luckyLoadout == null) return false;

        player.GiveNamedItem("item_assaultsuit");
        foreach (var item in _luckyLoadout)
        {
            if (!string.IsNullOrWhiteSpace(item)) player.GiveNamedItem(item);
        }
        player.GiveNamedItem("weapon_knife");
        return true;
    }

    private bool ApplyPistol(CCSPlayerController player)
    {
        var s = _settings.Pistol;

        if (s.GiveArmor) player.GiveNamedItem(s.GiveHelmet ? "item_assaultsuit" : "item_kevlar");
        if (s.GiveDefuseKit && player.Team == CsTeam.CounterTerrorist) GiveDefuser(player);

        if (s.Pistols.Count > 0)
        {
            string pistol;
            if (s.Mode == "same_for_all")
            {
                // Deterministic pick for the whole round (first in list).
                pistol = s.Pistols[0];
            }
            else
            {
                pistol = s.Pistols[_random.Next(s.Pistols.Count)];
            }
            player.GiveNamedItem(pistol);
        }

        player.GiveNamedItem("weapon_knife");
        return true;
    }

    private static void GiveDefuser(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is { IsValid: true } && pawn.ItemServices != null)
        {
            new CCSPlayer_ItemServices(pawn.ItemServices.Handle).HasDefuser = true;
        }
    }
    #endregion
}
