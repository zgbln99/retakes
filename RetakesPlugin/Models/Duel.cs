namespace RetakesPlugin.Models;

/// <summary>
/// An increment to a player-vs-player kill record: <see cref="KillerSteamId"/>
/// killed <see cref="VictimSteamId"/> <see cref="Kills"/> times (of which
/// <see cref="Headshots"/> were headshots) since the last flush.
/// </summary>
public class DuelDelta
{
    public ulong KillerSteamId { get; set; }
    public ulong VictimSteamId { get; set; }
    public string KillerName { get; set; } = "";
    public string VictimName { get; set; } = "";
    public int Kills { get; set; }
    public int Headshots { get; set; }
}

/// <summary>
/// One row of a player's PvP record: how many times they killed an opponent and
/// were killed by them. From the querying player's perspective.
/// </summary>
public class DuelRow
{
    public ulong OpponentSteamId { get; set; }
    public string OpponentName { get; set; } = "";
    /// <summary>Times the querying player killed this opponent.</summary>
    public int Kills { get; set; }
    /// <summary>Times this opponent killed the querying player.</summary>
    public int Deaths { get; set; }
}
