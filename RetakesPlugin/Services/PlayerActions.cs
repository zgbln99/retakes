using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Shared player effects/actions used by both the in-game admin menus and the
/// remote (web panel) command. One implementation, no duplication. All methods
/// must run on the game thread.
/// </summary>
public static class PlayerActions
{
    /// <summary>Runs a named action on the player found by SteamID. Returns false if not found.</summary>
    public static bool Apply(ulong steamId, string action, out string message)
    {
        var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
        if (target == null)
        {
            message = "player not found";
            return false;
        }

        var name = target.PlayerName;
        try
        {
            switch (action.ToLowerInvariant())
            {
                case "kick": Server.ExecuteCommand($"kickid {target.UserId}"); break;
                case "slay": if (target.PawnIsAlive) target.CommitSuicide(false, true); break;
                case "respawn": if (!target.PawnIsAlive) target.Respawn(); break;
                case "t": target.ChangeTeam(CsTeam.Terrorist); break;
                case "ct": target.ChangeTeam(CsTeam.CounterTerrorist); break;
                case "spec": target.ChangeTeam(CsTeam.Spectator); break;
                case "strip": target.RemoveWeapons(); break;
                case "god": Pawn(target, p => { p.Health = 999999; State(p, "m_iHealth"); p.TakesDamage = false; }); break;
                case "ungod": Pawn(target, p => { p.TakesDamage = true; p.Health = 100; State(p, "m_iHealth"); }); break;
                case "hp": Pawn(target, p => { p.Health = 100; State(p, "m_iHealth"); }); break;
                case "freeze": Pawn(target, p => SetMove(p, MoveType_t.MOVETYPE_NONE)); break;
                case "unfreeze": Pawn(target, p => SetMove(p, MoveType_t.MOVETYPE_WALK)); break;
                case "noclip": Pawn(target, p => SetMove(p, MoveType_t.MOVETYPE_NOCLIP)); break;
                case "lowgrav": Pawn(target, p => p.GravityScale = 0.3f); break;
                case "normgrav": Pawn(target, p => p.GravityScale = 1.0f); break;
                case "speed": Pawn(target, p => p.VelocityModifier = 2.0f); break;
                case "normspeed": Pawn(target, p => p.VelocityModifier = 1.0f); break;
                case "small": Pawn(target, p => Size(p, 0.5f)); break;
                case "big": Pawn(target, p => Size(p, 1.5f)); break;
                case "giant": Pawn(target, p => Size(p, 2.5f)); break;
                case "normsize": Pawn(target, p => Size(p, 1.0f)); break;
                default:
                    message = $"unknown action '{action}'";
                    return false;
            }

            message = $"{name}: {action}";
            return true;
        }
        catch (Exception ex)
        {
            message = $"{name}: {action} failed — {ex.Message}";
            return false;
        }
    }

    private static void Pawn(CCSPlayerController controller, Action<CCSPlayerPawn> action)
    {
        var pawn = controller.PlayerPawn.Value;
        if (pawn is { IsValid: true }) action(pawn);
    }

    private static void State(CCSPlayerPawn pawn, string field) =>
        Utilities.SetStateChanged(pawn, "CBaseEntity", field);

    private static void SetMove(CCSPlayerPawn pawn, MoveType_t moveType)
    {
        pawn.MoveType = moveType;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }

    private static void Size(CCSPlayerPawn pawn, float scale) =>
        pawn.AcceptInput("SetScale", pawn, pawn, scale.ToString(CultureInfo.InvariantCulture));
}
