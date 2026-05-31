using System.Text.Json;
using CounterStrikeSharp.API;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Persists the live <see cref="BaseConfigs"/> back to the plugin's JSON config
/// file on disk, so changes made at runtime (e.g. from the web panel) survive a
/// server restart. CounterStrikeSharp loads the config from
/// csgo/addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json
/// — we write to the same path.
/// </summary>
public static class ConfigPersistence
{
    private static string ConfigPath => Path.Combine(
        Server.GameDirectory, "csgo", "addons", "counterstrikesharp",
        "configs", "plugins", "RetakesPlugin", "RetakesPlugin.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    /// <summary>Writes the current config to disk. Returns true on success.</summary>
    public static bool Save(BaseConfigs config)
    {
        try
        {
            var path = ConfigPath;
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Write to a temp file then move, so a crash mid-write can't corrupt
            // the live config.
            var json = JsonSerializer.Serialize(config, Options);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);

            Utils.Logger.LogInfo("Config", $"Config saved to {path}");
            return true;
        }
        catch (Exception ex)
        {
            Utils.Logger.LogWarning("Config", $"Config save failed: {ex.Message}");
            return false;
        }
    }
}
