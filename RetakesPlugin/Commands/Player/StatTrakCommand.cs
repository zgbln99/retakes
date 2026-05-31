using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !stattrak — shows the caller's StatTrak weapon counters (kills per weapon).
/// </summary>
public class StatTrakCommand
{
    private readonly StatsService _statsService;
    private readonly int _limit;

    public StatTrakCommand(StatsService statsService, int limit)
    {
        _statsService = statsService;
        _limit = limit;
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
                var rows = await _statsService.GetStatTrakAsync(steamId, _limit);

                Server.NextFrame(() =>
                {
                    var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
                    if (target is not { IsValid: true }) return;

                    target.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} StatTrak — Twoje bronie:");
                    if (rows.Count == 0)
                    {
                        target.PrintToChat($" {ChatColors.Grey}Brak danych — zagraj kilka rund.");
                        return;
                    }

                    foreach (var r in rows)
                    {
                        target.PrintToChat(
                            $" {ChatColors.White}{Pretty(r.Weapon)}{ChatColors.Grey}: {ChatColors.Gold}{r.Kills}{ChatColors.Grey} zab.");
                    }
                });
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning("Stats", $"!stattrak query failed: {ex.Message}");
            }
        });
    }

    private static string Pretty(string weapon) => weapon.Replace("weapon_", "").ToUpperInvariant();
}
