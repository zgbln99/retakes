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

/// <summary>
/// A player's "nemesis" (opponent who killed them most) and "victim" (opponent
/// they killed most), derived from their duel rows.
/// </summary>
public class NemesisInfo
{
    public DuelRow? Nemesis { get; set; }   // most deaths to this opponent
    public DuelRow? Victim { get; set; }    // most kills on this opponent

    public static NemesisInfo From(IEnumerable<DuelRow> duels)
    {
        var info = new NemesisInfo();
        foreach (var d in duels)
        {
            if (d.Deaths > 0 && (info.Nemesis == null || d.Deaths > info.Nemesis.Deaths))
                info.Nemesis = d;
            if (d.Kills > 0 && (info.Victim == null || d.Kills > info.Victim.Kills))
                info.Victim = d;
        }
        return info;
    }
}
