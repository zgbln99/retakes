using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

/// <summary>
/// Settings for periodic automatic messages: a rotating chat advert plus a short
/// center-screen tip shown at the start of some rounds.
/// </summary>
public class AutoMessageSettings
{
    /// <summary>Master toggle. Also flippable from the admin panel.</summary>
    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>How often (seconds) the next chat message is shown, rotating through the list.</summary>
    [JsonPropertyName("ChatIntervalSeconds")]
    public float ChatIntervalSeconds { get; set; } = 180.0f;

    /// <summary>Show a short center-screen tip at the start of a round.</summary>
    [JsonPropertyName("ShowRoundTip")]
    public bool ShowRoundTip { get; set; } = true;

    /// <summary>Only show the round tip every N rounds (1 = every round).</summary>
    [JsonPropertyName("RoundTipEveryNRounds")]
    public int RoundTipEveryNRounds { get; set; } = 3;

    /// <summary>
    /// Chat messages, rotated in order. Color tags are supported:
    /// {green} {red} {gold} {white} {grey} {lightred} {blue}. The [CWELOWNIA]
    /// prefix is added automatically.
    /// </summary>
    [JsonPropertyName("ChatMessages")]
    public List<string> ChatMessages { get; set; } = new()
    {
        "Wpisz {gold}!guns{white} aby wybrać swoją broń!",
        "Sprawdź swoje statystyki: {gold}!rank{white} — ranking serwera: {gold}!top",
        "Nie podoba Ci się broń? {gold}!guns{white} i wybierz ulubioną!",
        "Po wygranym meczu zagłosujecie na następną mapę."
    };

    /// <summary>
    /// Short tips shown on the center of the screen at round start (rotated).
    /// Plain text (no color tags).
    /// </summary>
    [JsonPropertyName("RoundTips")]
    public List<string> RoundTips { get; set; } = new()
    {
        "Wpisz !guns aby wybrać broń",
        "!rank — twoje statystyki",
        "!top — ranking serwera"
    };
}
