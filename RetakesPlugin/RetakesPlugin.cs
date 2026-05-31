using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using RetakesPluginShared;
using System.Text.Json;

using RetakesPlugin.Configs;
using RetakesPlugin.Configs.JsonConverters;
using RetakesPlugin.Events;
using RetakesPlugin.Managers;
using RetakesPlugin.Models;
using RetakesPlugin.Modules;
using RetakesPlugin.Services;
using RetakesPlugin.Services.Stats;
using RetakesPlugin.Utils;

using RetakesPlugin.Commands.Admin;
using RetakesPlugin.Commands.MapConfig;
using RetakesPlugin.Commands.Player;
using RetakesPlugin.Commands.SpawnEditor;

namespace RetakesPlugin;

[MinimumApiVersion(345)]
public class RetakesPlugin : BasePlugin, IPluginConfig<BaseConfigs>
{
    public const string Version = "3.0.4";

    #region Plugin Info
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "https://github.com/b3none/cs2-retakes";
    #endregion

    #region Configuration
    public required BaseConfigs Config { get; set; }

    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
        Utils.Logger.Initialize(Config.Debug.IsDebugMode);
        Utils.Logger.LogInfo("Main", "Configuration parsed successfully");
    }
    #endregion

    #region Services & Managers
    private readonly Random _random = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private GameManager? _gameManager;
    private SpawnManager? _spawnManager;
    private BreakerManager? _breakerManager;
    private MapConfigService? _mapConfigService;
    private AllocationService? _allocationService;
    private AnnouncementService? _announcementService;
    private RoundEventHandlers? _roundEventHandlers;
    private PlayerEventHandlers? _playerEventHandlers;

    // Extended feature services
    private InstadefuseService? _instadefuseService;
    private AdminMenuService? _adminMenuService;
    private WeaponAllocationService? _weaponAllocationService;
    private StatsService? _statsService;
    private MapVoteService? _mapVoteService;
    private KillFeedService? _killFeedService;
    private AutoMessageService? _autoMessageService;
    private AdminFunModeMenu? _adminFunModeMenu;
    private MenuService? _menuService;

    public MapConfigService? MapConfigService => _mapConfigService;
    public SpawnManager? SpawnManager => _spawnManager;
    public WeaponAllocationService? WeaponAllocation => _weaponAllocationService;
    #endregion

    #region Commands
    // Admin Commands
    private ForceBombsiteCommand? _forceBombsiteCommand;
    private ForceBombsiteStopCommand? _forceBombsiteStopCommand;
    private ScrambleCommand? _scrambleCommand;
    private DebugQueuesCommand? _debugQueuesCommand;

    // Map Config Commands
    private MapConfigCommand? _mapConfigCommand;
    private MapConfigsCommand? _mapConfigsCommand;

    // Player Commands
    private VoicesCommand? _voicesCommand;

    // Spawn Editor Commands
    private ShowSpawnsCommand? _showSpawnsCommand;
    private AddSpawnCommand? _addSpawnCommand;
    private RemoveSpawnCommand? _removeSpawnCommand;
    private NearestSpawnCommand? _nearestSpawnCommand;
    private HideSpawnsCommand? _hideSpawnsCommand;
    #endregion

    #region Capabilities
    public static PluginCapability<IRetakesPluginEventSender> RetakesPluginEventSenderCapability { get; } = new("retakes_plugin:event_sender");
    #endregion

    #region State
    private readonly HashSet<CCSPlayerController> _hasMutedVoices = [];

    /// <summary>
    /// Set true the moment a map change is initiated (vote finished / admin). While
    /// true, all game-logic event handlers and timers bail out early, so nothing
    /// touches entities the engine is unloading during ChangeLevel. Reset on the
    /// next OnMapStart. volatile because timer/event callbacks may run on different
    /// threads than the one that sets it.
    /// </summary>
    private volatile bool _isChangingMap;

    public bool IsChangingMap => _isChangingMap;

    /// <summary>
    /// Called right before a ChangeLevel. Freezes all plugin game logic and kills
    /// the repeating timers so nothing fires while the map is unloading.
    /// </summary>
    public void BeginMapChange()
    {
        _isChangingMap = true;
        Utils.Logger.LogInfo("MapChange", "Map change starting — plugin logic frozen");

        // Stop periodic work that would otherwise fire during the unload.
        _statsService?.StopTimers();
        _autoMessageService?.StopTimers();

        // Close any open menus so no menu callback runs during the unload.
        _menuService?.CloseAll();
    }
    #endregion

    public RetakesPlugin()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new VectorJsonConverter(),
                new QAngleJsonConverter()
            }
        };
    }

    public override void Load(bool hotReload)
    {
        Utils.Logger.LogInfo("Main", "Plugin loading...");

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        AddCommandListener("jointeam", OnCommandJoinTeam);

        var retakesPluginEventSender = new RetakesPluginEventSender();
        Capabilities.RegisterPluginCapability(RetakesPluginEventSenderCapability, () => retakesPluginEventSender);

        // Register event handlers
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundPoststart>(OnRoundPostStart);
        RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted, HookMode.Pre);
        RegisterEventHandler<EventBombDefused>(OnBombDefused);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam, HookMode.Pre);

        // Built-in instadefuse (adapted from B3none/cs2-instadefuse, GPL-3.0 — see NOTICE)
        _instadefuseService = new InstadefuseService(Config.Instadefuse);
        RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        RegisterEventHandler<EventInfernoStartburn>(OnInfernoStartBurn);
        RegisterEventHandler<EventInfernoExtinguish>(OnInfernoExtinguish);
        RegisterEventHandler<EventInfernoExpire>(OnInfernoExpire);
        RegisterEventHandler<EventHegrenadeDetonate>(OnHeGrenadeDetonate);
        RegisterEventHandler<EventMolotovDetonate>(OnMolotovDetonate);
        RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);

        // Built-in weapon allocator (random allocation + !guns preferences)
        _weaponAllocationService = new WeaponAllocationService(Config.Weapon, _random);
        // Persist preferences in the same MySQL database as stats, whenever the DB
        // is configured via config or env vars (independent of the stats toggle).
        if (Services.Stats.DbConnectionFactory.IsConfigured(Config.Stats.Database))
        {
            _weaponAllocationService.AttachRepository(
                new MySqlWeaponPreferenceRepository(Config.Stats.Database));
        }
        // Shared menu framework (Back / Close / pagination / guards). Frozen during
        // map changes via the same flag the event handlers use.
        _menuService = new MenuService(this, () => _isChangingMap);

        var gunsCommand = new GunsCommand(_menuService, _weaponAllocationService);
        AddCommand("css_guns", "Choose your preferred weapons.", gunsCommand.OnCommand);
        AddCommand("css_gun", "Choose your preferred weapons.", gunsCommand.OnCommand);
        AddCommand("css_weapon", "Choose your preferred weapons.", gunsCommand.OnCommand);

        // PvP statistics (MySQL). Disabled until configured; fails safe if the DB is down.
        var statsRepository = new MySqlStatsRepository(Config.Stats.Database);
        _statsService = new StatsService(this, Config.Stats, statsRepository);
        _statsService.Initialize();
        var rankCommand = new RankCommand(_statsService);
        var topCommand = new TopCommand(_statsService);
        AddCommand("css_rank", "Show your PvP stats.", rankCommand.OnCommand);
        AddCommand("css_stats", "Show your PvP stats.", rankCommand.OnCommand);
        AddCommand("css_top", "Show the PvP leaderboard.", topCommand.OnCommand);

        // Admin DB connection test
        var dbTestCommand = new DbTestCommand(Config.Stats.Database);
        AddCommand("css_dbtest", "Test the MySQL database connection.", dbTestCommand.OnCommand);

        // On-screen / chat HUD: kill streaks, dominations, bomb location
        _killFeedService = new KillFeedService(() => Config.Hud.IsEnabled && Config.Hud.ShowKillStreaks);

        // Periodic automatic messages (chat advert + round-start tip)
        _autoMessageService = new AutoMessageService(this, Config.AutoMessage);
        _autoMessageService.Initialize();

        // Player-driven map vote (rock the vote)
        _mapVoteService = new MapVoteService(this, _menuService, Config.MapVote, _random);
        // Freeze all plugin logic just before the map actually changes.
        _mapVoteService.OnBeginMapChange = BeginMapChange;
        var rtvCommand = new RtvCommand(_mapVoteService);
        AddCommand("css_rtv", "Rock the vote — request a map change.", rtvCommand.OnCommand);
        AddCommand("css_votemap", "Rock the vote — request a map change.", rtvCommand.OnCommand);

        // In-game admin panel (GUI) + runtime feature toggles
        SetupAdminMenu();

        if (hotReload)
        {
            Utils.Logger.LogServer($"Update detected, restarting map...");
            Server.ExecuteCommand($"map {Server.MapName}");
        }

        Utils.Logger.LogInfo("Main", "Plugin loaded successfully");
    }

    #region Admin Panel
    private void SetupAdminMenu()
    {
        if (!Config.AdminMenu.IsEnabled)
        {
            Utils.Logger.LogInfo("AdminMenu", "Admin panel is disabled in config");
            return;
        }

        _adminMenuService = new AdminMenuService(_menuService!, Config.AdminMenu);

        // Feature toggles (bound live to the config object the services read from).
        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "instadefuse",
            DisplayName = "Instadefuse",
            Get = () => Config.Instadefuse.IsEnabled,
            Set = value => Config.Instadefuse.IsEnabled = value
        });

        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "weapons",
            DisplayName = "Przydział broni",
            Get = () => Config.Weapon.IsEnabled,
            Set = value => Config.Weapon.IsEnabled = value
        });

        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "weapon_preferences",
            DisplayName = "Wybór broni (!guns)",
            Get = () => Config.Weapon.AllowPreferences,
            Set = value => Config.Weapon.AllowPreferences = value
        });

        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "stats",
            DisplayName = "Zapis statystyk PvP",
            Get = () => Config.Stats.IsEnabled,
            Set = value => Config.Stats.IsEnabled = value
        });

        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "mapvote",
            DisplayName = "Głosowanie na mapę (!rtv)",
            Get = () => Config.MapVote.IsEnabled,
            Set = value => Config.MapVote.IsEnabled = value
        });

        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "hud",
            DisplayName = "Komunikaty HUD (bomba/serie)",
            Get = () => Config.Hud.IsEnabled,
            Set = value => Config.Hud.IsEnabled = value
        });

        _adminMenuService.RegisterToggle(new FeatureToggle
        {
            Key = "automsg",
            DisplayName = "Automatyczne wiadomości",
            Get = () => Config.AutoMessage.IsEnabled,
            Set = value => Config.AutoMessage.IsEnabled = value
        });

        // Weapon-set submenu (force a global loadout / back to random).
        if (_weaponAllocationService != null)
        {
            var weaponSetMenu = new AdminWeaponSetMenu(_menuService!, _weaponAllocationService);
            _adminMenuService.RegisterSubmenu(new AdminSubmenu
            {
                DisplayName = "Wybór zestawu broni",
                Open = weaponSetMenu.Open
            });

            // Fun-mode submenu (symmetric, server-wide).
            if (Config.Fun.IsEnabled)
            {
                _adminFunModeMenu = new AdminFunModeMenu(_menuService!, _weaponAllocationService, Config.Fun);
                var funMenu = _adminFunModeMenu;
                _adminMenuService.RegisterSubmenu(new AdminSubmenu
                {
                    DisplayName = "Tryb fun (noże/deagle/HE/scout/low-grav)",
                    Open = funMenu.Open
                });
            }
        }

        // Per-player actions submenu (slay / kick / move).
        var playerMenu = new AdminPlayerMenu(_menuService!);
        _adminMenuService.RegisterSubmenu(new AdminSubmenu
        {
            DisplayName = "Akcje na graczu (slay/kick/move)",
            Open = playerMenu.Open
        });

        // Change map now submenu (direct changelevel, freezes plugin first).
        if (_mapVoteService != null)
        {
            var changeMapMenu = new AdminChangeMapMenu(_menuService!, Config.MapVote, BeginMapChange);
            _adminMenuService.RegisterSubmenu(new AdminSubmenu
            {
                DisplayName = "Zmień mapę teraz",
                Open = changeMapMenu.Open
            });
        }

        // Round actions (reuse existing registered commands / direct callbacks).
        _adminMenuService.RegisterAction(new AdminAction { DisplayName = "Wymieszaj drużyny (następna runda)", Command = "css_scramble" });
        _adminMenuService.RegisterAction(new AdminAction { DisplayName = "Wymuś bombsite A", Command = "css_forcebombsite A" });
        _adminMenuService.RegisterAction(new AdminAction { DisplayName = "Wymuś bombsite B", Command = "css_forcebombsite B" });
        _adminMenuService.RegisterAction(new AdminAction { DisplayName = "Przestań wymuszać bombsite", Command = "css_forcebombsitestop" });

        if (_mapVoteService != null)
        {
            _adminMenuService.RegisterAction(new AdminAction
            {
                DisplayName = "Rozpocznij głosowanie na mapę",
                Execute = admin => _mapVoteService.ForceStartVote(admin)
            });
        }

        var adminMenuCommand = new AdminMenuCommand(_adminMenuService);
        foreach (var alias in Config.AdminMenu.OpenCommands)
        {
            var commandName = alias.StartsWith("css_") ? alias : $"css_{alias}";
            AddCommand(commandName, "Opens the Retakes admin panel.", adminMenuCommand.OnCommand);
        }

        Utils.Logger.LogInfo("AdminMenu", "Admin panel initialized");
    }
    #endregion

    #region Map Initialization
    private void OnMapStart(string mapName)
    {
        Utils.Logger.LogInfo("MapStart", $"Map started: {mapName}");

        // New map has loaded — unfreeze plugin logic.
        _isChangingMap = false;

        _mapVoteService?.Reset();
        SpawnService.Reset();

        AddTimer(1.0f, ServerHelper.ExecuteRetakesConfiguration);

        InitializeServices(mapName);
    }

    private void InitializeServices(string mapName, string? customMapConfig = null)
    {
        try
        {
            // Initialize MapConfigService
            _mapConfigService = new MapConfigService(ModuleDirectory, customMapConfig ?? mapName, _jsonOptions);
            _mapConfigService.Load();

            // Initialize Managers
            _spawnManager = new SpawnManager(_mapConfigService);
            _allocationService = new AllocationService(_random);

            _gameManager = new GameManager(
                this,
                new QueueManager(
                    this,
                    Config.Game.MaxPlayers,
                    Config.Team.TerroristRatio,
                    Config.Queue.GetPriorityFlags(),
                    Config.Queue.GetImmunityFlags(),
                    Config.Team.ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10,
                    Config.Team.ShouldPreventTeamChangesMidRound
                ),
                Config.Team.RoundsToScramble,
                Config.Team.IsScrambleEnabled,
                Config.Queue.ShouldRemoveSpectators,
                Config.Team.IsBalanceEnabled
            );

            _breakerManager = new BreakerManager(
                Config.Game.ShouldBreakBreakables,
                Config.Game.ShouldOpenDoors
            );

            _announcementService = new AnnouncementService(
                this,
                _random,
                _hasMutedVoices,
                Config.MapConfig.EnableBombsiteAnnouncementVoices,
                Config.MapConfig.EnableBombsiteAnnouncementCenter
            );

            // Initialize Event Handlers
            _roundEventHandlers = new RoundEventHandlers(
                this,
                _gameManager,
                _spawnManager,
                _breakerManager,
                _allocationService,
                _announcementService,
                Config.Bomb.IsAutoPlantEnabled,
                Config.Game.EnableFallbackAllocation,
                Config.MapConfig.EnableFallbackBombsiteAnnouncement,
                _random
            );

            _playerEventHandlers = new PlayerEventHandlers(this, _gameManager, _hasMutedVoices);

            // Initialize Commands
            _forceBombsiteCommand = new ForceBombsiteCommand(this, _roundEventHandlers);
            _forceBombsiteStopCommand = new ForceBombsiteStopCommand(this, _roundEventHandlers);
            _scrambleCommand = new ScrambleCommand(this, _gameManager);
            _debugQueuesCommand = new DebugQueuesCommand(this, _gameManager);

            _mapConfigCommand = new MapConfigCommand(this, ModuleDirectory, (configName) =>
            {
                InitializeServices(Server.MapName, configName);
            });
            _mapConfigsCommand = new MapConfigsCommand(this, ModuleDirectory);

            _voicesCommand = new VoicesCommand(this, Config, _hasMutedVoices);

            _showSpawnsCommand = new ShowSpawnsCommand(this);
            _addSpawnCommand = new AddSpawnCommand(this, _showSpawnsCommand);
            _removeSpawnCommand = new RemoveSpawnCommand(this, _showSpawnsCommand);
            _nearestSpawnCommand = new NearestSpawnCommand(this, _showSpawnsCommand);
            _hideSpawnsCommand = new HideSpawnsCommand(this, _showSpawnsCommand);

            // Set command references in event handlers
            _roundEventHandlers?.SetCommandReferences(_showSpawnsCommand);

            // Register all commands
            RegisterCommands();

            Utils.Logger.LogInfo("Services", "All services initialized successfully");
        }
        catch (Exception ex)
        {
            Utils.Logger.LogException("Services", ex);
        }
    }

    private void RegisterCommands()
    {
        if (_forceBombsiteCommand == null || _forceBombsiteStopCommand == null || _scrambleCommand == null || _debugQueuesCommand == null || _mapConfigCommand == null || _mapConfigsCommand == null || _voicesCommand == null || _showSpawnsCommand == null || _addSpawnCommand == null || _removeSpawnCommand == null || _nearestSpawnCommand == null || _hideSpawnsCommand == null)
        {
            Utils.Logger.LogWarning("Commands", "Cannot register commands - command handlers not initialized");
            return;
        }

        // Admin Commands
        AddCommand("css_forcebombsite", "Force the retakes to occur from a single bombsite.", _forceBombsiteCommand.OnCommand);
        AddCommand("css_forcebombsitestop", "Clear the forced bombsite and return back to normal.", _forceBombsiteStopCommand.OnCommand);
        AddCommand("css_scramble", "Sets teams to scramble on the next round.", _scrambleCommand.OnCommand);
        AddCommand("css_scrambleteams", "Sets teams to scramble on the next round.", _scrambleCommand.OnCommand);
        AddCommand("css_debugqueues", "Prints the state of the queues to the console.", _debugQueuesCommand.OnCommand);

        // Map Config Commands
        AddCommand("css_mapconfig", "Forces a specific map config file to load.", _mapConfigCommand.OnCommand);
        AddCommand("css_setmapconfig", "Forces a specific map config file to load.", _mapConfigCommand.OnCommand);
        AddCommand("css_loadmapconfig", "Forces a specific map config file to load.", _mapConfigCommand.OnCommand);
        AddCommand("css_mapconfigs", "Displays a list of available map configs.", _mapConfigsCommand.OnCommand);
        AddCommand("css_viewmapconfigs", "Displays a list of available map configs.", _mapConfigsCommand.OnCommand);
        AddCommand("css_listmapconfigs", "Displays a list of available map configs.", _mapConfigsCommand.OnCommand);

        // Spawn Editor Commands
        AddCommand("css_showspawns", "Show the spawns for the specified bombsite.", _showSpawnsCommand.OnCommand);
        AddCommand("css_spawns", "Show the spawns for the specified bombsite.", _showSpawnsCommand.OnCommand);
        AddCommand("css_edit", "Show the spawns for the specified bombsite.", _showSpawnsCommand.OnCommand);
        AddCommand("css_add", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_addspawn", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_new", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_newspawn", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_remove", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_removespawn", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_delete", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_deletespawn", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_nearestspawn", "Goes to nearest retakes spawn.", _nearestSpawnCommand.OnCommand);
        AddCommand("css_nearest", "Goes to nearest retakes spawn.", _nearestSpawnCommand.OnCommand);
        AddCommand("css_hidespawns", "Exits the spawn editing mode.", _hideSpawnsCommand.OnCommand);
        AddCommand("css_done", "Exits the spawn editing mode.", _hideSpawnsCommand.OnCommand);
        AddCommand("css_exitedit", "Exits the spawn editing mode.", _hideSpawnsCommand.OnCommand);

        // Player Commands
        AddCommand("css_voices", "Toggles whether or not you want to hear bombsite voice announcements.", _voicesCommand.OnCommand);

        Utils.Logger.LogInfo("Commands", "All commands registered successfully");
    }
    #endregion

    #region Event Handlers
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;

        var player = @event.Userid;
        if (player is { IsValid: true, IsBot: false })
        {
            _statsService?.OnPlayerConnect(player.SteamID, player.PlayerName);
            _weaponAllocationService?.LoadPreference(player.SteamID);
        }

        return _playerEventHandlers?.OnPlayerConnectFull(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        return _roundEventHandlers?.OnRoundPreStart(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;

        _instadefuseService?.ResetForNewRound();
        _killFeedService?.Reset();
        _autoMessageService?.OnRoundStart();

        // Re-apply the gravity cvar for the active fun mode (resets on map change).
        if (_adminFunModeMenu != null && _weaponAllocationService != null)
        {
            _adminFunModeMenu.ApplyGravity(_weaponAllocationService.ActiveFunMode);
        }

        return _roundEventHandlers?.OnRoundStart(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        return _roundEventHandlers?.OnRoundPostStart(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        return _roundEventHandlers?.OnRoundFreezeEnd(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;

        _statsService?.OnRoundEnd();
        return _roundEventHandlers?.OnRoundEnd(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _mapVoteService?.OnMatchEnd();
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        return _playerEventHandlers?.OnPlayerSpawn(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;

        _statsService?.OnPlayerDeath(@event);
        _killFeedService?.OnPlayerDeath(@event);
        return _playerEventHandlers?.OnPlayerDeath(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;

        _instadefuseService?.OnBombPlanted();
        return _roundEventHandlers?.OnBombPlanted(@event, info) ?? HookResult.Continue;
    }

    #region Instadefuse Event Handlers
    private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnGrenadeThrown(@event.Weapon);
        return HookResult.Continue;
    }

    private HookResult OnInfernoStartBurn(EventInfernoStartburn @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnInfernoStartBurn(@event.X, @event.Y, @event.Z, @event.Entityid);
        return HookResult.Continue;
    }

    private HookResult OnInfernoExtinguish(EventInfernoExtinguish @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnInfernoGone(@event.Entityid);
        return HookResult.Continue;
    }

    private HookResult OnInfernoExpire(EventInfernoExpire @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnInfernoGone(@event.Entityid);
        return HookResult.Continue;
    }

    private HookResult OnHeGrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnHeDetonate();
        return HookResult.Continue;
    }

    private HookResult OnMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnMolotovDetonate();
        return HookResult.Continue;
    }

    private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        _instadefuseService?.OnBombBeginDefuse(@event.Userid);
        return HookResult.Continue;
    }
    #endregion

    private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        return _roundEventHandlers?.OnBombDefused(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        // Always let stats flush a disconnecting player, but skip the retakes
        // queue logic during a map change (it touches teams/entities being torn down).
        var player = @event.Userid;
        if (player is { IsValid: true, IsBot: false })
        {
            _statsService?.OnPlayerDisconnect(player.SteamID);
            _menuService?.OnPlayerDisconnect(player.SteamID);
        }

        if (_isChangingMap) return HookResult.Continue;

        return _playerEventHandlers?.OnPlayerDisconnect(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (_isChangingMap) return HookResult.Continue;
        return _playerEventHandlers?.OnPlayerTeam(@event, info) ?? HookResult.Continue;
    }
    #endregion

    #region Command Handlers
    private HookResult OnCommandJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        // Don't touch teams/queues while the map is unloading.
        if (_isChangingMap) return HookResult.Continue;

        // EXPERIMENTAL free team choice: let the engine handle jointeam directly,
        // bypassing the retakes queue/balance. See TeamSettings.AllowFreeTeamChoice.
        if (Config.Team.AllowFreeTeamChoice)
        {
            return HookResult.Continue;
        }

        if (_gameManager == null)
        {
            Utils.Logger.LogWarning("Commands", "Game manager not loaded");
            return HookResult.Continue;
        }

        if (!PlayerHelper.IsValid(player) || commandInfo.ArgCount < 2 ||
            !Enum.TryParse<CounterStrikeSharp.API.Modules.Utils.CsTeam>(commandInfo.GetArg(1), out var toTeam))
        {
            return HookResult.Handled;
        }

        var fromTeam = player!.Team;
        Utils.Logger.LogDebug("Commands", $"[{player.PlayerName}] {fromTeam} -> {toTeam}");

        _gameManager.QueueManager.DebugQueues(true);
        var response = _gameManager.QueueManager.PlayerJoinedTeam(player, fromTeam, toTeam);
        _gameManager.QueueManager.DebugQueues(false);

        if (_gameManager.QueueManager.ActivePlayers.Count == 0)
        {
            Utils.Logger.LogDebug("Commands", "No active players, updating queue and restarting game");
            _gameManager.QueueManager.ClearRoundTeams();
            _gameManager.QueueManager.Update();
            GameRulesHelper.RestartGame();
        }

        return response;
    }
    #endregion

    public override void Unload(bool hotReload)
    {
        Utils.Logger.LogInfo("Main", "Plugin unloading...");
        _menuService?.CloseAll();
        base.Unload(hotReload);
    }
}
