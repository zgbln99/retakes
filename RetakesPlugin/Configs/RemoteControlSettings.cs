using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the MySQL-bridge remote control: the plugin polls a commands
/// table that a web panel (on a separate VPS) writes to, executes the queued
/// commands in-game, and publishes a status row the panel can read. Uses the
/// same database as the stats module (StatsSettings.Database).
/// </summary>
public class RemoteControlSettings
{
    /// <summary>Master toggle. Requires StatsSettings.Database to be configured.</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = false;

    /// <summary>How often (seconds) the plugin checks for queued commands.</summary>
    [JsonPropertyName("PollIntervalSeconds")]
    public float PollIntervalSeconds { get; set; } = 3.0f;

    /// <summary>How often (seconds) the plugin publishes server status (map/players).</summary>
    [JsonPropertyName("StatusIntervalSeconds")]
    public float StatusIntervalSeconds { get; set; } = 10.0f;

    /// <summary>
    /// Identifier for this game server in the shared database. Lets one panel +
    /// one database serve multiple servers. Commands are only run if their
    /// target server_id matches (or is empty = any).
    /// </summary>
    [JsonPropertyName("ServerId")]
    public string ServerId { get; set; } = "cwelownia1";

    /// <summary>
    /// Allowlist of command prefixes the bridge may execute (defence in depth).
    /// A queued command must start with one of these. Empty = allow all (not
    /// recommended).
    /// </summary>
    [JsonPropertyName("AllowedCommandPrefixes")]
    public List<string> AllowedCommandPrefixes { get; set; } = new()
    {
        "css_", "changelevel", "map", "mp_", "sv_", "bot_", "kickid", "say", "exec"
    };
}
