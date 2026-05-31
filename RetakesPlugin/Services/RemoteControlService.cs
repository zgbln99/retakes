using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using RetakesPlugin.Configs;
using RetakesPlugin.Services.Stats;

namespace RetakesPlugin.Services;

/// <summary>
/// MySQL-bridge remote control. A web panel on a separate VPS writes rows into a
/// shared commands table; this service polls it, runs each queued command in-game
/// (on the main thread), marks it processed, and publishes a status row
/// (map + player list) the panel can read. No open game-server ports required —
/// everything goes through the existing MySQL database (e.g. DatHost).
///
/// Tables (auto-created):
///   &lt;prefix&gt;remote_commands(id, server_id, command, status, created_at, processed_at)
///   &lt;prefix&gt;server_status(server_id PK, map, players, max_players, updated_at)
/// </summary>
public class RemoteControlService
{
    private readonly BasePlugin _plugin;
    private readonly RemoteControlSettings _settings;
    private readonly DatabaseSettings _db;
    private readonly Func<bool> _isBusy;

    private readonly string _connectionString;
    private readonly string _commandsTable;
    private readonly string _statusTable;

    private bool _ready;
    private volatile bool _stopped;
    private volatile bool _polling;

    public RemoteControlService(BasePlugin plugin, RemoteControlSettings settings, DatabaseSettings db, Func<bool> isBusy)
    {
        _plugin = plugin;
        _settings = settings;
        _db = db;
        _isBusy = isBusy;

        _connectionString = DbConnectionFactory.BuildConnectionString(db);
        var prefix = DbConnectionFactory.TablePrefix(db);
        _commandsTable = $"{prefix}remote_commands";
        _statusTable = $"{prefix}server_status";
        _playersTable = $"{prefix}server_players";
    }

    private readonly string _playersTable;

    public void Initialize()
    {
        if (!_settings.IsEnabled)
        {
            Utils.Logger.LogInfo("Remote", "Remote control disabled in config");
            return;
        }

        if (!DbConnectionFactory.IsConfigured(_db))
        {
            Utils.Logger.LogWarning("Remote", "Remote control needs StatsSettings.Database — disabled");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await InitSchemaAsync();
                _ready = true;
                Utils.Logger.LogInfo("Remote", "Remote control bridge initialized");
            }
            catch (Exception ex)
            {
                _ready = false;
                Utils.Logger.LogWarning("Remote", $"Init failed, remote control off: {ex.Message}");
            }
        });

        _plugin.AddTimer(Math.Max(1.0f, _settings.PollIntervalSeconds), PollCommands, TimerFlags.REPEAT);
        _plugin.AddTimer(Math.Max(3.0f, _settings.StatusIntervalSeconds), PublishStatus, TimerFlags.REPEAT);
    }

    public void StopTimers() => _stopped = true;

    private async Task InitSchemaAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var commandsSql = $@"
