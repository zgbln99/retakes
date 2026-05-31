using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Built-in instadefuse logic. When the last terrorist dies and a lone CT starts
/// defusing a winnable bomb, the defuse completes instantly (provided there is no
/// active grenade/fire threat near the bomb).
///
/// Ported and adapted from B3none/cs2-instadefuse (GPL-3.0). See NOTICE for
/// attribution. Original author: B3none.
/// </summary>
public class InstadefuseService
{
    private const string LogPrefix = "[Instadefuse] ";
    private static readonly string MessagePrefix = $" [{ChatColors.Green}CWELOWNIA{ChatColors.White}] ";

    private readonly InstadefuseSettings _settings;

    private float _bombPlantedTime = float.NaN;
    private bool _bombTicking;
    private int _molotovThreat;
    private int _heThreat;
    private List<int> _infernoThreat = new();

    public InstadefuseService(InstadefuseSettings settings)
    {
        _settings = settings;
    }

    private bool Enabled => _settings.IsEnabled;

    public void ResetForNewRound()
    {
        _bombPlantedTime = float.NaN;
        _bombTicking = false;
        _heThreat = 0;
        _molotovThreat = 0;
        _infernoThreat = new List<int>();
    }

    public void OnBombPlanted()
    {
        _bombPlantedTime = Server.CurrentTime;
        _bombTicking = true;
    }

    public void OnGrenadeThrown(string weapon)
    {
        if (!Enabled) return;

        switch (weapon)
        {
            case "hegrenade":
                _heThreat++;
                break;
            case "incgrenade":
            case "molotov":
                _molotovThreat++;
                break;
        }
    }

    public void OnInfernoStartBurn(float x, float y, float z, int entityId)
    {
        if (!Enabled) return;

        var infernoPos = new Vector3(x, y, z);

        var plantedBomb = FindPlantedBomb();
        var bombOrigin = plantedBomb?.CBodyComponent?.SceneNode?.AbsOrigin;
        if (bombOrigin == null) return;

        var distance = Vector3.Distance(infernoPos, new Vector3(bombOrigin.X, bombOrigin.Y, bombOrigin.Z));
        if (distance > _settings.InfernoThreatDistance) return;

        _infernoThreat.Add(entityId);
    }

    public void OnInfernoGone(int entityId) => _infernoThreat.Remove(entityId);

    public void OnHeDetonate()
    {
        if (_heThreat > 0) _heThreat--;
    }

    public void OnMolotovDetonate()
    {
        if (_molotovThreat > 0) _molotovThreat--;
    }

    public void OnBombBeginDefuse(CCSPlayerController? player)
    {
        if (!Enabled) return;
        if (player == null || !player.IsValid || !player.PawnIsAlive) return;

        AttemptInstadefuse(player);
    }

    private void AttemptInstadefuse(CCSPlayerController defuser)
    {
        if (!_bombTicking) return;

        var plantedBomb = FindPlantedBomb();
        if (plantedBomb == null || plantedBomb.CannotBeDefused) return;

        if (_settings.RequireAllTerroristsDead && TeamHasAlivePlayers(CsTeam.Terrorist)) return;

        if (_heThreat > 0 || _molotovThreat > 0 || _infernoThreat.Any())
        {
            Server.PrintToChatAll($"{MessagePrefix}{ChatColors.LightRed}Instant defuse not possible — there is a grenade threat!");
            return;
        }

        var bombTimeUntilDetonation = plantedBomb.TimerLength - (Server.CurrentTime - _bombPlantedTime);

        var defuseLength = plantedBomb.DefuseLength;
        if (defuseLength != 5 && defuseLength != 10)
        {
            defuseLength = defuser.PawnHasDefuser ? 5.0f : 10.0f;
        }

        var timeLeftAfterDefuse = bombTimeUntilDetonation - defuseLength;
        var bombCanBeDefusedInTime = timeLeftAfterDefuse >= 0.0f;

        if (!bombCanBeDefusedInTime)
        {
            Server.PrintToChatAll(
                $"{MessagePrefix}{ChatColors.Default}{defuser.PlayerName} was {ChatColors.DarkRed}{Math.Abs(timeLeftAfterDefuse):n3} seconds{ChatColors.Default} away from defusing.");

            Server.NextFrame(() =>
            {
                var bomb = FindPlantedBomb();
                if (bomb == null) return;
                bomb.C4Blow = 1.0f;
            });

            return;
        }

        Server.NextFrame(() =>
        {
            // Re-fetch the bomb entity as it occasionally crashed otherwise.
            var bomb = FindPlantedBomb();
            if (bomb == null) return;

            bomb.DefuseCountDown = 0;

            Server.PrintToChatAll(
                $"{MessagePrefix}{ChatColors.Default}{defuser.PlayerName} defused with {ChatColors.Green}{Math.Abs(bombTimeUntilDetonation):n3} seconds{ChatColors.Default} left on the bomb.");
        });
    }

    private static bool TeamHasAlivePlayers(CsTeam team)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;
            if (player.Team != team) continue;
            if (!player.PawnIsAlive) continue;

            return true;
        }

        return false;
    }

    private static CPlantedC4? FindPlantedBomb()
    {
        var plantedBombList = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").ToList();
        return plantedBombList.Count > 0 ? plantedBombList.FirstOrDefault() : null;
    }
}
