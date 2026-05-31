using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Retakes;

[MinimumApiVersion(305)]
public class RetakesPlugin : BasePlugin
{
    public override string ModuleName => "Retakes";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "LTS Logistik";
    public override string ModuleDescription => "Tryb Retakes dla Counter-Strike 2.";

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("Retakes v{Version} załadowany.", ModuleVersion);
    }

    // Komenda czatu: !retakes  ->  wyświetla informacje o pluginie
    [ConsoleCommand("css_retakes", "Informacje o pluginie Retakes")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRetakesCommand(CCSPlayerController? player, CommandInfo command)
    {
        command.ReplyToCommand($" {ChatColors.Green}[Retakes]{ChatColors.Default} wersja {ModuleVersion} działa poprawnie.");
    }

    // Przykładowy event: początek nowej rundy
    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // TODO: rozstawienie graczy, podłożenie bomby, alokacja broni
        Logger.LogInformation("Nowa runda rozpoczęta — logika retakes do uzupełnienia.");
        return HookResult.Continue;
    }
}
