using RetakesPlugin.Models;

namespace RetakesPlugin.Services.Stats;

/// <summary>
/// Storage abstraction for PvP statistics. A MySQL implementation is provided;
/// the interface keeps the door open for a SQLite fallback later.
/// </summary>
public interface IStatsRepository
{
    /// <summary>Creates the schema if it does not exist. Throws on failure.</summary>
    Task InitializeAsync();

    /// <summary>Loads a single player's totals, or null if they have none yet.</summary>
    Task<PlayerStats?> LoadAsync(ulong steamId);

    /// <summary>Upserts a batch of player totals (absolute values).</summary>
    Task SaveBatchAsync(IReadOnlyCollection<PlayerStats> stats);

    /// <summary>Returns the top players ordered by kills.</summary>
    Task<List<PlayerStats>> GetTopAsync(int limit);

    /// <summary>Adds player-vs-player kill counts (killer-&gt;victim, with headshots).</summary>
    Task SaveDuelsAsync(IReadOnlyCollection<DuelDelta> duels);

    /// <summary>Returns one player's PvP record vs everyone they fought.</summary>
    Task<List<DuelRow>> GetDuelsAsync(ulong steamId);

    /// <summary>Adds StatTrak kill counts per (player, weapon).</summary>
    Task SaveStatTrakAsync(IReadOnlyCollection<StatTrakDelta> deltas);

    /// <summary>Returns a player's StatTrak weapon counters (most-used first).</summary>
    Task<List<StatTrakRow>> GetStatTrakAsync(ulong steamId, int limit);

    /// <summary>Resets a player's StatTrak counters (or all if steamId is 0).</summary>
    Task ResetStatTrakAsync(ulong steamId);
}
