using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Player-driven map vote. Players type !rtv to request a change; once the
/// configured ratio of connected players agree, a vote opens for everyone and
/// the winning map is loaded. Admins can also force a vote from the panel.
/// </summary>
public class MapVoteService
{
    private readonly BasePlugin _plugin;
    private readonly MenuService _menus;
    private readonly MapVoteSettings _settings;
    private readonly Random _random;

    private readonly HashSet<ulong> _rtvVoters = new();
    private readonly Dictionary<ulong, string> _votes = new();
    private List<string> _candidates = new();
    private bool _voteActive;

    /// <summary>
    /// Invoked right before the map changes, so the plugin can freeze its game
    /// logic (set _isChangingMap) and stop timers before ChangeLevel.
    /// </summary>
    public Action? OnBeginMapChange { get; set; }

    public MapVoteService(BasePlugin plugin, MenuService menus, MapVoteSettings settings, Random random)
    {
        _plugin = plugin;
        _menus = menus;
        _settings = settings;
        _random = random;
    }

    public bool IsEnabled => _settings.IsEnabled;

    /// <summary>Clears state on map start.</summary>
    public void Reset()
    {
        _rtvVoters.Clear();
        _votes.Clear();
        _candidates.Clear();
        _voteActive = false;
    }

    /// <summary>Opens the end-of-match map vote (a team won the match).</summary>
    public void OnMatchEnd()
    {
        Utils.Logger.LogInfo("MapVote", $"OnMatchEnd: enabled={_settings.IsEnabled}, startAtMatchEnd={_settings.StartAtMatchEnd}, voteActive={_voteActive}");
        if (!_settings.IsEnabled || !_settings.StartAtMatchEnd || _voteActive) return;

        Server.PrintToChatAll($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Koniec gry — głosowanie na następną mapę!");
        StartVote();
    }

    public void OnRtv(CCSPlayerController player)
    {
        Utils.Logger.LogInfo("MapVote", $"OnRtv by {player.PlayerName}: enabled={_settings.IsEnabled}, allowRtv={_settings.AllowRtv}, voteActive={_voteActive}");

        if (!_settings.IsEnabled || !_settings.AllowRtv)
        {
            player.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Głosowanie !rtv jest wyłączone (mapa zmienia się na końcu gry).");
            return;
        }

        if (_voteActive)
        {
            player.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Głosowanie już trwa.");
            return;
        }

        var steamId = player.SteamID;
        if (!_rtvVoters.Add(steamId))
        {
            player.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Już zagłosowałeś za zmianą mapy.");
            return;
        }

        var needed = RequiredVotes();
        var have = _rtvVoters.Count;

        Server.PrintToChatAll(
            $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} {player.PlayerName} chce zmienić mapę " +
            $"({ChatColors.Gold}{have}/{needed}{ChatColors.White}). Wpisz {ChatColors.Green}!rtv{ChatColors.White}.");

        if (have >= needed)
        {
            StartVote();
        }
    }

    /// <summary>Forces a vote to start immediately (admin panel). Ignores AllowRtv.</summary>
    public void ForceStartVote(CCSPlayerController admin)
    {
        if (!_settings.IsEnabled)
        {
            admin.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Głosowanie na mapę jest wyłączone.");
            return;
        }

        if (_voteActive)
        {
            admin.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Głosowanie już trwa.");
            return;
        }

        StartVote();
    }

    private int RequiredVotes()
    {
        var players = ConnectedHumanCount();
        if (players <= 0) return 1;
        return Math.Max(1, (int)Math.Ceiling(players * _settings.RtvRatio));
    }

