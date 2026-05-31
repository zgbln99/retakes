using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu for selecting a symmetric fun mode (or returning to normal).
/// The chosen mode applies to everyone equally and is announced to the server.
/// </summary>
public class AdminFunModeMenu
{
    private readonly BasePlugin _plugin;
    private readonly WeaponAllocationService _weaponService;
    private readonly FunModeSettings _settings;

    public AdminFunModeMenu(BasePlugin plugin, WeaponAllocationService weaponService, FunModeSettings settings)
    {
        _plugin = plugin;
        _weaponService = weaponService;
        _settings = settings;
    }

    public void Open(CCSPlayerController player)
    {
        var current = _weaponService.ActiveFunMode.DisplayName();
        var menu = new CenterHtmlMenu($"Tryb fun (teraz: {current})", _plugin);

        foreach (FunMode mode in Enum.GetValues(typeof(FunMode)))
        {
            var captured = mode;
            menu.AddMenuOption(mode.DisplayName(), (_, _) => Select(captured));
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
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
