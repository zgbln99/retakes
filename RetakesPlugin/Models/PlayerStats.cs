namespace RetakesPlugin.Models;

/// <summary>
/// PvP statistics for a single player. Held in memory while the player is on the
/// server and periodically flushed to the database.
/// </summary>
public class PlayerStats
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = "";

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Headshots { get; set; }
    public int Assists { get; set; }
    public int RoundsPlayed { get; set; }

    /// <summary>The in-memory values changed and need flushing to the DB.</summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// The player's stored totals have been merged in from the database. Entries
    /// are only flushed once loaded, so we never overwrite stored totals with a
    /// session-only count.
    /// </summary>
    public bool Loaded { get; set; }

    /// <summary>A database load is currently in flight for this entry.</summary>
    public bool Loading { get; set; }

    public double KillDeathRatio => Deaths == 0 ? Kills : Math.Round((double)Kills / Deaths, 2);

    public double HeadshotPercentage => Kills == 0 ? 0 : Math.Round((double)Headshots / Kills * 100, 1);
}
