using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the player-driven map vote (rock-the-vote style). Players type
/// !rtv to request a map change; once enough players agree a vote opens and the
/// winning map is loaded.
/// </summary>
public class MapVoteSettings
{
    /// <summary>Master toggle. Also flippable from the admin panel.</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Automatically open a map vote when the match ends (a team wins the match).
    /// This is the primary way to change maps.
    /// </summary>
    [JsonPropertyName("StartAtMatchEnd")]
    public bool StartAtMatchEnd { get; set; } = true;

    /// <summary>Allow players to trigger a vote mid-match with !rtv. Off by default.</summary>
    [JsonPropertyName("AllowRtv")]
    public bool AllowRtv { get; set; } = false;

    /// <summary>
    /// Fraction of connected human players that must type !rtv to trigger a vote
    /// (0.6 = 60%).
    /// </summary>
    [JsonPropertyName("RtvRatio")]
    public double RtvRatio { get; set; } = 0.6;

    /// <summary>How long the vote menu stays open, in seconds.</summary>
    [JsonPropertyName("VoteDurationSeconds")]
    public float VoteDurationSeconds { get; set; } = 25.0f;

    /// <summary>How many map choices are offered in a vote.</summary>
    [JsonPropertyName("MapsInVote")]
    public int MapsInVote { get; set; } = 5;

    /// <summary>Seconds to wait after a vote finishes before changing the map.</summary>
    [JsonPropertyName("ChangeDelaySeconds")]
    public float ChangeDelaySeconds { get; set; } = 5.0f;

    /// <summary>
    /// Maps players can vote for. Each entry is a normal map name (e.g. "de_mirage")
    /// loaded with changelevel. Make sure the maps are installed on the server.
    /// </summary>
    [JsonPropertyName("Maps")]
    public List<string> Maps { get; set; } = new()
    {
        "de_ancient", "de_anubis", "de_dust2", "de_inferno",
        "de_mirage", "de_nuke", "de_overpass", "de_train", "de_vertigo"
    };
}
