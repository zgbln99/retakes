using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class GameSettings
{
    [JsonPropertyName("MaxPlayers")]
    public int MaxPlayers { get; set; } = 9;

    [JsonPropertyName("ShouldBreakBreakables")]
    public bool ShouldBreakBreakables { get; set; } = false;

    [JsonPropertyName("ShouldOpenDoors")]
    public bool ShouldOpenDoors { get; set; } = false;

    [JsonPropertyName("EnableFallbackAllocation")]
    public bool EnableFallbackAllocation { get; set; } = true;

    /// <summary>
    /// Global voice chat: everyone hears everyone regardless of team or alive/dead
    /// state (sv_full_alltalk). When false, the normal retakes voice rules apply.
    /// </summary>
    [JsonPropertyName("EnableGlobalVoiceChat")]
    public bool EnableGlobalVoiceChat { get; set; } = true;
}