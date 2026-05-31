using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !top — shows the kills leaderboard. The query runs off the game thread and
/// results are printed back on the main thread.
/// </summary>
public class TopCommand
{
    private readonly StatsService _statsService;

    public TopCommand(StatsService statsService)
    {
        _statsService = statsService;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!PlayerHelper.IsValid(player)) return;

        if (!_statsService.IsReady)
        {
            command.ReplyToCommand($" {ChatColors.Green}[Retakes]{ChatColors.White} Statistics are currently unavailable.");
            return;
        }

        var steamId = player!.SteamID;

        Task.Run(async () =>
        {
            try
            {
                var top = await _statsService.GetTopAsync();

                Server.NextFrame(() =>
                {
                    var target = Utilities.GetPlayers()
                        .FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
                    if (target is not { IsValid: true }) return;

                    target.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Top players:");

                    if (top.Count == 0)
                    {
                        target.PrintToChat($" {ChatColors.Grey}No stats recorded yet.");
                        return;
                    }

                    var rank = 1;
                    foreach (var stats in top)
                    {
                        target.PrintToChat(
                            $" {ChatColors.Gold}#{rank}{ChatColors.White} {stats.Name} " +
                            $"{ChatColors.Grey}- {ChatColors.Green}{stats.Kills}K{ChatColors.Grey}/{ChatColors.Red}{stats.Deaths}D " +
                            $"{ChatColors.Grey}(K/D {ChatColors.Gold}{stats.KillDeathRatio}{ChatColors.Grey})");
                        rank++;
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Stats", $"!top query failed: {ex.Message}");
            }
        });
    }
}
