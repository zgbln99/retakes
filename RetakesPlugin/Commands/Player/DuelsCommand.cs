using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !duels / !vs — shows the caller's player-vs-player record: how many times
/// they killed each opponent and were killed by them. Query runs off-thread,
/// printed back on the main thread.
/// </summary>
public class DuelsCommand
{
    private readonly StatsService _statsService;

    public DuelsCommand(StatsService statsService)
    {
        _statsService = statsService;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!PlayerHelper.IsValid(player)) return;

        if (!_statsService.IsReady)
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Statystyki są obecnie niedostępne.");
            return;
        }

        var steamId = player!.SteamID;

        Task.Run(async () =>
        {
            try
            {
                var duels = await _statsService.GetDuelsAsync(steamId);

                Server.NextFrame(() =>
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
                    if (target is not { IsValid: true }) return;

                    target.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Twoje pojedynki (kto kogo):");

                    if (duels.Count == 0)
                    {
                        target.PrintToChat($" {ChatColors.Grey}Brak danych — zagraj kilka rund.");
                        return;
                    }

                    // Show the 10 most-fought opponents.
                    foreach (var d in duels.Take(10))
                    {
                        var color = d.Kills > d.Deaths ? ChatColors.Green
                                  : d.Kills < d.Deaths ? ChatColors.Red
                                  : ChatColors.Gold;
                        target.PrintToChat(
                            $" {ChatColors.White}{d.OpponentName}{ChatColors.Grey}: " +
                            $"{color}{d.Kills}{ChatColors.Grey}:{ChatColors.Red}{d.Deaths}" +
                            $"{ChatColors.Grey} (zab.:śmierci)");
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Stats", $"!duels query failed: {ex.Message}");
            }
        });
    }
}
