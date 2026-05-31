using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Admin submenu with per-player actions: pick a player, then slay / kick / move
/// to T, CT or spectator. Rendered through the shared MenuService and only
/// reachable from the permission-gated admin panel.
/// </summary>
public class AdminPlayerMenu
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly MenuService _menus;

    public AdminPlayerMenu(MenuService menus)
    {
        _menus = menus;
    }

    public void Open(CCSPlayerController admin) => ShowPlayerList(admin);

    private void ShowPlayerList(CCSPlayerController admin)
    {
        _menus.Show(admin, "Wybierz gracza", menu =>
        {
            var players = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV)
                .ToList();

            if (players.Count == 0)
            {
                menu.AddOption("Brak graczy", _ => { }, disabled: true);
                return;
            }

            foreach (var target in players)
            {
                var steamId = target.SteamID;
                var name = target.PlayerName;
                menu.AddOption(name, a => ShowActions(a, steamId, name));
            }
        }, ShowPlayerList);
    }

    private void ShowActions(CCSPlayerController admin, ulong targetSteamId, string targetName)
    {
        void Screen(CCSPlayerController a) => ShowActions(a, targetSteamId, targetName);

        _menus.Show(admin, $"Gracz: {targetName}", menu =>
        {
            menu.AddOption("Zabij (slay)", a => Act(a, targetSteamId, t =>
            {
                if (t.PawnIsAlive) t.CommitSuicide(false, true);
            }, $"{targetName} — zabity"));

            menu.AddOption("Przenieś do T", a => Act(a, targetSteamId, t =>
                t.ChangeTeam(CsTeam.Terrorist), $"{targetName} → T"));

            menu.AddOption("Przenieś do CT", a => Act(a, targetSteamId, t =>
                t.ChangeTeam(CsTeam.CounterTerrorist), $"{targetName} → CT"));

            menu.AddOption("Przenieś do widzów", a => Act(a, targetSteamId, t =>
                t.ChangeTeam(CsTeam.Spectator), $"{targetName} → widzowie"));

            menu.AddOption($"{ChatColors.Red}Wyrzuć (kick)", a => Act(a, targetSteamId, t =>
                Server.ExecuteCommand($"kickid {t.UserId}"), $"{targetName} — wyrzucony"));
        }, Screen);
    }

    private void Act(CCSPlayerController admin, ulong targetSteamId, Action<CCSPlayerController> action, string feedback)
    {
        var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == targetSteamId);
        if (target == null)
        {
            admin.PrintToChat($"{Prefix}{ChatColors.Red}Gracz już nie jest dostępny.");
            return;
        }

        try
        {
            action(target);
            admin.PrintToChat($"{Prefix}{ChatColors.Green}{feedback}");
        }
        catch (Exception ex)
        {
            admin.PrintToChat($"{Prefix}{ChatColors.Red}Nie udało się: {ex.Message}");
        }
    }
}
