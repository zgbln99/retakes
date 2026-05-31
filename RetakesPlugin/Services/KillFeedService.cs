using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Announces kill streaks, dominations and revenge to the whole server (chat),
/// and shows a short center-screen message on notable events. Purely cosmetic —
/// affects nobody's gameplay.
/// </summary>
public class KillFeedService
{
    private const string Prefix = " [{0}CWELOWNIA{1}] ";

    private readonly bool _enabledProvider;
    private readonly Func<bool> _isEnabled;

    // Current kill streak per player (reset on death / round start).
    private readonly Dictionary<ulong, int> _streaks = new();
    // How many times killer has killed victim this round: key = (killer, victim).
    private readonly Dictionary<(ulong, ulong), int> _dominationCount = new();
    // Who is currently dominating whom (so we only announce the domination once).
    private readonly HashSet<(ulong, ulong)> _dominating = new();

    private const int DominationThreshold = 4;

    public KillFeedService(Func<bool> isEnabled)
    {
        _isEnabled = isEnabled;
        _enabledProvider = true;
    }

    public void Reset()
    {
        _streaks.Clear();
        _dominationCount.Clear();
        _dominating.Clear();
    }

    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        if (_enabledProvider && !_isEnabled()) return;

        var victim = @event.Userid;
        var attacker = @event.Attacker;

        var victimId = SteamId(victim);
        var attackerId = SteamId(attacker);

        if (victimId != 0)
        {
            // Revenge: the victim was being dominated by their now-killer? handled below.
            _streaks[victimId] = 0;
        }

        // Suicide or world death — nothing to celebrate.
        if (attackerId == 0 || attackerId == victimId || victim == null || attacker == null) return;

        // Kill streak.
        _streaks.TryGetValue(attackerId, out var streak);
        streak++;
        _streaks[attackerId] = streak;
        AnnounceStreak(attacker, streak);

        // Domination / revenge tracking.
        if (victimId != 0)
        {
            // Did the victim previously dominate the attacker? Then this is revenge.
            if (_dominating.Remove((victimId, attackerId)))
            {
                Announce($"{ChatColors.Green}{attacker.PlayerName}{ChatColors.White} wziął {ChatColors.Gold}REWANŻ{ChatColors.White} na {ChatColors.Red}{victim.PlayerName}{ChatColors.White}!");
                _dominationCount[(victimId, attackerId)] = 0;
            }

            var key = (attackerId, victimId);
            _dominationCount.TryGetValue(key, out var count);
            count++;
            _dominationCount[key] = count;

            if (count == DominationThreshold && _dominating.Add(key))
            {
                Announce($"{ChatColors.Green}{attacker.PlayerName}{ChatColors.White} {ChatColors.Gold}DOMINUJE{ChatColors.White} nad {ChatColors.Red}{victim.PlayerName}{ChatColors.White}!");
            }
        }
    }

    private void AnnounceStreak(CCSPlayerController attacker, int streak)
    {
        var label = streak switch
        {
            3 => "seria 3 zabójstw",
            4 => "seria 4 zabójstw",
            5 => "RAGE — 5 zabójstw!",
            6 => "UNSTOPPABLE — 6 zabójstw!",
            7 => "GODLIKE — 7 zabójstw!",
            _ => streak > 7 ? $"seria {streak} zabójstw!" : null
        };

        if (label == null) return;

        Announce($"{ChatColors.Green}{attacker.PlayerName}{ChatColors.White} — {ChatColors.Gold}{label}");
    }

    private static void Announce(string message)
    {
        var prefix = string.Format(Prefix, ChatColors.Green, ChatColors.White);
        Server.PrintToChatAll(prefix + message);
    }

    private static ulong SteamId(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return 0;
        return player.SteamID > 0 ? player.SteamID : 0;
    }
}
