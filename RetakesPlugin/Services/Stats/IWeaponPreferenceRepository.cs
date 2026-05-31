using RetakesPlugin.Models;

namespace RetakesPlugin.Services.Stats;

/// <summary>
/// Storage for per-player weapon preferences, so a player's !guns choice
/// survives map changes and reconnects.
/// </summary>
public interface IWeaponPreferenceRepository
{
    /// <summary>Creates the schema if it does not exist. Throws on failure.</summary>
    Task InitializeAsync();

    /// <summary>Loads a player's saved preference, or null if they have none.</summary>
    Task<WeaponPreference?> LoadAsync(ulong steamId);

    /// <summary>Inserts or updates a player's preference.</summary>
    Task SaveAsync(ulong steamId, WeaponPreference preference);

    /// <summary>Deletes a player's saved preference (reset to random).</summary>
    Task DeleteAsync(ulong steamId);
}
