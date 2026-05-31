using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !guns — lets a player choose their preferred rifle per team (or random), and
/// whether they'd like a sniper. Preferences are applied by the weapon allocator
/// on the next round.
/// </summary>
public class GunsCommand
{
    private readonly BasePlugin _plugin;
    private readonly WeaponAllocationService _weaponService;

    public GunsCommand(BasePlugin plugin, WeaponAllocationService weaponService)
    {
        _plugin = plugin;
        _weaponService = weaponService;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!PlayerHelper.IsValid(player)) return;

        if (!_weaponService.Settings.IsEnabled || !_weaponService.Settings.AllowPreferences)
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Wybór broni jest wyłączony.");
            return;
        }

        OpenMainMenu(player!);
    }

    private void OpenMainMenu(CCSPlayerController player)
    {
        var pref = _weaponService.GetPreference(player.SteamID);
        var menu = new CenterHtmlMenu($"{ChatColors.Green}Wybór broni{ChatColors.White}", _plugin);

        var tRifle = pref?.TerroristRifle != null ? WeaponAllocationService.DisplayName(pref.TerroristRifle) : "Losowo";
        var ctRifle = pref?.CounterTerroristRifle != null ? WeaponAllocationService.DisplayName(pref.CounterTerroristRifle) : "Losowo";

        menu.AddMenuOption($"Karabin T: {ChatColors.Gold}{tRifle}", (p, _) => OpenRifleMenu(p, CsTeam.Terrorist));
        menu.AddMenuOption($"Karabin CT: {ChatColors.Gold}{ctRifle}", (p, _) => OpenRifleMenu(p, CsTeam.CounterTerrorist));

        if (_weaponService.Settings.AllowSnipers)
        {
            var sniper = pref?.PreferSniper == true ? "TAK" : "nie";
            menu.AddMenuOption($"Preferuj snajperkę: {ChatColors.Gold}{sniper}", (p, _) =>
            {
                var current = _weaponService.GetOrCreatePreference(p.SteamID);
                current.PreferSniper = !current.PreferSniper;
                p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Snajperka: " +
                              (current.PreferSniper ? $"{ChatColors.Green}TAK" : $"{ChatColors.Red}nie"));
                OpenMainMenu(p);
            });
        }

        menu.AddMenuOption($"{ChatColors.Grey}Reset (wszystko losowo)", (p, _) =>
        {
            _weaponService.ResetPreference(p.SteamID);
            p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Preferencje zresetowane do losowych.");
            OpenMainMenu(p);
        });

        OpenWithTimeout(player, menu);
    }

    /// <summary>Opens a center menu that auto-closes after 10 seconds.</summary>
    private void OpenWithTimeout(CCSPlayerController player, CenterHtmlMenu menu)
    {
        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);

        var steamId = player.SteamID;
        _plugin.AddTimer(10.0f, () =>
        {
            var target = CounterStrikeSharp.API.Utilities.GetPlayers()
                .FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
            if (target != null)
            {
                MenuManager.CloseActiveMenu(target);
            }
        });
    }

    private void OpenRifleMenu(CCSPlayerController player, CsTeam team)
    {
        var rifles = team == CsTeam.Terrorist
            ? _weaponService.Settings.TerroristRifles
            : _weaponService.Settings.CounterTerroristRifles;

        var teamName = team == CsTeam.Terrorist ? "T" : "CT";
        var menu = new CenterHtmlMenu($"Karabin {teamName}", _plugin);

        menu.AddMenuOption("Losowo", (p, _) =>
        {
            var pref = _weaponService.GetOrCreatePreference(p.SteamID);
            if (team == CsTeam.Terrorist) pref.TerroristRifle = null;
            else pref.CounterTerroristRifle = null;
            p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Karabin {teamName}: {ChatColors.Gold}Losowo");
            OpenMainMenu(p);
        });

        foreach (var rifle in rifles)
        {
            menu.AddMenuOption(WeaponAllocationService.DisplayName(rifle), (p, _) =>
            {
                var pref = _weaponService.GetOrCreatePreference(p.SteamID);
                if (team == CsTeam.Terrorist) pref.TerroristRifle = rifle;
                else pref.CounterTerroristRifle = rifle;
                p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Karabin {teamName}: {ChatColors.Gold}{WeaponAllocationService.DisplayName(rifle)}");
                OpenMainMenu(p);
            });
        }

        OpenWithTimeout(player, menu);
    }
}
