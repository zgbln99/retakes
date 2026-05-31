using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu that changes the map immediately (no vote). Uses the configured
/// map pool. Freezes plugin logic via the supplied callback before changelevel so
/// no event/timer runs during the unload (same guard the vote uses).
/// </summary>
public class AdminChangeMapMenu
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly MenuService _menus;
    private readonly MapVoteSettings _settings;
    private readonly Action _beginMapChange;

    public AdminChangeMapMenu(MenuService menus, MapVoteSettings settings, Action beginMapChange)
    {
        _menus = menus;
        _settings = settings;
        _beginMapChange = beginMapChange;
    }

    public void Open(CCSPlayerController admin)
    {
        _menus.Show(admin, "Zmień mapę", menu =>
        {
            if (_settings.Maps.Count == 0)
            {
                menu.AddOption("Brak map w configu (MapVoteSettings.Maps)", _ => { }, disabled: true);
                return;
            }

            foreach (var map in _settings.Maps)
            {
                var captured = map;
                menu.AddOption(captured, a => ChangeTo(a, captured));
            }
        }, Open);
    }

    private void ChangeTo(CCSPlayerController admin, string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName)) return;

        Server.PrintToChatAll($"{Prefix}{admin.PlayerName} zmienia mapę na {ChatColors.Gold}{mapName}{ChatColors.White}...");

        // Freeze plugin logic, then change on the next frame.
        _beginMapChange();
        Server.NextFrame(() => Server.ExecuteCommand($"changelevel {mapName}"));
    }
}
