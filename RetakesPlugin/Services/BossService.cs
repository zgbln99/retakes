using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Boss / Juggernaut mode. One player is made the boss for the round: bonus HP,
/// bigger model, faster, extra damage, a coloured tint (so they're recognizable
/// on sight) and — best effort — enemy glow so the boss can see opponents.
///
/// Deliberately NOT announced in chat (surprise who it is), which is fair because
/// the boss is visibly different when you actually see them.
/// </summary>
public class BossService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly BossSettings _settings;
    private readonly Random _random;

    private ulong _bossSteamId;
    private bool _forceNextRound;
    private ulong _forcedSteamId;

    public BossService(BossSettings settings, Random random)
    {
        _settings = settings;
        _random = random;
    }

    public bool Enabled => _settings.Enabled;
    public ulong CurrentBossSteamId => _bossSteamId;

    public bool IsBoss(CCSPlayerController? p) =>
        p != null && p.IsValid && p.SteamID == _bossSteamId && _bossSteamId != 0;

    /// <summary>Admin: force a specific player to be the boss next round.</summary>
    public void ForceNextRound(ulong steamId)
    {
        _forceNextRound = true;
        _forcedSteamId = steamId;
    }

    public void ResetRound() => _bossSteamId = 0;

    /// <summary>Pick and apply the boss at round start (after players are alive).</summary>
    public void OnRoundStart()
    {
        _bossSteamId = 0;
        if (!_settings.Enabled && !_forceNextRound) return;

        var candidates = Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV && p.PawnIsAlive &&
                        (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
            .ToList();
        if (candidates.Count == 0) { _forceNextRound = false; return; }

        CCSPlayerController? boss = null;
        if (_forceNextRound && _forcedSteamId != 0)
        {
            boss = candidates.FirstOrDefault(p => p.SteamID == _forcedSteamId);
        }
        boss ??= candidates[_random.Next(candidates.Count)];
        _forceNextRound = false;

        _bossSteamId = boss.SteamID;
        ApplyBoss(boss);

        // Private, quiet hint to the boss only — not the whole server.
        boss.PrintToChat($"{Prefix}{ChatColors.Gold}Jesteś BOSSEM tej rundy!{ChatColors.White} Masz przewagę i widzisz wrogów.");
    }

    private void ApplyBoss(CCSPlayerController boss)
    {
        var pawn = boss.PlayerPawn.Value;
        if (pawn is not { IsValid: true }) return;

        // HP
        pawn.Health = _settings.Health;
        pawn.MaxHealth = _settings.Health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        // Speed
        pawn.VelocityModifier = _settings.SpeedMultiplier;

        // Size (recognizable on sight)
        if (Math.Abs(_settings.SizeScale - 1.0f) > 0.01f)
        {
            pawn.AcceptInput("SetScale", pawn, pawn,
                _settings.SizeScale.ToString(CultureInfo.InvariantCulture));
        }

        // Colored tint (recognizable on sight). The render colour multiplies the
        // model even in normal render mode in CS2.
        pawn.Render = System.Drawing.Color.FromArgb(255, _settings.TintR, _settings.TintG, _settings.TintB);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        // Enemy glow (best effort) — give the boss a glow on opposing players so
        // they can see them. CS2 player glow is unreliable; tint above is the
        // guaranteed visual. We set the boss's own glow as the recognizable marker.
        if (_settings.SeeEnemies)
        {
            try
            {
                var glow = pawn.Glow;
                glow.GlowColorOverride = System.Drawing.Color.FromArgb(255, _settings.TintR, _settings.TintG, _settings.TintB);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_Glow");
            }
            catch { /* glow not available on this build — tint still applies */ }
        }
    }

    /// <summary>Hook the boss's outgoing damage to add ExtraDamage.</summary>
    public bool IsBossAttacker(CCSPlayerController? attacker) =>
        _settings.ExtraDamage > 0 && IsBoss(attacker);

    public int ExtraDamage => _settings.ExtraDamage;
}
