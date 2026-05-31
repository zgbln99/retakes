using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the PvP statistics module (kills/deaths/K-D/HS%), stored in
/// MySQL (e.g. a database provided by DatHost).
/// </summary>
public class StatsSettings
{
    /// <summary>
    /// Master toggle for the stats module. Can also be flipped from the admin
    /// panel at runtime (stops recording, commands still respond).
    /// </summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = false;

    [JsonPropertyName("Database")]
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// How often (in seconds) buffered stats are flushed to the database.
    /// </summary>
    [JsonPropertyName("FlushIntervalSeconds")]
    public float FlushIntervalSeconds { get; set; } = 60.0f;

    /// <summary>
    /// How many players the !top leaderboard shows.
    /// </summary>
    [JsonPropertyName("LeaderboardSize")]
    public int LeaderboardSize { get; set; } = 10;

    /// <summary>StatTrak (per-weapon kill counters) settings.</summary>
    [JsonPropertyName("StatTrak")]
    public StatTrakSettings StatTrak { get; set; } = new();
}

public class StatTrakSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>How many weapons the !stattrak list shows.</summary>
    [JsonPropertyName("TopWeaponsLimit")]
    public int TopWeaponsLimit { get; set; } = 8;
}

/// <summary>
/// MySQL connection settings. Fill these in with the values from your DatHost
/// "Databases" panel. Do NOT commit real credentials to source control.
/// </summary>
public class DatabaseSettings
{
    [JsonPropertyName("Host")]
    public string Host { get; set; } = "";

    [JsonPropertyName("Port")]
    public uint Port { get; set; } = 3306;

    [JsonPropertyName("User")]
    public string User { get; set; } = "";

    [JsonPropertyName("Password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Prefix for the plugin's table(s), allowing multiple plugins to share one
    /// database.
    /// </summary>
    [JsonPropertyName("TablePrefix")]
    public string TablePrefix { get; set; } = "retakes_";
}
