namespace RetakesPlugin.Models;

/// <summary>
/// A runtime on/off switch surfaced in the admin panel. The getter/setter are
/// bound to a config property (or any backing field) so flipping it from the
/// menu takes effect live.
/// </summary>
public class FeatureToggle
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required Func<bool> Get { get; init; }
    public required Action<bool> Set { get; init; }
}

/// <summary>
/// A one-shot admin action surfaced in the admin panel. Either runs an existing
/// registered command as the selecting admin, or invokes a direct callback —
/// whichever keeps the panel decoupled from each feature's implementation.
/// </summary>
public class AdminAction
{
    public required string DisplayName { get; init; }

    /// <summary>
    /// The command to run on selection, e.g. "css_scramble" or
    /// "css_forcebombsite A". Ignored when <see cref="Execute"/> is set.
    /// </summary>
    public string? Command { get; init; }

    /// <summary>
    /// A direct callback to run on selection. Takes precedence over
    /// <see cref="Command"/> when set.
    /// </summary>
    public Action<CounterStrikeSharp.API.Core.CCSPlayerController>? Execute { get; init; }
}

/// <summary>
/// A nested submenu surfaced in the admin panel. The open callback builds and
/// shows the submenu for the selecting admin.
/// </summary>
public class AdminSubmenu
{
    public required string DisplayName { get; init; }
    public required Action<CounterStrikeSharp.API.Core.CCSPlayerController> Open { get; init; }
}
