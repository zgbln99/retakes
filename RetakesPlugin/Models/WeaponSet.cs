using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Models;

/// <summary>
/// A preset weapon loadout that an admin can force globally from the panel. It is
/// symmetric: every player on a given team receives the same kind of loadout, so
/// it is a game mode, not an advantage. Primary/Pistol return the item name to
/// give for the supplied team (Primary may return null for pistol-only sets).
/// </summary>
public sealed class WeaponSet
{
    public string Key { get; }
    public string DisplayName { get; }
    public Func<CsTeam, Random, string?> Primary { get; }
    public Func<CsTeam, Random, string?> Pistol { get; }

    public WeaponSet(
        string key,
        string displayName,
        Func<CsTeam, Random, string?> primary,
        Func<CsTeam, Random, string?> pistol)
    {
        Key = key;
        DisplayName = displayName;
        Primary = primary;
        Pistol = pistol;
    }
}
