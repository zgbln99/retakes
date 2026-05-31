using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for the in-game admin panel (GUI menu).
/// </summary>
public class AdminMenuSettings
{
    /// <summary>
    /// Master toggle for the admin panel command.
    /// </summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Comma separated CounterStrikeSharp permission flags/groups required to
    /// open the panel (e.g. "@css/root" or "@css/generic,#css/admin").
    /// </summary>
    [JsonPropertyName("PermissionFlags")]
    public string PermissionFlags { get; set; } = "@css/root";

    /// <summary>
    /// Chat/console commands that open the panel. Each entry is registered as a
    /// command alias (prefix with css_).
    /// </summary>
    [JsonPropertyName("OpenCommands")]
    public List<string> OpenCommands { get; set; } = new() { "css_admin", "css_panel" };
}
