using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

/// <summary>
/// !rtv — rock the vote: request a map change. Once enough players agree a map
/// vote opens for everyone.
/// </summary>
public class RtvCommand
{
    private readonly MapVoteService _mapVoteService;

    public RtvCommand(MapVoteService mapVoteService)
    {
        _mapVoteService = mapVoteService;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!PlayerHelper.IsValid(player)) return;
        _mapVoteService.OnRtv(player!);
    }
}
