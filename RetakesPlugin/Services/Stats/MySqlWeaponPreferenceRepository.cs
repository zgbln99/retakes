using MySqlConnector;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services.Stats;

/// <summary>
/// MySQL-backed storage for weapon preferences, sharing the same database as the
/// stats module (e.g. the DatHost MySQL database).
/// </summary>
public class MySqlWeaponPreferenceRepository : IWeaponPreferenceRepository
{
    private readonly string _connectionString;
    private readonly string _table;

    public MySqlWeaponPreferenceRepository(DatabaseSettings settings)
    {
        _connectionString = DbConnectionFactory.BuildConnectionString(settings);
        _table = $"{DbConnectionFactory.TablePrefix(settings)}weapon_prefs";
    }

    public async Task InitializeAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $@"
CREATE TABLE IF NOT EXISTS `{_table}` (
    `steam_id`    BIGINT UNSIGNED NOT NULL PRIMARY KEY,
    `t_rifle`     VARCHAR(64)     NULL,
    `ct_rifle`    VARCHAR(64)     NULL,
    `sniper`      TINYINT(1)      NOT NULL DEFAULT 0,
    `pref_sniper` VARCHAR(64)     NULL,
    `updated_at`  TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        await using (var command = new MySqlCommand(sql, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        // Add the column on tables created before pref_sniper existed (idempotent).
        try
        {
            var alter = $"ALTER TABLE `{_table}` ADD COLUMN IF NOT EXISTS `pref_sniper` VARCHAR(64) NULL;";
            await using var alterCmd = new MySqlCommand(alter, connection);
            await alterCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Older MySQL without IF NOT EXISTS on ADD COLUMN — ignore if it already exists.
        }
    }

    public async Task<WeaponPreference?> LoadAsync(ulong steamId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"SELECT `t_rifle`, `ct_rifle`, `sniper`, `pref_sniper` FROM `{_table}` WHERE `steam_id` = @id;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", steamId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new WeaponPreference
        {
            TerroristRifle = reader.IsDBNull(0) ? null : reader.GetString(0),
            CounterTerroristRifle = reader.IsDBNull(1) ? null : reader.GetString(1),
            PreferSniper = reader.GetBoolean(2),
            PreferredSniper = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }

    public async Task SaveAsync(ulong steamId, WeaponPreference preference)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $@"
INSERT INTO `{_table}` (`steam_id`, `t_rifle`, `ct_rifle`, `sniper`, `pref_sniper`)
VALUES (@id, @t, @ct, @sniper, @psniper)
ON DUPLICATE KEY UPDATE
    `t_rifle` = VALUES(`t_rifle`),
    `ct_rifle` = VALUES(`ct_rifle`),
    `sniper` = VALUES(`sniper`),
    `pref_sniper` = VALUES(`pref_sniper`);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", steamId);
        command.Parameters.AddWithValue("@t", (object?)preference.TerroristRifle ?? DBNull.Value);
        command.Parameters.AddWithValue("@ct", (object?)preference.CounterTerroristRifle ?? DBNull.Value);
        command.Parameters.AddWithValue("@sniper", preference.PreferSniper ? 1 : 0);
        command.Parameters.AddWithValue("@psniper", (object?)preference.PreferredSniper ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(ulong steamId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"DELETE FROM `{_table}` WHERE `steam_id` = @id;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", steamId);
        await command.ExecuteNonQueryAsync();
    }
}