CREATE TABLE IF NOT EXISTS `{_commandsTable}` (
    `id`           BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `server_id`    VARCHAR(64)     NOT NULL DEFAULT '',
    `command`      VARCHAR(512)    NOT NULL,
    `status`       VARCHAR(16)     NOT NULL DEFAULT 'pending',
    `created_at`   TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `processed_at` TIMESTAMP       NULL,
    INDEX `idx_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        await using (var cmd = new MySqlCommand(commandsSql, connection)) await cmd.ExecuteNonQueryAsync();

        var statusSql = $@"
CREATE TABLE IF NOT EXISTS `{_statusTable}` (
    `server_id`   VARCHAR(64)  NOT NULL PRIMARY KEY,
    `map`         VARCHAR(64)  NOT NULL DEFAULT '',
    `players`     INT          NOT NULL DEFAULT 0,
    `max_players` INT          NOT NULL DEFAULT 0,
    `updated_at`  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        await using (var cmd = new MySqlCommand(statusSql, connection)) await cmd.ExecuteNonQueryAsync();

        var playersSql = $@"
CREATE TABLE IF NOT EXISTS `{_playersTable}` (
    `server_id`  VARCHAR(64)     NOT NULL,
    `steam_id`   BIGINT UNSIGNED NOT NULL,
    `name`       VARCHAR(128)    NOT NULL DEFAULT '',
    `team`       INT             NOT NULL DEFAULT 0,
    `alive`      TINYINT(1)      NOT NULL DEFAULT 0,
    `updated_at` TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`server_id`, `steam_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        await using (var cmd = new MySqlCommand(playersSql, connection)) await cmd.ExecuteNonQueryAsync();
    }

    #region Poll & execute
    private void PollCommands()
    {
        if (!_ready || _stopped || _isBusy() || _polling) return;
        _polling = true;

        Task.Run(async () =>
        {
            try
            {
                var commands = await FetchPendingAsync();
                if (commands.Count > 0)
                {
                    // Execute on the game thread.
                    Server.NextFrame(() =>
                    {
                        foreach (var (id, command) in commands)
                        {
                            ExecuteOne(id, command);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Remote", $"Poll failed: {ex.Message}");
            }
            finally
            {
                _polling = false;
            }
        });
    }

    private async Task<List<(ulong id, string command)>> FetchPendingAsync()
    {
        var result = new List<(ulong, string)>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Claim pending rows for this server atomically: mark them 'running' first.
        var claimSql = $@"
UPDATE `{_commandsTable}`
SET `status` = 'running'
WHERE `status` = 'pending' AND (`server_id` = @sid OR `server_id` = '')
ORDER BY `id` ASC
LIMIT 20;";
        await using (var claim = new MySqlCommand(claimSql, connection))
        {
            claim.Parameters.AddWithValue("@sid", _settings.ServerId);
            await claim.ExecuteNonQueryAsync();
        }

        var selectSql = $@"
SELECT `id`, `command` FROM `{_commandsTable}`
WHERE `status` = 'running' AND (`server_id` = @sid OR `server_id` = '')
ORDER BY `id` ASC;";
        await using var select = new MySqlCommand(selectSql, connection);
        select.Parameters.AddWithValue("@sid", _settings.ServerId);
        await using var reader = await select.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add((reader.GetUInt64(0), reader.GetString(1)));
        }

        return result;
    }

    private void ExecuteOne(ulong id, string command)
    {
        var trimmed = command.Trim();
        var allowed = _settings.AllowedCommandPrefixes;
        var ok = allowed.Count == 0 ||
                 allowed.Any(p => trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!ok)
        {
            Utils.Logger.LogWarning("Remote", $"Rejected disallowed command: {trimmed}");
            MarkDone(id, "rejected");
            return;
        }

        try
        {
            Utils.Logger.LogInfo("Remote", $"Executing remote command #{id}: {trimmed}");
            Server.ExecuteCommand(trimmed);
            MarkDone(id, "done");
        }
        catch (Exception ex)
        {
            Utils.Logger.LogWarning("Remote", $"Command #{id} failed: {ex.Message}");
            MarkDone(id, "error");
        }
    }

    private void MarkDone(ulong id, string status)
    {
        Task.Run(async () =>
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                var sql = $"UPDATE `{_commandsTable}` SET `status` = @s, `processed_at` = CURRENT_TIMESTAMP WHERE `id` = @id;";
                await using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Remote", $"MarkDone #{id} failed: {ex.Message}");
            }
        });
    }
    #endregion

    #region Status publishing
    private void PublishStatus()
    {
        if (!_ready || _stopped) return;

        // Read game state on the main thread, then write off-thread.
        Server.NextFrame(() =>
        {
            var map = Server.MapName ?? "";
            var maxPlayers = Server.MaxPlayers;

            var roster = new List<(ulong steamId, string name, int team, bool alive)>();
            foreach (var p in Utilities.GetPlayers())
            {
                if (!p.IsValid || p.IsBot || p.IsHLTV) continue;
                if (p.SteamID == 0) continue;
                roster.Add((p.SteamID, p.PlayerName, (int)p.Team, p.PawnIsAlive));
            }

            var playerCount = roster.Count;

            Task.Run(async () =>
            {
                try
                {
                    await using var connection = new MySqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var statusSql = $@"
INSERT INTO `{_statusTable}` (`server_id`, `map`, `players`, `max_players`)
VALUES (@sid, @map, @players, @max)
ON DUPLICATE KEY UPDATE `map`=VALUES(`map`), `players`=VALUES(`players`), `max_players`=VALUES(`max_players`);";
                    await using (var cmd = new MySqlCommand(statusSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@sid", _settings.ServerId);
                        cmd.Parameters.AddWithValue("@map", map);
                        cmd.Parameters.AddWithValue("@players", playerCount);
                        cmd.Parameters.AddWithValue("@max", maxPlayers);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Refresh the player roster: clear this server's rows, re-insert.
                    await using (var del = new MySqlCommand($"DELETE FROM `{_playersTable}` WHERE `server_id` = @sid", connection))
                    {
                        del.Parameters.AddWithValue("@sid", _settings.ServerId);
                        await del.ExecuteNonQueryAsync();
                    }

                    foreach (var (steamId, name, team, alive) in roster)
                    {
                        var insSql = $@"
INSERT INTO `{_playersTable}` (`server_id`, `steam_id`, `name`, `team`, `alive`)
VALUES (@sid, @steam, @name, @team, @alive);";
                        await using var ins = new MySqlCommand(insSql, connection);
                        ins.Parameters.AddWithValue("@sid", _settings.ServerId);
                        ins.Parameters.AddWithValue("@steam", steamId);
                        ins.Parameters.AddWithValue("@name", name.Length > 128 ? name[..128] : name);
                        ins.Parameters.AddWithValue("@team", team);
                        ins.Parameters.AddWithValue("@alive", alive ? 1 : 0);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    Utils.Logger.LogWarning("Remote", $"Status publish failed: {ex.Message}");
                }
            });
        });
    }
    #endregion
}
