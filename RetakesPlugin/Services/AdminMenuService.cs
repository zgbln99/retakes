using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services;

/// <summary>
/// Builds and opens the in-game admin panel and owns the registries of feature
/// toggles and admin actions that other modules plug into. This is the "command
/// center" the rest of the features hang off. All rendering goes through the
/// shared <see cref="MenuService"/> (Back / Close / pagination / guards).
/// </summary>
public class AdminMenuService
{
    private readonly MenuService _menus;
    private readonly AdminMenuSettings _settings;
    private readonly List<FeatureToggle> _toggles = new();
    private readonly List<AdminAction> _actions = new();
    private readonly List<AdminSubmenu> _submenus = new();

    public AdminMenuService(MenuService menus, AdminMenuSettings settings)
    {
        _menus = menus;
        _settings = settings;
    }

    public void RegisterToggle(FeatureToggle toggle) => _toggles.Add(toggle);
    public void RegisterAction(AdminAction action) => _actions.Add(action);
    public void RegisterSubmenu(AdminSubmenu submenu) => _submenus.Add(submenu);

    public bool CanUse(CCSPlayerController player)
    {
        var flags = _settings.PermissionFlags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // No flags configured => allow anyone (useful for testing / open servers).
        return flags.Length == 0 || AdminManager.PlayerHasPermissions(player, flags);
    }

    public void OpenMainMenu(CCSPlayerController player) => _menus.OpenRoot(player, ShowMainMenu);

    private void ShowMainMenu(CCSPlayerController player)
    {
        _menus.Show(player, $"{ChatColors.Green}Retakes{ChatColors.White} — Panel Admina", menu =>
        {
            menu.AddOption("Funkcje (on/off)", ShowTogglesMenu);

            if (_actions.Count > 0)
            {
                menu.AddOption("Akcje rundy", ShowActionsMenu);
            }

            foreach (var submenu in _submenus)
            {
                var captured = submenu;
                menu.AddOption(captured.DisplayName, p => captured.Open(p));
            }
        }, ShowMainMenu);
    }

    private void ShowTogglesMenu(CCSPlayerController player)
    {
        _menus.Show(player, "Funkcje (on/off)", menu =>
        {
            if (_toggles.Count == 0)
            {
                menu.AddOption("Brak funkcji do przełączenia", _ => { }, disabled: true);
            }

            foreach (var toggle in _toggles)
            {
                var captured = toggle;
                var state = captured.Get()
                    ? "<font color='#40ff40'>ON</font>"
                    : "<font color='#ff4040'>OFF</font>";

                menu.AddOption($"{captured.DisplayName}: {state}", p =>
                {
                    var newValue = !captured.Get();
                    captured.Set(newValue);

                    p.PrintToChat(
                        $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} {captured.DisplayName}: " +
                        (newValue ? $"{ChatColors.Green}ON" : $"{ChatColors.Red}OFF"));

                    // Reopen so the state label refreshes.
                    ShowTogglesMenu(p);
                });
            }
        }, ShowTogglesMenu);
    }

    private void ShowActionsMenu(CCSPlayerController player)
    {
        _menus.Show(player, "Akcje rundy", menu =>
        {
            foreach (var action in _actions)
            {
                var captured = action;
                menu.AddOption(captured.DisplayName, p =>
                {
                    if (captured.Execute != null)
                    {
                        captured.Execute(p);
                    }
                    else if (captured.Command != null)
                    {
                        p.ExecuteClientCommandFromServer(captured.Command);
                    }

                    p.PrintToChat(
                        $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Wykonano: {ChatColors.Green}{captured.DisplayName}");
                });
            }
        }, ShowActionsMenu);
    }
}
