using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;
using RetakesPlugin.Models;

namespace RetakesPlugin.Services;

/// <summary>
/// Builds and opens the in-game admin panel (a center-screen HTML menu) and owns
/// the registries of feature toggles and admin actions that other modules plug
/// into. This is the "command center" the rest of the features hang off.
/// </summary>
public class AdminMenuService
{
    private readonly BasePlugin _plugin;
    private readonly AdminMenuSettings _settings;
    private readonly List<FeatureToggle> _toggles = new();
    private readonly List<AdminAction> _actions = new();

    public AdminMenuService(BasePlugin plugin, AdminMenuSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void RegisterToggle(FeatureToggle toggle) => _toggles.Add(toggle);
    public void RegisterAction(AdminAction action) => _actions.Add(action);

    public bool CanUse(CCSPlayerController player)
    {
        var flags = _settings.PermissionFlags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // No flags configured => allow anyone (useful for testing / open servers).
        return flags.Length == 0 || AdminManager.PlayerHasPermissions(player, flags);
    }

    public void OpenMainMenu(CCSPlayerController player)
    {
        var menu = new CenterHtmlMenu($"{ChatColors.Green}Retakes{ChatColors.White} — Panel Admina", _plugin);

        menu.AddMenuOption("Funkcje (on/off)", (p, _) => OpenTogglesMenu(p));

        if (_actions.Count > 0)
        {
            menu.AddMenuOption("Akcje rundy", (p, _) => OpenActionsMenu(p));
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
    }

    private void OpenTogglesMenu(CCSPlayerController player)
    {
        var menu = new CenterHtmlMenu("Funkcje (on/off)", _plugin);

        if (_toggles.Count == 0)
        {
            menu.AddMenuOption("Brak funkcji do przełączenia", (_, _) => { }, disabled: true);
        }

        foreach (var toggle in _toggles)
        {
            var isOn = toggle.Get();
            var state = isOn
                ? "<font color='#40ff40'>ON</font>"
                : "<font color='#ff4040'>OFF</font>";

            menu.AddMenuOption($"{toggle.DisplayName}: {state}", (p, _) =>
            {
                var newValue = !toggle.Get();
                toggle.Set(newValue);

                p.PrintToChat(
                    $" {ChatColors.Green}[Retakes]{ChatColors.White} {toggle.DisplayName}: " +
                    (newValue ? $"{ChatColors.Green}ON" : $"{ChatColors.Red}OFF"));

                // Reopen so the state label refreshes.
                OpenTogglesMenu(p);
            });
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
    }

    private void OpenActionsMenu(CCSPlayerController player)
    {
        var menu = new CenterHtmlMenu("Akcje rundy", _plugin);

        foreach (var action in _actions)
        {
            menu.AddMenuOption(action.DisplayName, (p, _) =>
            {
                p.ExecuteClientCommandFromServer(action.Command);
                p.PrintToChat(
                    $" {ChatColors.Green}[Retakes]{ChatColors.White} Wykonano: {ChatColors.Green}{action.DisplayName}");
            });
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
    }
}
