using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu for forcing a global weapon set (or returning to random). The
/// chosen set applies to everyone equally and is announced to the whole server,
/// so it acts as a fair game mode rather than an advantage.
/// </summary>
public class AdminWeaponSetMenu
{
    private readonly BasePlugin _plugin;
    private readonly WeaponAllocationService _weaponService;

    public AdminWeaponSetMenu(BasePlugin plugin, WeaponAllocationService weaponService)
    {
        _plugin = plugin;
        _weaponService = weaponService;
    }

    public void Open(CCSPlayerController player)
    {
        var current = _weaponService.ForcedSet?.DisplayName ?? "Losowo";
        var menu = new CenterHtmlMenu($"Zestaw broni (teraz: {current})", _plugin);

        menu.AddMenuOption("Losowo (domyślnie)", (_, _) =>
        {
            _weaponService.SetForcedSet(null);
            Announce("Losowy przydział broni");
        });

        foreach (var set in _weaponService.AvailableSets)
        {
            var captured = set;
            menu.AddMenuOption(captured.DisplayName, (_, _) =>
            {
                _weaponService.SetForcedSet(captured);
                Announce(captured.DisplayName);
            });
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
    }

    private static void Announce(string setName)
    {
        Server.PrintToChatAll(
            $" {ChatColors.Green}[Retakes]{ChatColors.White} Zestaw broni ustawiony na: {ChatColors.Gold}{setName}{ChatColors.White} (od następnej rundy).");
    }
}
