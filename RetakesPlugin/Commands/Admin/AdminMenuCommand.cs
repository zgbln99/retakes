using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Admin;

/// <summary>
/// Opens the in-game admin panel. Permission-gated via AdminMenuSettings.
/// </summary>
public class AdminMenuCommand
{
    private readonly AdminMenuService _adminMenuService;

    public AdminMenuCommand(AdminMenuService adminMenuService)
    {
        _adminMenuService = adminMenuService;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!PlayerHelper.IsValid(player))
        {
            command.ReplyToCommand("Tej komendy można użyć tylko w grze.");
            return;
        }

        if (!_adminMenuService.CanUse(player!))
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Nie masz uprawnień do tej komendy.");
            return;
        }

        _adminMenuService.OpenMainMenu(player!);
    }
}
