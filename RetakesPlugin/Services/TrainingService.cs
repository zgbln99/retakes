using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Consent-based training position preview. While enabled it:
///  1) tints every alive player with the highlight colour (so positions stand out
///     when seen), and
///  2) shows EVERY player a persistent center-screen notice that positions are
///     visible — so the mode is fully transparent and no one is deceived.
///
/// This is deliberately symmetric and announced: it is a practice tool (vs bots
/// or a willing partner), not a hidden advantage in a real match.
/// </summary>
public class TrainingService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly BasePlugin _plugin;
    private readonly TrainingSettings _settings;
    private bool _running;

    public TrainingService(BasePlugin plugin, TrainingSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public bool Enabled => _settings.Enabled;

    public void Start()
    {
        if (_running) return;
        _running = true;
        Server.PrintToChatAll($"{Prefix}{ChatColors.Gold}Tryb treningowy: podgląd pozycji WŁĄCZONY{ChatColors.White} — wszyscy widzą, że pozycje są jawne.");
    }

    public void Stop()
    {
        _running = false;
        Server.PrintToChatAll($"{Prefix}Tryb treningowy: podgląd pozycji wyłączony.");
        ClearHighlights();
    }

    /// <summary>Apply highlights at round start when the mode is on.</summary>
    public void OnRoundStart()
    {
        if (!_settings.Enabled) { _running = false; return; }
        _running = true;
        ApplyHighlights();
    }

    /// <summary>
    /// Repeating tick (called from a timer): keep the consent notice on everyone's
    /// HUD so it can never be hidden, and refresh highlights.
    /// </summary>
    public void Tick()
    {
        if (!_settings.Enabled || !_running) return;

        var notice = $"<font color='#ffd000'>{Esc(_settings.Notice)}</font>";
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && !p.IsBot) p.PrintToCenterHtml(notice);
        }
    }

    private void ApplyHighlights()
    {
        var color = System.Drawing.Color.FromArgb(255, _settings.HighlightR, _settings.HighlightG, _settings.HighlightB);
        foreach (var p in Utilities.GetPlayers())
        {
            var pawn = p.PlayerPawn.Value;
            if (pawn is { IsValid: true } && p.PawnIsAlive)
            {
                pawn.Render = color;
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            var pawn = p.PlayerPawn.Value;
            if (pawn is { IsValid: true })
            {
                pawn.Render = System.Drawing.Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    private static string Esc(string s) => s.Replace("<", "").Replace(">", "");
}
