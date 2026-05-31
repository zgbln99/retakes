using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class TeamSettings
{
    [JsonPropertyName("TerroristRatio")]
    public float TerroristRatio { get; set; } = 0.45f;

    [JsonPropertyName("RoundsToScramble")]
    public int RoundsToScramble { get; set; } = 5;

    [JsonPropertyName("IsScrambleEnabled")]
    public bool IsScrambleEnabled { get; set; } = true;

    [JsonPropertyName("IsBalanceEnabled")]
    public bool IsBalanceEnabled { get; set; } = true;

    [JsonPropertyName("ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10")]
    public bool ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 { get; set; } = true;

    [JsonPropertyName("ShouldPreventTeamChangesMidRound")]
    public bool ShouldPreventTeamChangesMidRound { get; set; } = true;

    /// <summary>
    /// EXPERIMENTAL. When true, players may freely pick T or CT and the retakes
    /// queue/balance is bypassed for team selection. WARNING: this breaks the core
    /// retakes balancing (teams can become very uneven) and may affect spawns and
    /// weapon allocation. Leave false unless you specifically want free team choice.
    /// </summary>
    [JsonPropertyName("AllowFreeTeamChoice")]
    public bool AllowFreeTeamChoice { get; set; } = false;
}