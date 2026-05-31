using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !rank — shows the calling player's own PvP statistics.
/// </summary>
public class RankCommand
{
    private readonly StatsService _statsService;

    public RankCommand(StatsService statsService)
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

        var stats = _statsService.GetCached(player!.SteamID);
        if (stats == null)
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Nie masz jeszcze zapisanych statystyk.");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Twoje statystyki:");
        player.PrintToChat(
            $" {ChatColors.Grey}Zabójstwa:{ChatColors.Green} {stats.Kills}{ChatColors.Grey} | Śmierci:{ChatColors.Red} {stats.Deaths}" +
            $"{ChatColors.Grey} | K/D:{ChatColors.Gold} {stats.KillDeathRatio}");
        player.PrintToChat(
            $" {ChatColors.Grey}HS%:{ChatColors.Gold} {stats.HeadshotPercentage}%{ChatColors.Grey} | Asysty:{ChatColors.Green} {stats.Assists}" +
            $"{ChatColors.Grey} | Rundy:{ChatColors.White} {stats.RoundsPlayed}");
    }
}
