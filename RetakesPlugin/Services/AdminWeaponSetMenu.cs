using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu for forcing a global weapon set (or returning to the default
/// AK-47 / M4A1-S loadout). The chosen set applies to everyone equally and is
/// announced to the whole server, so it acts as a fair game mode rather than an
/// advantage. Rendered through the shared MenuService.
/// </summary>
public class AdminWeaponSetMenu
{
    private readonly MenuService _menus;
    private readonly WeaponAllocationService _weaponService;

    public AdminWeaponSetMenu(MenuService menus, WeaponAllocationService weaponService)
    {
        _menus = menus;
        _weaponService = weaponService;
    }

    public void Open(CCSPlayerController player)
    {
        var current = _weaponService.ForcedSet?.DisplayName ?? "Domyślny (AK-47 / M4A1-S)";

        _menus.Show(player, $"Zestaw broni (teraz: {current})", menu =>
        {
            menu.AddOption("Domyślny (AK-47 / M4A1-S)", _ =>
            {
                _weaponService.SetForcedSet(null);
                Announce("Domyślny przydział broni (AK-47 / M4A1-S)");
            });

            foreach (var set in _weaponService.AvailableSets)
            {
                var captured = set;
                menu.AddOption(captured.DisplayName, _ =>
                {
                    _weaponService.SetForcedSet(captured);
                    Announce(captured.DisplayName);
                });
            }
        }, Open);
    }

    private static void Announce(string setName)
    {
        Server.PrintToChatAll(
            $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Zestaw broni ustawiony na: {ChatColors.Gold}{setName}{ChatColors.White} (od następnej rundy).");
    }
}
