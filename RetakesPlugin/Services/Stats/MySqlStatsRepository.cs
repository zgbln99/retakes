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

        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
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

    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
