using MySqlConnector;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services.Stats;

/// <summary>
/// MySQL-backed statistics storage using MySqlConnector. Designed to work with a
/// hosted database such as the one provided by DatHost.
/// </summary>
public class MySqlStatsRepository : IStatsRepository
{
    private readonly string _connectionString;
    private readonly string _table;
    private readonly string _duelsTable;

    public MySqlStatsRepository(DatabaseSettings settings)
    {
        _connectionString = new MySqlConnectionStringBuilder
        {
            Server = settings.Host,
            Port = settings.Port,
            UserID = settings.User,
            Password = settings.Password,
            Database = settings.Name,
            // Keep the pool modest; a retakes server has few concurrent queries.
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 5,
            ConnectionTimeout = 10
        }.ConnectionString;

        _table = $"{settings.TablePrefix}player_stats";
        _duelsTable = $"{settings.TablePrefix}duels";
    }

    public async Task InitializeAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $@"
CREATE TABLE IF NOT EXISTS `{_table}` (
    `steam_id`   BIGINT UNSIGNED NOT NULL PRIMARY KEY,
    `name`       VARCHAR(128)    NOT NULL DEFAULT '',
    `kills`      INT             NOT NULL DEFAULT 0,
    `deaths`     INT             NOT NULL DEFAULT 0,
    `headshots`  INT             NOT NULL DEFAULT 0,
    `assists`    INT             NOT NULL DEFAULT 0,
    `rounds`     INT             NOT NULL DEFAULT 0,
    `updated_at` TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        await using (var command = new MySqlCommand(sql, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var duelsSql = $@"
CREATE TABLE IF NOT EXISTS `{_duelsTable}` (
    `killer_id`   BIGINT UNSIGNED NOT NULL,
    `victim_id`   BIGINT UNSIGNED NOT NULL,
    `killer_name` VARCHAR(128)    NOT NULL DEFAULT '',
    `victim_name` VARCHAR(128)    NOT NULL DEFAULT '',
    `kills`       INT             NOT NULL DEFAULT 0,
    `headshots`   INT             NOT NULL DEFAULT 0,
    `updated_at`  TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`killer_id`, `victim_id`),
    INDEX `idx_victim` (`victim_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        await using (var duelsCmd = new MySqlCommand(duelsSql, connection))
        {
            await duelsCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<PlayerStats?> LoadAsync(ulong steamId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"SELECT `name`, `kills`, `deaths`, `headshots`, `assists`, `rounds` " +
                  $"FROM `{_table}` WHERE `steam_id` = @steamId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@steamId", steamId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PlayerStats
        {
            SteamId = steamId,
            Name = reader.GetString(0),
            Kills = reader.GetInt32(1),
            Deaths = reader.GetInt32(2),
            Headshots = reader.GetInt32(3),
            Assists = reader.GetInt32(4),
            RoundsPlayed = reader.GetInt32(5)
        };
    }

    public async Task SaveBatchAsync(IReadOnlyCollection<PlayerStats> stats)
    {
        if (stats.Count == 0)
        {
            return;
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var sql = $@"
INSERT INTO `{_table}` (`steam_id`, `name`, `kills`, `deaths`, `headshots`, `assists`, `rounds`)
VALUES (@steamId, @name, @kills, @deaths, @headshots, @assists, @rounds)
ON DUPLICATE KEY UPDATE
    `name` = VALUES(`name`),
    `kills` = VALUES(`kills`),
    `deaths` = VALUES(`deaths`),
    `headshots` = VALUES(`headshots`),
    `assists` = VALUES(`assists`),
    `rounds` = VALUES(`rounds`);";

        foreach (var player in stats)
        {
            await using var command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@steamId", player.SteamId);
            command.Parameters.AddWithValue("@name", Truncate(player.Name, 128));
            command.Parameters.AddWithValue("@kills", player.Kills);
            command.Parameters.AddWithValue("@deaths", player.Deaths);
            command.Parameters.AddWithValue("@headshots", player.Headshots);
            command.Parameters.AddWithValue("@assists", player.Assists);
            command.Parameters.AddWithValue("@rounds", player.RoundsPlayed);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task<List<PlayerStats>> GetTopAsync(int limit)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"SELECT `steam_id`, `name`, `kills`, `deaths`, `headshots`, `assists`, `rounds` " +
                  $"FROM `{_table}` ORDER BY `kills` DESC LIMIT @limit;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limit);

        var result = new List<PlayerStats>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new PlayerStats
            {
                SteamId = reader.GetUInt64(0),
                Name = reader.GetString(1),
                Kills = reader.GetInt32(2),
                Deaths = reader.GetInt32(3),
                Headshots = reader.GetInt32(4),
                Assists = reader.GetInt32(5),
                RoundsPlayed = reader.GetInt32(6)
            });
        }

        return result;
    }

    public async Task SaveDuelsAsync(IReadOnlyCollection<DuelDelta> duels)
    {
        if (duels.Count == 0) return;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var sql = $@"
INSERT INTO `{_duelsTable}` (`killer_id`, `victim_id`, `killer_name`, `victim_name`, `kills`, `headshots`)
VALUES (@killer, @victim, @kname, @vname, @kills, @hs)
ON DUPLICATE KEY UPDATE
    `killer_name` = VALUES(`killer_name`),
    `victim_name` = VALUES(`victim_name`),
    `kills` = `kills` + VALUES(`kills`),
    `headshots` = `headshots` + VALUES(`headshots`);";

        foreach (var d in duels)
        {
            await using var command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@killer", d.KillerSteamId);
            command.Parameters.AddWithValue("@victim", d.VictimSteamId);
            command.Parameters.AddWithValue("@kname", Truncate(d.KillerName, 128));
            command.Parameters.AddWithValue("@vname", Truncate(d.VictimName, 128));
            command.Parameters.AddWithValue("@kills", d.Kills);
            command.Parameters.AddWithValue("@hs", d.Headshots);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task<List<DuelRow>> GetDuelsAsync(ulong steamId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Combine both directions: my kills on opponent (as killer) and their kills
        // on me (as victim of them = my deaths), grouped per opponent.
        var sql = $@"
SELECT opp_id,
       MAX(opp_name) AS opp_name,
       SUM(my_kills) AS my_kills,
       SUM(my_deaths) AS my_deaths
FROM (
    SELECT `victim_id` AS opp_id, `victim_name` AS opp_name, `kills` AS my_kills, 0 AS my_deaths
    FROM `{_duelsTable}` WHERE `killer_id` = @id
    UNION ALL
    SELECT `killer_id` AS opp_id, `killer_name` AS opp_name, 0 AS my_kills, `kills` AS my_deaths
    FROM `{_duelsTable}` WHERE `victim_id` = @id
) t
GROUP BY opp_id
ORDER BY (my_kills + my_deaths) DESC
LIMIT 50;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", steamId);

        var result = new List<DuelRow>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new DuelRow
            {
                OpponentSteamId = reader.GetUInt64(0),
                OpponentName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Kills = reader.GetInt32(2),
                Deaths = reader.GetInt32(3)
            });
        }

        return result;
    }

    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
