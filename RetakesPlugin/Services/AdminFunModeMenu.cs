using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu for selecting a symmetric fun mode (or returning to normal).
/// The chosen mode applies to everyone equally and is announced to the server.
/// Rendered through the shared MenuService.
/// </summary>
public class AdminFunModeMenu
{
    private readonly MenuService _menus;
    private readonly WeaponAllocationService _weaponService;
    private readonly FunModeSettings _settings;

    public AdminFunModeMenu(MenuService menus, WeaponAllocationService weaponService, FunModeSettings settings)
    {
        _menus = menus;
        _weaponService = weaponService;
        _settings = settings;
    }

    public void Open(CCSPlayerController player)
    {
        var current = _weaponService.ActiveFunMode.DisplayName();

        _menus.Show(player, $"Tryb fun (teraz: {current})", menu =>
        {
            foreach (FunMode mode in Enum.GetValues(typeof(FunMode)))
            {
                var captured = mode;
                menu.AddOption(captured.DisplayName(), _ => Select(captured));
            }
        }, Open);
    }

    private void Select(FunMode mode)
    {
        _weaponService.SetFunMode(mode);
        ApplyGravity(mode);

        Server.PrintToChatAll(
            $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Tryb fun: {ChatColors.Gold}{mode.DisplayName()}{ChatColors.White} (od następnej rundy).");
    }

    /// <summary>Applies (or clears) the low-gravity cvar based on the selected mode.</summary>
    public void ApplyGravity(FunMode mode)
    {
        var gravity = mode == FunMode.LowGravity ? _settings.LowGravityValue : 800;
        Server.ExecuteCommand($"sv_gravity {gravity}");
    }
}
