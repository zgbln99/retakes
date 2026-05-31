using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// FaceIt-style per-round damage report. Tracks damage dealt between players
/// during a round and, at round end, privately prints each player a breakdown:
/// "to X: 87 dmg (3 hits) — left 13 HP" and (optionally) the damage they took.
/// Health values are the victim's remaining HP after each hit.
/// </summary>
public class DamageReportService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly DamageReportSettings _settings;

    private sealed class Hit
    {
        public int Damage;
        public int Hits;
        public int VictimHpLeft = 100;
        public string OtherName = "";
    }

    // dealt[attacker][victim] and taken is derived by swapping.
    private readonly Dictionary<ulong, Dictionary<ulong, Hit>> _dealt = new();
    private readonly Dictionary<ulong, string> _names = new();

    public DamageReportService(DamageReportSettings settings)
    {
        _settings = settings;
    }

    public bool Enabled => _settings.Enabled;

    public void Reset()
    {
        _dealt.Clear();
        _names.Clear();
    }

    public void OnPlayerHurt(EventPlayerHurt @event)
    {
        if (!_settings.Enabled) return;

        var attacker = @event.Attacker;
        var victim = @event.Userid;
        var aid = SteamId(attacker);
        var vid = SteamId(victim);
        if (aid == 0 || vid == 0 || aid == vid || attacker == null || victim == null) return;

        _names[aid] = attacker.PlayerName;
        _names[vid] = victim.PlayerName;

        if (!_dealt.TryGetValue(aid, out var perVictim))
        {
            perVictim = new Dictionary<ulong, Hit>();
            _dealt[aid] = perVictim;
        }
        if (!perVictim.TryGetValue(vid, out var hit))
        {
            hit = new Hit { OtherName = victim.PlayerName };
            perVictim[vid] = hit;
        }

        hit.Damage += Math.Max(0, @event.DmgHealth);
        hit.Hits++;
        hit.VictimHpLeft = Math.Max(0, @event.Health);   // remaining HP after this hit
        hit.OtherName = victim.PlayerName;
    }

    /// <summary>Prints each player their personal damage report.</summary>
    public void PrintReports()
    {
        if (!_settings.Enabled) return;

        foreach (var player in Utilities.GetPlayers())
        {
            var id = SteamId(player);
            if (id == 0) continue;

            player.PrintToChat($"{Prefix}{ChatColors.Gold}Raport obrażeń (runda):");

            // Damage dealt by this player.
            if (_dealt.TryGetValue(id, out var dealt) && dealt.Count > 0)
            {
                foreach (var kv in dealt.OrderByDescending(x => x.Value.Damage))
                {
                    var h = kv.Value;
                    var hpText = h.VictimHpLeft <= 0 ? $"{ChatColors.Red}zabity" : $"{ChatColors.Grey}został {ChatColors.Green}{h.VictimHpLeft} HP";
                    player.PrintToChat(
                        $" {ChatColors.Grey}Do {ChatColors.White}{h.OtherName}{ChatColors.Grey}: {ChatColors.Gold}{h.Damage}{ChatColors.Grey} dmg ({h.Hits} traf.) — {hpText}");
                }
            }
            else
            {
                player.PrintToChat($" {ChatColors.Grey}Brak zadanych obrażeń.");
            }

            // Damage taken (optional).
            if (_settings.ShowDamageTaken)
            {
                var taken = CollectTaken(id);
                foreach (var t in taken.OrderByDescending(x => x.Value.Damage))
                {
                    player.PrintToChat(
                        $" {ChatColors.Grey}Od {ChatColors.White}{t.Value.OtherName}{ChatColors.Grey}: {ChatColors.Red}{t.Value.Damage}{ChatColors.Grey} dmg ({t.Value.Hits} traf.)");
                }
            }
        }
    }

    private Dictionary<ulong, Hit> CollectTaken(ulong victimId)
    {
        var taken = new Dictionary<ulong, Hit>();
        foreach (var (attackerId, perVictim) in _dealt)
        {
            if (!perVictim.TryGetValue(victimId, out var h)) continue;
            taken[attackerId] = new Hit
            {
                Damage = h.Damage,
                Hits = h.Hits,
                OtherName = _names.TryGetValue(attackerId, out var n) ? n : "?"
            };
        }
        return taken;
    }

    private static ulong SteamId(CCSPlayerController? p)
    {
        if (p == null || !p.IsValid || p.IsBot || p.IsHLTV) return 0;
        return p.SteamID > 0 ? p.SteamID : 0;
    }
}
