namespace RetakesPlugin.Models;

/// <summary>An increment to a player's StatTrak counter for a weapon.</summary>
public class StatTrakDelta
{
    public ulong SteamId { get; set; }
    public string Weapon { get; set; } = "";
    public int Kills { get; set; }
}

/// <summary>A player's StatTrak counter for one weapon.</summary>
public class StatTrakRow
{
    public string Weapon { get; set; } = "";
    public int Kills { get; set; }
}
