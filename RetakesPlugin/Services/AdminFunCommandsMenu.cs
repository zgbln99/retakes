using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// SimpleAdmin-style "Fun Commands" admin submenu: pick a player, then apply a
/// fun effect (god, hp, slay, respawn, freeze/unfreeze, noclip, low/high gravity,
/// burn, strip weapons). Rendered through the shared MenuService and only
/// reachable from the permission-gated admin panel.
/// </summary>
public class AdminFunCommandsMenu
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly MenuService _menus;

    public AdminFunCommandsMenu(MenuService menus)
    {
        _menus = menus;
    }

    public void Open(CCSPlayerController admin) => ShowPlayerList(admin);

    private void ShowPlayerList(CCSPlayerController admin)
    {
        _menus.Show(admin, "Fun — wybierz gracza", menu =>
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
                menu.AddOption(name, a => ShowFunMenu(a, steamId, name));
            }
        }, ShowPlayerList);
    }

    private void ShowFunMenu(CCSPlayerController admin, ulong targetSteamId, string targetName)
    {
        void Screen(CCSPlayerController a) => ShowFunMenu(a, targetSteamId, targetName);

        _menus.Show(admin, $"Fun: {targetName}", menu =>
        {
            menu.AddOption("God mode (on)", a => OnPawn(a, targetSteamId, pawn =>
            {
                pawn.Health = 999999;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                pawn.TakesDamage = false;
            }, $"{targetName}: god ON"));

            menu.AddOption("God mode (off) + 100 HP", a => OnPawn(a, targetSteamId, pawn =>
            {
                pawn.TakesDamage = true;
                pawn.Health = 100;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }, $"{targetName}: god OFF"));

            menu.AddOption("HP +50", a => OnPawn(a, targetSteamId, pawn =>
            {
                pawn.Health += 50;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }, $"{targetName}: +50 HP"));

            menu.AddOption("Pełne HP (100)", a => OnPawn(a, targetSteamId, pawn =>
            {
                pawn.Health = 100;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }, $"{targetName}: 100 HP"));

            menu.AddOption("Zamroź", a => OnPawn(a, targetSteamId, pawn =>
                SetMoveType(pawn, MoveType_t.MOVETYPE_NONE), $"{targetName}: zamrożony"));

            menu.AddOption("Odmroź", a => OnPawn(a, targetSteamId, pawn =>
                SetMoveType(pawn, MoveType_t.MOVETYPE_WALK), $"{targetName}: odmrożony"));

            menu.AddOption("Noclip (on)", a => OnPawn(a, targetSteamId, pawn =>
                SetMoveType(pawn, MoveType_t.MOVETYPE_NOCLIP), $"{targetName}: noclip ON"));

            menu.AddOption("Noclip (off)", a => OnPawn(a, targetSteamId, pawn =>
                SetMoveType(pawn, MoveType_t.MOVETYPE_WALK), $"{targetName}: noclip OFF"));

            menu.AddOption("Niska grawitacja", a => OnPawn(a, targetSteamId, pawn =>
                SetGravity(pawn, 0.3f), $"{targetName}: niska grawitacja"));

            menu.AddOption("Normalna grawitacja", a => OnPawn(a, targetSteamId, pawn =>
                SetGravity(pawn, 1.0f), $"{targetName}: normalna grawitacja"));

            menu.AddOption("Szybkość x2", a => OnPawn(a, targetSteamId, pawn =>
                SetSpeed(pawn, 2.0f), $"{targetName}: szybkość x2"));

            menu.AddOption("Normalna szybkość", a => OnPawn(a, targetSteamId, pawn =>
                SetSpeed(pawn, 1.0f), $"{targetName}: normalna szybkość"));

            menu.AddOption("Zabierz bronie", a => OnController(a, targetSteamId, c =>
                c.RemoveWeapons(), $"{targetName}: bronie zabrane"));

            menu.AddOption("Respawn", a => OnController(a, targetSteamId, c =>
            {
                if (!c.PawnIsAlive) c.Respawn();
            }, $"{targetName}: respawn"));

            menu.AddOption($"{ChatColors.Red}Zabij (slay)", a => OnController(a, targetSteamId, c =>
            {
                if (c.PawnIsAlive) c.CommitSuicide(false, true);
            }, $"{targetName}: zabity"));
        }, Screen);
    }

    #region effect helpers
    private static void SetMoveType(CCSPlayerPawn pawn, MoveType_t moveType)
    {
        pawn.MoveType = moveType;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }

    private static void SetGravity(CCSPlayerPawn pawn, float scale)
    {
        pawn.GravityScale = scale;
    }

    private static void SetSpeed(CCSPlayerPawn pawn, float modifier)
    {
        pawn.VelocityModifier = modifier;
    }
    #endregion

    private void OnPawn(CCSPlayerController admin, ulong targetSteamId, Action<CCSPlayerPawn> action, string feedback)
    {
        OnController(admin, targetSteamId, c =>
        {
            var pawn = c.PlayerPawn.Value;
            if (pawn is not { IsValid: true })
            {
                throw new InvalidOperationException("Gracz nie ma aktywnej postaci (martwy?).");
            }
            action(pawn);
        }, feedback);
    }

    private void OnController(CCSPlayerController admin, ulong targetSteamId, Action<CCSPlayerController> action, string feedback)
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
