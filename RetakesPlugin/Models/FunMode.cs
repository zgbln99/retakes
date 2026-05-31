namespace RetakesPlugin.Models;

/// <summary>
/// Symmetric, server-wide fun modes. Every player is affected equally and the
/// active mode is announced, so these are game modes, not advantages. Weapon
/// modes are applied by the weapon allocator; LowGravity is applied via a cvar.
/// </summary>
public enum FunMode
{
    /// <summary>Normal allocation, normal gravity.</summary>
    None,

    /// <summary>Everyone gets only a knife.</summary>
    KnivesOnly,

    /// <summary>Everyone gets a Desert Eagle (+knife).</summary>
    DeagleOnly,

    /// <summary>Everyone gets HE grenades (+knife) — grenade war.</summary>
    HeWar,

    /// <summary>Everyone gets a scout/SSG08 (+knife).</summary>
    ScoutsOnly,

    /// <summary>Normal weapons, but low gravity for everyone.</summary>
    LowGravity
}

public static class FunModeExtensions
{
    public static string DisplayName(this FunMode mode) => mode switch
    {
        FunMode.None => "Wyłączony (normalna gra)",
        FunMode.KnivesOnly => "Tylko noże",
        FunMode.DeagleOnly => "Tylko Deagle",
        FunMode.HeWar => "Wojna granatami (HE)",
        FunMode.ScoutsOnly => "Tylko Scout",
        FunMode.LowGravity => "Niska grawitacja",
        _ => mode.ToString()
    };

    /// <summary>True if the mode changes weapon allocation (handled by the allocator).</summary>
    public static bool IsWeaponMode(this FunMode mode) =>
        mode is FunMode.KnivesOnly or FunMode.DeagleOnly or FunMode.HeWar or FunMode.ScoutsOnly;
}
