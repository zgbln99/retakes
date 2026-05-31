using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Player-driven map vote. Players type !rtv to request a change; once the
/// configured ratio of connected players agree, a vote opens for everyone and
/// the winning map is loaded. Admins can also force a vote from the panel.
/// </summary>
public class MapVoteService
{
    private readonly BasePlugin _plugin;
    private readonly MapVoteSettings _settings;
    private readonly Random _random;

    private readonly HashSet<ulong> _rtvVoters = new();
    private readonly Dictionary<ulong, string> _votes = new();
    private List<string> _candidates = new();
    private bool _voteActive;

    public MapVoteService(BasePlugin plugin, MapVoteSettings settings, Random random)
    {
        _plugin = plugin;
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

    public void OnRtv(CCSPlayerController player)
    {
        if (!_settings.IsEnabled)
        {
            player.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Głosowanie na mapę jest wyłączone.");
            return;
        }

        if (_voteActive)
        {
            player.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Głosowanie już trwa.");
            return;
        }

        var steamId = player.SteamID;
        if (!_rtvVoters.Add(steamId))
        {
            player.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Już zagłosowałeś za zmianą mapy.");
            return;
        }

        var needed = RequiredVotes();
        var have = _rtvVoters.Count;

        Server.PrintToChatAll(
            $" {ChatColors.Green}[Retakes]{ChatColors.White} {player.PlayerName} chce zmienić mapę " +
            $"({ChatColors.Gold}{have}/{needed}{ChatColors.White}). Wpisz {ChatColors.Green}!rtv{ChatColors.White}.");

        if (have >= needed)
        {
            StartVote();
        }
    }

    /// <summary>Forces a vote to start immediately (admin panel).</summary>
    public void ForceStartVote(CCSPlayerController admin)
    {
        if (!_settings.IsEnabled)
        {
            admin.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Głosowanie na mapę jest wyłączone.");
            return;
        }

        if (_voteActive)
        {
            admin.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Głosowanie już trwa.");
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

        Server.PrintToChatAll($" {ChatColors.Green}[Retakes]{ChatColors.White} Rozpoczęto głosowanie na mapę!");

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && !player.IsBot)
            {
                OpenVoteMenu(player);
            }
        }

        _plugin.AddTimer(Math.Max(5.0f, _settings.VoteDurationSeconds), FinishVote);
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
        var menu = new CenterHtmlMenu($"{ChatColors.Green}Głosowanie na mapę{ChatColors.White}", _plugin);

        foreach (var map in _candidates)
        {
            var captured = map;
            menu.AddMenuOption(captured, (p, _) =>
            {
                _votes[p.SteamID] = captured;
                p.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Zagłosowałeś na: {ChatColors.Gold}{captured}");
                MenuManager.CloseActiveMenu(p);
            });
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
    }

    private void FinishVote()
    {
        if (!_voteActive) return;
        _voteActive = false;

        string winner;
        if (_votes.Count == 0)
        {
            // Nobody voted — pick a random candidate so the !rtv still does something.
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

        Server.PrintToChatAll(
            $" {ChatColors.Green}[Retakes]{ChatColors.White} Wygrała mapa: {ChatColors.Gold}{winner}{ChatColors.White}. Zmiana za {(int)_settings.ChangeDelaySeconds}s...");

        _rtvVoters.Clear();

        _plugin.AddTimer(Math.Max(1.0f, _settings.ChangeDelaySeconds), () =>
        {
            Server.ExecuteCommand($"changelevel {winner}");
        });
    }
}
