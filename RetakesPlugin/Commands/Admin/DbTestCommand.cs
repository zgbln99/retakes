using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using RetakesPlugin.Configs;
using RetakesPlugin.Services.Stats;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Admin;

/// <summary>
/// !dbtest — admin command that verifies the MySQL connection and reports the
/// result in chat. Connection happens off the game thread; never leaks the
/// password.
/// </summary>
public class DbTestCommand
{
    private readonly DatabaseSettings _database;

    public DbTestCommand(DatabaseSettings database)
    {
        _database = database;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null && !AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Brak uprawnień.");
            return;
        }

        if (!DbConnectionFactory.IsConfigured(_database))
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} {ChatColors.Red}Baza nieskonfigurowana — uzupełnij StatsSettings.Database (lub zmienne środowiskowe RETAKES_DB_*).");
            return;
        }

        var steamId = player?.SteamID ?? 0;
        command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Testuję połączenie z bazą...");

        var connectionString = DbConnectionFactory.BuildConnectionString(_database);
        var host = DbConnectionFactory.Host(_database);

        Task.Run(async () =>
        {
            string result;
            try
            {
                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                await using var cmd = new MySqlCommand("SELECT VERSION();", connection);
                var version = (await cmd.ExecuteScalarAsync())?.ToString() ?? "?";
                result = $"{ChatColors.Green}OK{ChatColors.White} — połączono z {host} (MySQL {version})";
            }
            catch (Exception ex)
            {
                result = $"{ChatColors.Red}BŁĄD{ChatColors.White} — {ex.Message}";
                Logger.LogWarning("DbTest", $"Connection failed: {ex.Message}");
            }

            Server.NextFrame(() =>
            {
                var prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";
                if (steamId == 0)
                {
                    Server.PrintToChatAll(prefix + result);
                    return;
                }

                var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
                target?.PrintToChat(prefix + result);
            });
        });
    }
}
