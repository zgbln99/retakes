using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Automatic end-of-cycle map vote. Independent of !rtv. On the last round of the
/// match it opens a vote for every real player; after the round ends, it changes
/// to the winning map — but only if at least one player voted. Uses the shared
/// MenuService and the same crash-safe map-change flow as the rest of the plugin.
/// </summary>
public class AutoEndMapVoteService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly BasePlugin _plugin;
    private readonly MenuService _menus;
    private readonly AutoEndMapVoteSettings _settings;
    private readonly Random _random;

    // One vote per SteamID (re-voting overwrites).
    private readonly Dictionary<ulong, string> _votes = new();

    private bool _voteActive;
    private bool _voteCompleted;     // a vote already ran this cycle (no restart)
    private bool _changeQueued;      // changelevel already scheduled (no double change)
    private string? _pendingMap;     // winning map awaiting the round end

    /// <summary>The winning next map if a vote has decided one, else null.</summary>
    public string? PendingMap => _pendingMap;

    /// <summary>
    /// Freezes plugin logic before the map changes (shared with the rtv flow). Must
    /// be set by the plugin to RetakesPlugin.BeginMapChange.
    /// </summary>
    public Action? OnBeginMapChange { get; set; }

    public AutoEndMapVoteService(BasePlugin plugin, MenuService menus, AutoEndMapVoteSettings settings, Random random)
    {
        _plugin = plugin;
        _menus = menus;
        _settings = settings;
        _random = random;
    }

    /// <summary>Clears state on map start (new cycle).</summary>
    public void Reset()
    {
        _votes.Clear();
        _voteActive = false;
        _voteCompleted = false;
        _changeQueued = false;
        _pendingMap = null;
    }

    /// <summary>
    /// Called at the start of every round. If this is the last round of the cycle,
    /// opens the vote (guarded so it can only run once per cycle).
    /// </summary>
    public void OnRoundStart()
    {
        if (!_settings.Enabled || !_settings.StartOnLastRound) return;
        if (_voteActive || _voteCompleted) return;

        if (!IsLastRound()) return;

        StartVote();
    }

    /// <summary>
    /// Called when a round ends. If a winning map is pending, performs the map
    /// change now (after the round, never mid-round).
    /// </summary>
    public void OnRoundEnd()
    {
        if (_pendingMap == null || _changeQueued) return;
        ChangeMap(_pendingMap);
    }

    private bool IsLastRound()
    {
        var gameRules = Utils.GameRulesHelper.GetGameRulesOrNull();
        if (gameRules == null) return false;
        if (gameRules.WarmupPeriod) return false;

        var maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
        if (maxRounds <= 0) return false;

        // TotalRoundsPlayed is the number of completed rounds. On the last round's
        // start it equals maxRounds - 1.
        var played = gameRules.TotalRoundsPlayed;
        return played >= maxRounds - 1;
    }

    private void StartVote()
    {
        // Guard: never start twice.
        if (_voteActive || _voteCompleted) return;

        _voteActive = true;
        _votes.Clear();

        Utils.Logger.LogInfo("AutoEndVote", $"Starting last-round map vote ({_settings.Maps.Count} maps)");
        Server.PrintToChatAll($"{Prefix}Ostatnia runda — głosowanie na następną mapę! Wpisz wybór w menu.");

        foreach (var player in Utilities.GetPlayers())
        {
            if (IsRealPlayer(player)) _menus.OpenRoot(player, ShowVoteMenu);
        }

        _plugin.AddTimer(Math.Max(5.0f, _settings.VoteDurationSeconds), FinishVote);
    }

    private void ShowVoteMenu(CCSPlayerController player)
    {
        _menus.Show(player, $"{ChatColors.Green}Następna mapa{ChatColors.White}", menu =>
        {
            foreach (var map in _settings.Maps)
            {
                var captured = map;
                menu.AddOption(captured, p =>
                {
                    if (!_voteActive) return;
                    _votes[p.SteamID] = captured;   // one vote per SteamID; re-vote overwrites
                    p.PrintToChat($"{Prefix}Zagłosowałeś na: {ChatColors.Gold}{captured}");

                    var steamId = p.SteamID;
                    _plugin.AddTimer(0.1f, () =>
                    {
                        var target = Utilities.GetPlayers().FirstOrDefault(x => x.IsValid && x.SteamID == steamId);
                        if (target != null) _menus.Close(target);
                    });
                });
            }
        }, ShowVoteMenu);
    }

    private void FinishVote()
    {
        if (!_voteActive) return;
        _voteActive = false;
        _voteCompleted = true;

        Utils.Logger.LogInfo("AutoEndVote", $"Vote finished, votes={_votes.Count}");

        // Requirement: if nobody voted, do not change the map.
        if (_votes.Count == 0)
        {
            Server.PrintToChatAll($"{Prefix}Nikt nie zagłosował — mapa zostaje bez zmian.");
            return;
        }

        var winner = _votes.Values
            .GroupBy(m => m)
            .Select(g => new { Map = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Tie -> random among the tied maps.
        var topCount = winner[0].Count;
        var tied = winner.Where(x => x.Count == topCount).Select(x => x.Map).ToList();
        var chosen = tied[_random.Next(tied.Count)];

        _pendingMap = chosen;
        Server.PrintToChatAll($"{Prefix}Wygrała mapa: {ChatColors.Gold}{chosen}{ChatColors.White} — zmiana po tej rundzie.");
        Utils.Logger.LogInfo("AutoEndVote", $"Pending map set: {chosen}");
    }

    private void ChangeMap(string mapName)
    {
        // Guard: changelevel must not run twice; do nothing if already changing.
        if (_changeQueued) return;
        _changeQueued = true;

        Utils.Logger.LogInfo("AutoEndVote", $"Round ended — changing to {mapName}");
        Server.PrintToChatAll($"{Prefix}Zmiana mapy na {ChatColors.Gold}{mapName}{ChatColors.White}...");

        // Freeze plugin logic + close menus + stop timers (shared safe flow).
        OnBeginMapChange?.Invoke();

        // Remove the live bomb / projectiles while entities are still valid.
        RemoveDangerousEntities();

        // Defer the changelevel: AddTimer -> NextFrame on the main thread. This is
        // the execution context that does not fault the engine on unload.
        _plugin.AddTimer(3.0f, () =>
        {
            Server.NextFrame(() =>
            {
                Utils.Logger.LogInfo("AutoEndVote", $"Executing: changelevel {mapName}");
                Server.ExecuteCommand($"changelevel {mapName}");
            });
        });
    }

    private static void RemoveDangerousEntities()
    {
        try
        {
            foreach (var bomb in Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4"))
            {
                if (bomb is { IsValid: true }) bomb.Remove();
            }

            foreach (var designer in new[] { "hegrenade_projectile", "molotov_projectile",
                         "smokegrenade_projectile", "flashbang_projectile", "decoy_projectile", "inferno" })
            {
                foreach (var ent in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(designer))
                {
                    if (ent is { IsValid: true }) ent.Remove();
                }
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.LogWarning("AutoEndVote", $"Entity cleanup failed: {ex.Message}");
        }
    }

    private static bool IsRealPlayer(CCSPlayerController? p) =>
        p is { IsValid: true, IsBot: false, IsHLTV: false };
}
