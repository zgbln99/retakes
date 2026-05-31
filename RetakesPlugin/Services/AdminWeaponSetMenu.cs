using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu for forcing a global weapon set (or returning to random). The
/// chosen set applies to everyone equally and is announced to the whole server,
/// so it acts as a fair game mode rather than an advantage. Rendered through the
/// shared MenuService.
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
        var current = _weaponService.ForcedSet?.DisplayName ?? "Losowo";

        _menus.Show(player, $"Zestaw broni (teraz: {current})", menu =>
        {
            menu.AddOption("Losowo (domyślnie)", _ =>
            {
                _weaponService.SetForcedSet(null);
                Announce("Losowy przydział broni");
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
