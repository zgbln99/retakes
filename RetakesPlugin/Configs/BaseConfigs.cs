using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class BaseConfigs : BasePluginConfig
{
    [JsonPropertyName("GameSettings")]
    public GameSettings Game { get; set; } = new();

    [JsonPropertyName("QueueSettings")]
    public QueueSettings Queue { get; set; } = new();

    [JsonPropertyName("TeamSettings")]
    public TeamSettings Team { get; set; } = new();

    [JsonPropertyName("MapConfigSettings")]
    public MapConfigSettings MapConfig { get; set; } = new();

    [JsonPropertyName("BombSettings")]
    public BombSettings Bomb { get; set; } = new();

    [JsonPropertyName("CommandsSettings")]
    public CommandsSettings Commands { get; set; } = new();

    [JsonPropertyName("InstadefuseSettings")]
    public InstadefuseSettings Instadefuse { get; set; } = new();

    [JsonPropertyName("AdminMenuSettings")]
    public AdminMenuSettings AdminMenu { get; set; } = new();

    [JsonPropertyName("WeaponSettings")]
    public WeaponSettings Weapon { get; set; } = new();

    [JsonPropertyName("StatsSettings")]
    public StatsSettings Stats { get; set; } = new();

    [JsonPropertyName("MapVoteSettings")]
    public MapVoteSettings MapVote { get; set; } = new();

    [JsonPropertyName("DebugSettings")]
    public DebugSettings Debug { get; set; } = new();

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 2;
}

public class DebugSettings
{
    [JsonPropertyName("IsDebugMode")]
    public bool IsDebugMode { get; set; } = false;
}