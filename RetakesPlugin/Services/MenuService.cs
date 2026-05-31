using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Services;

/// <summary>
/// Central menu framework for the whole plugin. Every menu is rendered through
/// this one service so they look and behave identically (the SimpleAdmin-style
/// CenterHtmlMenu: W/S to move, E to select). It adds:
///  - a per-player Back stack (« Wstecz returns to the previous screen)
///  - a Close button (CenterHtmlMenu.ExitButton)
///  - native Next/Prev pagination (CenterHtmlMenu.MenuItemsPerPage)
///  - guards for null / invalid players and for map changes
///  - CloseAll on unload / map change
///
/// CenterHtmlMenu is the same menu type CS2 SimpleAdmin uses by default; the
/// external ScreenMenu/WASD library is NOT part of CounterStrikeSharp 1.0.x, so
/// it is intentionally not used (it would require a separate server-side plugin).
/// </summary>
public class MenuService
{
    private readonly BasePlugin _plugin;
    private readonly Func<bool> _isBusy;

    // Per-player navigation stack of "open this screen" delegates. The top is the
    // currently shown screen; Back pops it and re-opens the one beneath.
    private readonly Dictionary<ulong, Stack<Action<CCSPlayerController>>> _stacks = new();

    public MenuService(BasePlugin plugin, Func<bool> isBusy)
    {
        _plugin = plugin;
        _isBusy = isBusy;
    }

    /// <summary>Opens a screen as a new root menu (clears the player's back history).</summary>
    public void OpenRoot(CCSPlayerController player, Action<CCSPlayerController> screen)
    {
        if (!CanOpen(player)) return;
        _stacks[player.SteamID] = new Stack<Action<CCSPlayerController>>();
        screen(player);
    }

    /// <summary>
    /// Renders a screen. Call this from inside a screen delegate. <paramref name="self"/>
    /// is the delegate that opens this same screen (used to rebuild it after a
    /// selection); pass the method you're in. Adds Back automatically when there is
    /// a parent screen on the stack.
    /// </summary>
    public void Show(CCSPlayerController player, string title, Action<IMenuBuilder> populate,
        Action<CCSPlayerController> self)
    {
        if (!CanOpen(player)) return;

        var stack = GetStack(player);

        // Track navigation: if this screen isn't the current top, it's a new push.
        if (stack.Count == 0 || stack.Peek() != self)
        {
            stack.Push(self);
        }

        // CenterHtmlMenu paginates automatically; ExitButton renders the Close item.
        var menu = new CenterHtmlMenu(title, _plugin)
        {
            ExitButton = true
        };

        var builder = new MenuBuilder(menu);
        populate(builder);

        // Back option when there is a previous screen.
        if (stack.Count > 1)
        {
            menu.AddMenuOption($"{ChatColors.Grey}« Wstecz", (p, _) =>
            {
                if (!CanOpen(p)) return;
                var s = GetStack(p);
                if (s.Count > 1)
                {
                    s.Pop();                 // remove current
                    var previous = s.Peek(); // re-open previous
                    previous(p);
                }
            });
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
    }

    /// <summary>Closes the player's menu and clears their navigation history.</summary>
    public void Close(CCSPlayerController player)
    {
        if (player.IsValid) MenuManager.CloseActiveMenu(player);
        _stacks.Remove(player.SteamID);
    }

    /// <summary>Closes every open menu (call on map change / plugin unload).</summary>
    public void CloseAll()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && !p.IsBot) MenuManager.CloseActiveMenu(p);
        }
        _stacks.Clear();
    }

    public void OnPlayerDisconnect(ulong steamId) => _stacks.Remove(steamId);

    private bool CanOpen(CCSPlayerController? player) =>
        player is { IsValid: true } && !_isBusy();

    private Stack<Action<CCSPlayerController>> GetStack(CCSPlayerController player)
    {
        if (!_stacks.TryGetValue(player.SteamID, out var stack))
        {
            stack = new Stack<Action<CCSPlayerController>>();
            _stacks[player.SteamID] = stack;
        }
        return stack;
    }
}

/// <summary>Fluent surface passed to screen populators.</summary>
public interface IMenuBuilder
{
    void AddOption(string text, Action<CCSPlayerController> onSelect, bool disabled = false);
}

internal sealed class MenuBuilder : IMenuBuilder
{
    private readonly CenterHtmlMenu _menu;

    public MenuBuilder(CenterHtmlMenu menu) => _menu = menu;

    public void AddOption(string text, Action<CCSPlayerController> onSelect, bool disabled = false)
    {
        _menu.AddMenuOption(text, (player, _) =>
        {
            if (player is { IsValid: true }) onSelect(player);
        }, disabled);
    }
}
