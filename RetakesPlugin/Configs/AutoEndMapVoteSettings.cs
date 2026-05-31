using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the automatic end-of-cycle map vote. This is SEPARATE from the
/// !rtv flow (MapVoteSettings). It opens a vote on the last round and, only if at
/// least one player voted, changes to the winning map after the round ends.
/// </summary>
public class AutoEndMapVoteSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>Start the vote automatically on the last round of the cycle.</summary>
    [JsonPropertyName("StartOnLastRound")]
    public bool StartOnLastRound { get; set; } = true;

    [JsonPropertyName("VoteDurationSeconds")]
    public float VoteDurationSeconds { get; set; } = 25.0f;

    /// <summary>Only change the map if at least one vote was cast.</summary>
    [JsonPropertyName("ChangeOnlyIfVotes")]
    public bool ChangeOnlyIfVotes { get; set; } = true;

    [JsonPropertyName("Maps")]
    public List<string> Maps { get; set; } = new()
    {
        "de_inferno",
        "de_mirage",
        "de_dust2",
        "de_cache",
        "de_nuke",
        "de_ancient",
        "de_anubis"
    };
}
