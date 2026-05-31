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
            command.ReplyToCommand($" {ChatColors.Green}[Retakes]{ChatColors.White} Statistics are currently unavailable.");
            return;
        }

        var stats = _statsService.GetCached(player!.SteamID);
        if (stats == null)
        {
            command.ReplyToCommand($" {ChatColors.Green}[Retakes]{ChatColors.White} No stats recorded for you yet.");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[Retakes]{ChatColors.White} Your stats:");
        player.PrintToChat(
            $" {ChatColors.Grey}Kills:{ChatColors.Green} {stats.Kills}{ChatColors.Grey} | Deaths:{ChatColors.Red} {stats.Deaths}" +
            $"{ChatColors.Grey} | K/D:{ChatColors.Gold} {stats.KillDeathRatio}");
        player.PrintToChat(
            $" {ChatColors.Grey}HS%:{ChatColors.Gold} {stats.HeadshotPercentage}%{ChatColors.Grey} | Assists:{ChatColors.Green} {stats.Assists}" +
            $"{ChatColors.Grey} | Rounds:{ChatColors.White} {stats.RoundsPlayed}");
    }
}
