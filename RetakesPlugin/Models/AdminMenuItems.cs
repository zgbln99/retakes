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
/// A one-shot admin action surfaced in the admin panel. It is executed by
/// running an existing registered command as the admin who selected it, which
/// keeps the panel decoupled from each feature's internal implementation.
/// </summary>
public class AdminAction
{
    public required string DisplayName { get; init; }

    /// <summary>
    /// The command to run on selection, e.g. "css_scramble" or
    /// "css_forcebombsite A".
    /// </summary>
    public required string Command { get; init; }
}