    private static int ConnectedHumanCount()
    {
        var count = 0;
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && !player.IsBot && !player.IsHLTV) count++;
        }
        return count;
    }

    private void StartVote()
    {
        _voteActive = true;
        _votes.Clear();
        _candidates = PickCandidates();

        Utils.Logger.LogInfo("MapVote", $"StartVote: {_candidates.Count} candidates: {string.Join(", ", _candidates)}");

        if (_candidates.Count == 0)
        {
            Utils.Logger.LogWarning("MapVote", "No candidate maps configured — aborting vote. Check MapVoteSettings.Maps.");
            _voteActive = false;
            return;
        }

        Server.PrintToChatAll($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Rozpoczęto głosowanie na mapę!");

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && !player.IsBot)
            {
                OpenVoteMenu(player);
            }
        }

        _plugin.AddTimer(Math.Max(5.0f, _settings.VoteDurationSeconds), FinishVote);
        Utils.Logger.LogInfo("MapVote", $"Vote menu opened, finishing in {Math.Max(5.0f, _settings.VoteDurationSeconds)}s");
    }

    private List<string> PickCandidates()
    {
        var current = Server.MapName;
        var pool = _settings.Maps
            .Where(m => !m.Equals(current, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(_ => _random.Next())
            .Take(Math.Max(2, _settings.MapsInVote))
            .ToList();

        // Fallback if the config pool is too small.
        if (pool.Count == 0) pool = _settings.Maps.Distinct().Take(Math.Max(2, _settings.MapsInVote)).ToList();
        return pool;
    }

    private void OpenVoteMenu(CCSPlayerController player)
    {
        _menus.OpenRoot(player, ShowVoteMenu);
    }

    private void ShowVoteMenu(CCSPlayerController player)
    {
        _menus.Show(player, $"{ChatColors.Green}Głosowanie na mapę{ChatColors.White}", menu =>
        {
            foreach (var map in _candidates)
            {
                var captured = map;
                menu.AddOption(captured, p =>
                {
                    try
                    {
                        _votes[p.SteamID] = captured;
                        Utils.Logger.LogInfo("MapVote", $"{p.PlayerName} voted for {captured}");
                        p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Zagłosowałeś na: {ChatColors.Gold}{captured}");

                        // Close on the next frame — closing from inside the option
                        // callback can crash the server (menu re-entrancy).
                        var steamId = p.SteamID;
                        _plugin.AddTimer(0.1f, () =>
                        {
                            var target = Utilities.GetPlayers().FirstOrDefault(x => x.IsValid && x.SteamID == steamId);
                            if (target != null) _menus.Close(target);
                        });
                    }
                    catch (Exception ex)
                    {
                        Utils.Logger.LogException("MapVote", ex);
                    }
                });
            }
        }, ShowVoteMenu);
    }

    private void FinishVote()
    {
        Utils.Logger.LogInfo("MapVote", $"FinishVote: voteActive={_voteActive}, votes={_votes.Count}");
        if (!_voteActive) return;
        _voteActive = false;

        string winner;
        if (_votes.Count == 0)
        {
            // Nobody voted — pick a random candidate so the vote still does something.
            winner = _candidates.Count > 0 ? _candidates[_random.Next(_candidates.Count)] : Server.MapName;
        }
        else
        {
            winner = _votes.Values
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .ThenBy(_ => _random.Next())
                .First().Key;
        }

        Utils.Logger.LogInfo("MapVote", $"Winner: {winner} — changing in {(int)_settings.ChangeDelaySeconds}s");

        Server.PrintToChatAll(
            $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Wygrała mapa: {ChatColors.Gold}{winner}{ChatColors.White}. Zmiana za {(int)_settings.ChangeDelaySeconds}s...");

        _rtvVoters.Clear();

        // Freeze the plugin NOW, the moment the vote ends — not at changelevel.
        // Otherwise retakes keeps running during the delay and starts a new round
        // (auto-planting a ticking bomb), and changelevel with a live planted_c4
        // crashes the engine during unload.
        OnBeginMapChange?.Invoke();

        _plugin.AddTimer(Math.Max(1.0f, _settings.ChangeDelaySeconds), () =>
        {
            ChangeMap(winner);
        });
    }

    /// <summary>
    /// Changes the map. Manual changelevel works fine, but doing it while a round
    /// is live (auto-planted ticking C4 present) crashes the engine on unload. So
    /// we first terminate the round and strip dangerous entities to reach the same
    /// clean state a manual changelevel runs from, then change the map a moment
    /// later. Plugin logic is already frozen by FinishVote.
    /// </summary>
    private void ChangeMap(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            Utils.Logger.LogWarning("MapVote", "Empty map name, skipping change");
            return;
        }

        Utils.Logger.LogInfo("MapVote", "Preparing clean state (end round + remove entities)");

        // 1) End the round so the engine tears the bomb down naturally.
        try
        {
            GameRulesHelper.TerminateRound(
                CounterStrikeSharp.API.Modules.Entities.Constants.RoundEndReason.RoundDraw);
        }
        catch (Exception ex)
        {
            Utils.Logger.LogWarning("MapVote", $"TerminateRound failed (continuing): {ex.Message}");
        }

        // 2) Belt-and-braces: remove any remaining planted bomb / live projectiles.
        RemoveDangerousEntities();

        // 3) Change the map a short delay later, from the now-clean state.
        _plugin.AddTimer(1.5f, () =>
        {
            Utils.Logger.LogInfo("MapVote", $"Executing: changelevel {mapName}");
            Server.ExecuteCommand($"changelevel {mapName}");
        });
    }

    /// <summary>
    /// Removes planted C4 and any in-flight projectiles so the engine doesn't fault
    /// on a live timed entity during the level change.
    /// </summary>
    private static void RemoveDangerousEntities()
    {
        try
        {
            var removedBombs = 0;
            foreach (var bomb in Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4"))
            {
                if (bomb is { IsValid: true }) { bomb.Remove(); removedBombs++; }
            }

            foreach (var designer in new[] { "hegrenade_projectile", "molotov_projectile",
                         "smokegrenade_projectile", "flashbang_projectile", "decoy_projectile",
                         "inferno" })
            {
                foreach (var ent in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(designer))
                {
                    if (ent is { IsValid: true }) ent.Remove();
                }
            }

            Utils.Logger.LogInfo("MapVote", $"Entity cleanup done (planted bombs removed: {removedBombs})");
        }
        catch (Exception ex)
        {
            Utils.Logger.LogWarning("MapVote", $"Entity cleanup before changelevel failed: {ex.Message}");
        }
    }
}
