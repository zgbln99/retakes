using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Tracks match-scoped per-player stats (kills, damage, clutches, rounds played)
/// and shows an end-of-match summary screen (center HTML) with MVP, top fragger,
/// best ADR, most clutches and the next map. Independent of the DB stats module —
/// these counters reset every map.
/// </summary>
public class EndGameScreenService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly BasePlugin _plugin;
    private readonly EndGameScreenSettings _settings;

    private sealed class Match
    {
        public string Name = "";
        public int Kills;
        public int Damage;
        public int Clutches;
    }

    private readonly Dictionary<ulong, Match> _match = new();

    // Clutch tracking (one in flight at a time, like HighlightService).
    private ulong _clutcherSteamId;
    private CsTeam _clutcherTeam = CsTeam.None;

    public EndGameScreenService(BasePlugin plugin, EndGameScreenSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public bool Enabled => _settings.Enabled;

    public void ResetMatch()
    {
        _match.Clear();
        _clutcherSteamId = 0;
        _clutcherTeam = CsTeam.None;
    }

    public void OnRoundStart()
    {
        _clutcherSteamId = 0;
        _clutcherTeam = CsTeam.None;

        // Count a round played for everyone on a team.
        foreach (var p in Utilities.GetPlayers())
        {
            var id = SteamId(p);
            if (id == 0) continue;
            if (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist) Entry(id, p.PlayerName);
        }
    }

    public void OnPlayerHurt(EventPlayerHurt @event)
    {
        if (!_settings.Enabled) return;
        var attacker = @event.Attacker;
        var id = SteamId(attacker);
        if (id == 0 || attacker == null) return;
        if (attacker.Team == @event.Userid?.Team) return; // ignore team damage

        Entry(id, attacker.PlayerName).Damage += Math.Max(0, @event.DmgHealth);
    }

    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        if (!_settings.Enabled) return;

        var attacker = @event.Attacker;
        var aid = SteamId(attacker);
        var vid = SteamId(@event.Userid);
        if (aid != 0 && aid != vid && attacker != null) Entry(aid, attacker.PlayerName).Kills++;

        DetectClutchStart();
    }

    private void DetectClutchStart()
    {
        if (_clutcherSteamId != 0) return;
        var (tAlive, tLone) = Alive(CsTeam.Terrorist);
        var (ctAlive, ctLone) = Alive(CsTeam.CounterTerrorist);

        if (tAlive == 1 && ctAlive >= 2 && tLone != null) { _clutcherSteamId = SteamId(tLone); _clutcherTeam = CsTeam.Terrorist; }
        else if (ctAlive == 1 && tAlive >= 2 && ctLone != null) { _clutcherSteamId = SteamId(ctLone); _clutcherTeam = CsTeam.CounterTerrorist; }
    }

    public void OnRoundEnd(CsTeam winner)
    {
        if (!_settings.Enabled) return;
        if (_clutcherSteamId != 0 && _clutcherTeam == winner && _match.TryGetValue(_clutcherSteamId, out var m))
        {
            m.Clutches++;
        }
    }

    /// <summary>Shows the end screen. <paramref name="nextMap"/> may be null/empty.</summary>
    public void ShowEndScreen(string? nextMap)
    {
        if (!_settings.Enabled || _match.Count == 0) return;

        var rounds = Math.Max(1, RoundsPlayed());
        var best = _match.Values.OrderByDescending(m => m.Kills).ThenByDescending(m => m.Damage).FirstOrDefault();
        var topFrag = _match.Values.OrderByDescending(m => m.Kills).FirstOrDefault();
        var bestAdr = _match.Values.OrderByDescending(m => m.Damage).FirstOrDefault();
        var mostClutch = _match.Values.OrderByDescending(m => m.Clutches).FirstOrDefault();

        var lines = new List<string> { "<font color='#40ff40'>KONIEC MECZU</font>" };

        if (_settings.ShowBestPlayer && best != null)
            lines.Add($"<font color='#ffd000'>MVP:</font> {Esc(best.Name)} ({best.Kills} zab.)");
        if (_settings.ShowTopFragger && topFrag != null)
            lines.Add($"<font color='#ffffff'>Top fragger:</font> {Esc(topFrag.Name)} ({topFrag.Kills})");
        if (_settings.ShowBestAdr && bestAdr != null)
            lines.Add($"<font color='#ffffff'>Najlepszy ADR:</font> {Esc(bestAdr.Name)} ({bestAdr.Damage / rounds})");
        if (_settings.ShowMostClutches && mostClutch is { Clutches: > 0 })
            lines.Add($"<font color='#ffffff'>Najwięcej clutchy:</font> {Esc(mostClutch.Name)} ({mostClutch.Clutches})");
        if (_settings.ShowNextMap && !string.IsNullOrWhiteSpace(nextMap))
            lines.Add($"<font color='#40c0ff'>Następna mapa:</font> {Esc(nextMap!)}");

        var html = string.Join("<br>", lines);

        // Repaint a few times so the center text stays visible for the duration.
        var ticks = (int)Math.Max(1, _settings.DurationSeconds);
        for (var i = 0; i < ticks; i++)
        {
            _plugin.AddTimer(i, () =>
            {
                foreach (var p in Utilities.GetPlayers())
                    if (p.IsValid && !p.IsBot) p.PrintToCenterHtml(html);
            });
        }
    }

    private int RoundsPlayed()
    {
        var gr = Utils.GameRulesHelper.GetGameRulesOrNull();
        return gr?.TotalRoundsPlayed ?? 1;
    }

    private Match Entry(ulong id, string name)
    {
        if (!_match.TryGetValue(id, out var m)) { m = new Match { Name = name }; _match[id] = m; }
        if (!string.IsNullOrEmpty(name)) m.Name = name;
        return m;
    }

    private static (int alive, CCSPlayerController? lone) Alive(CsTeam team)
    {
        var alive = 0; CCSPlayerController? lone = null;
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid || p.Team != team || !p.PawnIsAlive) continue;
            alive++; lone = p;
        }
        return (alive, alive == 1 ? lone : null);
    }

    private static ulong SteamId(CCSPlayerController? p)
    {
        if (p == null || !p.IsValid || p.IsBot || p.IsHLTV) return 0;
        return p.SteamID > 0 ? p.SteamID : 0;
    }

    private static string Esc(string s) =>
        s.Replace("<", "").Replace(">", "");
}
