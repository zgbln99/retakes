using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Models;
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

        // Nemesis / victim — fetched off-thread, printed back on the main thread.
        var steamId = player.SteamID;
        Task.Run(async () =>
        {
            try
            {
                var info = NemesisInfo.From(await _statsService.GetDuelsAsync(steamId));
                if (info.Nemesis == null && info.Victim == null) return;

                Server.NextFrame(() =>
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
                    if (target is not { IsValid: true }) return;

                    if (info.Nemesis != null)
                        target.PrintToChat(
                            $" {ChatColors.Grey}Nemezis:{ChatColors.Red} {info.Nemesis.OpponentName}{ChatColors.Grey} (zabił Cię {info.Nemesis.Deaths}x)");
                    if (info.Victim != null)
                        target.PrintToChat(
                            $" {ChatColors.Grey}Twoja ofiara:{ChatColors.Green} {info.Victim.OpponentName}{ChatColors.Grey} (zabity {info.Victim.Kills}x)");
                });
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Stats", $"!rank nemesis query failed: {ex.Message}");
            }
        });
    }
}
