using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;
using RetakesPlugin.Services.Stats;

namespace RetakesPlugin.Services;

/// <summary>
/// Records PvP statistics. Counting happens on the game thread (cheap in-memory
/// increments); all database I/O runs asynchronously off the game thread so it
/// never stalls the server. If the database is unreachable the module disables
/// itself gracefully and the game is unaffected.
///
/// A player's stored totals are merged in lazily on first interaction; entries
/// are only ever flushed once that load has completed, so a session count can
/// never overwrite a player's stored totals.
/// </summary>
public class StatsService
{
    private readonly BasePlugin _plugin;
    private readonly StatsSettings _settings;
    private readonly IStatsRepository _repository;

    private readonly ConcurrentDictionary<ulong, PlayerStats> _cache = new();
    private readonly object _lock = new();

    private bool _databaseReady;
    private volatile bool _stopped;

    public StatsService(BasePlugin plugin, StatsSettings settings, IStatsRepository repository)
    {
        _plugin = plugin;
        _settings = settings;
        _repository = repository;
    }

    private bool Active => _settings.IsEnabled && _databaseReady && !_stopped;

    public bool IsReady => Active;

    public void Initialize()
    {
        if (!_settings.IsEnabled)
        {
            Utils.Logger.LogInfo("Stats", "Stats disabled in config");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await _repository.InitializeAsync();
                _databaseReady = true;
                Utils.Logger.LogInfo("Stats", "Database initialized");
            }
            catch (Exception ex)
            {
                _databaseReady = false;
                Utils.Logger.LogWarning("Stats", $"Database init failed, stats disabled: {ex.Message}");
            }
        });

        var interval = Math.Max(15.0f, _settings.FlushIntervalSeconds);
        _plugin.AddTimer(interval, FlushDirty, TimerFlags.REPEAT);
    }

    /// <summary>
    /// Stops periodic work (called when a map change starts). Performs one final
    /// synchronous-style flush request so nothing is lost, then goes quiet.
    /// </summary>
    public void StopTimers()
    {
        if (_stopped) return;
        FlushDirty();   // final flush while still Active
        _stopped = true;
    }

    #region Game-thread counting
    public void OnPlayerConnect(ulong steamId, string name)
    {
        if (!Active || steamId == 0) return;
        lock (_lock) Get(steamId, name);
    }

    public void OnPlayerDeath(EventPlayerDeath @event)
    {
        if (!Active) return;

        var victim = @event.Userid;
        var attacker = @event.Attacker;
        var assister = @event.Assister;

        var victimId = GetHumanSteamId(victim);
        var attackerId = GetHumanSteamId(attacker);
        var assisterId = GetHumanSteamId(assister);

        lock (_lock)
        {
            if (victimId != 0)
            {
                var stats = Get(victimId, victim!.PlayerName);
                stats.Deaths++;
                stats.IsDirty = true;
            }

            if (attackerId != 0 && attackerId != victimId)
            {
                var stats = Get(attackerId, attacker!.PlayerName);
                stats.Kills++;
                if (@event.Headshot) stats.Headshots++;
                stats.IsDirty = true;
            }

            if (assisterId != 0 && assisterId != attackerId && assisterId != victimId)
            {
                var stats = Get(assisterId, assister!.PlayerName);
                stats.Assists++;
                stats.IsDirty = true;
            }
        }
    }

    public void OnRoundEnd()
    {
        if (!Active) return;

        lock (_lock)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                var steamId = GetHumanSteamId(player);
                if (steamId == 0) continue;
                if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist) continue;

                var stats = Get(steamId, player.PlayerName);
                stats.RoundsPlayed++;
                stats.IsDirty = true;
            }
        }
    }

    public void OnPlayerDisconnect(ulong steamId)
    {
        if (steamId == 0) return;
        if (!_cache.TryRemove(steamId, out var stats)) return;
        if (!Active || !stats.IsDirty || !stats.Loaded) return;

        var snapshot = Clone(stats);
        Task.Run(async () =>
        {
            try
            {
                await _repository.SaveBatchAsync(new[] { snapshot });
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Stats", $"Save on disconnect failed for {steamId}: {ex.Message}");
            }
        });
    }
    #endregion

    #region Reads
    public PlayerStats? GetCached(ulong steamId) =>
        _cache.TryGetValue(steamId, out var stats) ? stats : null;

    public async Task<List<PlayerStats>> GetTopAsync() =>
        await _repository.GetTopAsync(_settings.LeaderboardSize);
    #endregion

    private void FlushDirty()
    {
        if (!Active) return;

        List<PlayerStats> snapshot;
        lock (_lock)
        {
            snapshot = _cache.Values.Where(s => s is { IsDirty: true, Loaded: true }).Select(Clone).ToList();
            foreach (var stats in _cache.Values)
            {
                if (stats.Loaded) stats.IsDirty = false;
            }
        }

        if (snapshot.Count == 0) return;

        Task.Run(async () =>
        {
            try
            {
                await _repository.SaveBatchAsync(snapshot);
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Stats", $"Flush failed: {ex.Message}");
            }
        });
    }

    // Must be called under _lock.
    private PlayerStats Get(ulong steamId, string name)
    {
        var entry = _cache.GetOrAdd(steamId, _ => new PlayerStats { SteamId = steamId, Name = name });
        if (!string.IsNullOrEmpty(name)) entry.Name = name;
        EnsureLoaded(entry);
        return entry;
    }

    // Must be called under _lock.
    private void EnsureLoaded(PlayerStats entry)
    {
        if (!_databaseReady || entry.Loaded || entry.Loading) return;
        entry.Loading = true;

        var steamId = entry.SteamId;
        Task.Run(async () =>
        {
            try
            {
                var loaded = await _repository.LoadAsync(steamId);
                lock (_lock)
                {
                    if (loaded != null)
                    {
                        entry.Kills += loaded.Kills;
                        entry.Deaths += loaded.Deaths;
                        entry.Headshots += loaded.Headshots;
                        entry.Assists += loaded.Assists;
                        entry.RoundsPlayed += loaded.RoundsPlayed;
                    }
                    entry.Loaded = true;
                    entry.Loading = false;
                }
            }
            catch (Exception ex)
            {
                lock (_lock) entry.Loading = false;
                Utils.Logger.LogWarning("Stats", $"Load failed for {steamId}: {ex.Message}");
            }
        });
    }

    private static ulong GetHumanSteamId(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return 0;
        var steamId = player.SteamID;
        return steamId > 0 ? steamId : 0;
    }

    private static PlayerStats Clone(PlayerStats s) => new()
    {
        SteamId = s.SteamId,
        Name = s.Name,
        Kills = s.Kills,
        Deaths = s.Deaths,
        Headshots = s.Headshots,
        Assists = s.Assists,
        RoundsPlayed = s.RoundsPlayed,
        Loaded = s.Loaded
    };
}
