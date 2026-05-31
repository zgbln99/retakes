using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Services;

/// <summary>
/// Periodic automatic messages: a rotating chat advert on a timer plus a short
/// center-screen tip at the start of some rounds. Purely cosmetic.
/// </summary>
public class AutoMessageService
{
    private static readonly string Prefix = $" {ChatColors.Green}[CWELOWNIA]{ChatColors.White} ";

    private readonly BasePlugin _plugin;
    private readonly AutoMessageSettings _settings;

    private int _chatIndex;
    private int _tipIndex;
    private int _roundCounter;

    public AutoMessageService(BasePlugin plugin, AutoMessageSettings settings)
    {
        _plugin = plugin;
        _settings = settings;
    }

    public void Initialize()
    {
        if (!_settings.IsEnabled) return;

        var interval = Math.Max(30.0f, _settings.ChatIntervalSeconds);
        _plugin.AddTimer(interval, ShowNextChatMessage, TimerFlags.REPEAT);
    }

    private void ShowNextChatMessage()
    {
        if (!_settings.IsEnabled || _settings.ChatMessages.Count == 0) return;

        // Don't advertise to an empty server.
        if (!Utilities.GetPlayers().Any(p => p.IsValid && !p.IsBot)) return;

        var message = _settings.ChatMessages[_chatIndex % _settings.ChatMessages.Count];
        _chatIndex++;

        Server.PrintToChatAll(Prefix + ApplyColorTags(message));
    }

    /// <summary>Called on round start to optionally show a center-screen tip.</summary>
    public void OnRoundStart()
    {
        if (!_settings.IsEnabled || !_settings.ShowRoundTip || _settings.RoundTips.Count == 0) return;

        _roundCounter++;
        var every = Math.Max(1, _settings.RoundTipEveryNRounds);
        if (_roundCounter % every != 0) return;

        var tip = _settings.RoundTips[_tipIndex % _settings.RoundTips.Count];
        _tipIndex++;

        var html = $"<font color='#40ff40'>CWELOWNIA</font><br><font color='#ffd000'>{tip}</font>";
        foreach (var player in Utilities.GetPlayers())
        {
            if (player.IsValid && !player.IsBot) player.PrintToCenterHtml(html);
        }
    }

    /// <summary>Replaces {tag} color placeholders with CS2 chat color codes.</summary>
    private static string ApplyColorTags(string message)
    {
        return message
            .Replace("{green}", ChatColors.Green.ToString())
            .Replace("{red}", ChatColors.Red.ToString())
            .Replace("{lightred}", ChatColors.LightRed.ToString())
            .Replace("{gold}", ChatColors.Gold.ToString())
            .Replace("{white}", ChatColors.White.ToString())
            .Replace("{default}", ChatColors.Default.ToString())
            .Replace("{grey}", ChatColors.Grey.ToString())
            .Replace("{blue}", ChatColors.Blue.ToString())
            .Replace("{purple}", ChatColors.Purple.ToString());
    }
}
