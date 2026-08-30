using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !guns — lets a player choose their preferred rifle per team (or random), and
/// whether they'd like a sniper. Preferences are applied by the weapon allocator
/// on the next round and persisted in the database. Rendered through the shared
/// MenuService (Back / Close / pagination / guards).
/// </summary>
public class GunsCommand
{
    private readonly MenuService _menus;
    private readonly WeaponAllocationService _weaponService;

    public GunsCommand(MenuService menus, WeaponAllocationService weaponService)
    {
        _menus = menus;
        _weaponService = weaponService;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!PlayerHelper.IsValid(player)) return;

        // With the fixed loadout (random weapons off) there is nothing to choose.
        if (!_weaponService.Settings.IsEnabled
            || !_weaponService.Settings.RandomWeapons
            || !_weaponService.Settings.AllowPreferences)
        {
            command.ReplyToCommand($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Wybór broni jest wyłączony — wszyscy dostają AK-47 (T) / M4A1-S (CT).");
            return;
        }

        _menus.OpenRoot(player!, ShowMainMenu);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        var pref = _weaponService.GetPreference(player.SteamID);
        var tRifle = pref?.TerroristRifle != null ? WeaponAllocationService.DisplayName(pref.TerroristRifle) : "Losowo";
        var ctRifle = pref?.CounterTerroristRifle != null ? WeaponAllocationService.DisplayName(pref.CounterTerroristRifle) : "Losowo";

        _menus.Show(player, $"{ChatColors.Green}Wybór broni{ChatColors.White}", menu =>
        {
            menu.AddOption($"Karabin T: {ChatColors.Gold}{tRifle}", p => ShowRifleMenu(p, CsTeam.Terrorist));
            menu.AddOption($"Karabin CT: {ChatColors.Gold}{ctRifle}", p => ShowRifleMenu(p, CsTeam.CounterTerrorist));

            if (_weaponService.Settings.AllowSnipers)
            {
                var sniper = pref?.PreferSniper == true ? "TAK" : "nie";
                menu.AddOption($"Preferuj snajperkę: {ChatColors.Gold}{sniper}", p =>
                {
                    var current = _weaponService.GetOrCreatePreference(p.SteamID);
                    current.PreferSniper = !current.PreferSniper;
                    _weaponService.SavePreference(p.SteamID);
                    p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Snajperka: " +
                                  (current.PreferSniper ? $"{ChatColors.Green}TAK" : $"{ChatColors.Red}nie"));
                    ShowMainMenu(p);
                });

                // When the player prefers a sniper AND the scout is allowed, let them
                // choose whether they want the SSG 08 specifically.
                if (pref?.PreferSniper == true && _weaponService.Settings.AllowScout)
                {
                    var wantsScout = pref?.PreferredSniper == "weapon_ssg08";
                    menu.AddOption($"  → Chcę Scout (SSG 08): {ChatColors.Gold}{(wantsScout ? "TAK" : "nie")}", p =>
                    {
                        var current = _weaponService.GetOrCreatePreference(p.SteamID);
                        current.PreferredSniper = current.PreferredSniper == "weapon_ssg08" ? null : "weapon_ssg08";
                        _weaponService.SavePreference(p.SteamID);
                        p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Scout (SSG 08): " +
                                      (current.PreferredSniper == "weapon_ssg08" ? $"{ChatColors.Green}TAK" : $"{ChatColors.Red}nie (losowa snajperka)"));
                        ShowMainMenu(p);
                    });
                }
            }

            menu.AddOption($"{ChatColors.Grey}Reset (wszystko losowo)", p =>
            {
                _weaponService.ResetPreference(p.SteamID);
                p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Preferencje zresetowane do losowych.");
                ShowMainMenu(p);
            });
        }, ShowMainMenu);
    }

    private void ShowRifleMenu(CCSPlayerController player, CsTeam team)
    {
        var rifles = team == CsTeam.Terrorist
            ? _weaponService.Settings.TerroristRifles
            : _weaponService.Settings.CounterTerroristRifles;

        var teamName = team == CsTeam.Terrorist ? "T" : "CT";

        void Screen(CCSPlayerController p) => ShowRifleMenu(p, team);

        _menus.Show(player, $"Karabin {teamName}", menu =>
        {
            menu.AddOption("Losowo", p =>
            {
                var pref = _weaponService.GetOrCreatePreference(p.SteamID);
                if (team == CsTeam.Terrorist) pref.TerroristRifle = null;
                else pref.CounterTerroristRifle = null;
                _weaponService.SavePreference(p.SteamID);
                p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Karabin {teamName}: {ChatColors.Gold}Losowo");
                ShowMainMenu(p);
            });

            foreach (var rifle in rifles)
            {
                var captured = rifle;
                menu.AddOption(WeaponAllocationService.DisplayName(captured), p =>
                {
                    var pref = _weaponService.GetOrCreatePreference(p.SteamID);
                    if (team == CsTeam.Terrorist) pref.TerroristRifle = captured;
                    else pref.CounterTerroristRifle = captured;
                    _weaponService.SavePreference(p.SteamID);
                    p.PrintToChat($" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} Karabin {teamName}: {ChatColors.Gold}{WeaponAllocationService.DisplayName(captured)}");
                    ShowMainMenu(p);
                });
            }
        }, Screen);
    }
}
