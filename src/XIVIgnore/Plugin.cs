// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVIgnore.Awareness;
using XIVIgnore.Core.Abstractions;
using XIVIgnore.Core.Services;
using XIVIgnore.EntryPoints;
using XIVIgnore.Filters;
using XIVIgnore.Logging;
using XIVIgnore.Services;
using XIVIgnore.Core.Localization;
using XIVIgnore.Localization;
using XIVIgnore.UI;
#if DEBUG
using XIVIgnore.Diagnostics;
#endif

namespace XIVIgnore;

public sealed class Plugin : IDalamudPlugin
{
    internal Configuration Config { get; }
    internal IgnoreStore Store { get; }
    internal PlayerMatcher Matcher { get; }

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly SystemClock _clock = new();
    private readonly WindowSystem _windowSystem = new("XIVIgnore");
    private readonly WorldResolver _worldResolver;
    private readonly MainWindow _mainWindow;
    private readonly SafetyGate _safetyGate;
    private readonly ChatFilter _chatFilter;
    private readonly PartyFinderFilter _partyFinderFilter;
    private readonly NameplateFilter _nameplateFilter;
    private readonly CharacterHideFilter _characterHideFilter;
    private readonly CommandHandler _commandHandler;
    private readonly ContextMenuHandler _contextMenuHandler;
    private readonly Localizer _localizer;
    private readonly PartyAwareness _partyAwareness;
    private readonly PartyListMarker _partyListMarker;
    private readonly SocialPartyMarker _socialPartyMarker;
    private readonly SocialListMarker _friendListMarker;
    private readonly SocialListMarker _playerSearchMarker;
    private readonly UpdateNotifier _updateNotifier;
    private readonly AddConfirmWindow _addConfirmWindow;
    private readonly BlacklistReader _blacklistReader;
    private readonly BlacklistImportWindow _blacklistImportWindow;
#if DEBUG
    private readonly PatchHealthCheck _patchHealthCheck;
#endif

    private const int PruneIntervalSeconds = 30;
    private DateTimeOffset _lastPrune = DateTimeOffset.MinValue;

    // Constructor injection: Dalamud injects these services on the ONE-TIME instantiation.
    // NO pluginInterface.Create<Plugin>() — that would recursively rebuild the plugin (runaway memory).
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IObjectTable objectTable,
        IPluginLog log,
        IFramework framework,
        IDataManager dataManager,
        IPartyList partyList,
        ICondition condition,
        IContextMenu contextMenu,
        INotificationManager notificationManager,
        IPartyFinderGui partyFinderGui,
        INamePlateGui nameplateGui,
        IAddonLifecycle addonLifecycle,
        IGameGui gameGui,
        ITargetManager targetManager)
    {
        ArgumentNullException.ThrowIfNull(pluginInterface);
        ArgumentNullException.ThrowIfNull(framework);
        ArgumentNullException.ThrowIfNull(log);
        _pluginInterface = pluginInterface;
        _framework = framework;
        _localizer = LocalizationLoader.Build(pluginInterface.UiLanguage, log);
        pluginInterface.LanguageChanged += OnLanguageChanged;

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(pluginInterface);

        var listPath = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "ignorelist.json");
        Store = new IgnoreStore(listPath, _clock, new PluginCoreLog(log));
        Config.FallbackCategoryId ??= Store.EnsureDefaultCategories(_localizer);
        if (Config.FallbackCategoryId != null)
        {
            Config.Save();
        }

        Store.PruneExpired();
        Matcher = new PlayerMatcher(Store, _clock);

        _worldResolver = new WorldResolver(dataManager);
        _blacklistReader = new BlacklistReader(gameGui, _worldResolver, log);
        _blacklistImportWindow = new BlacklistImportWindow(_blacklistReader, Store, Config, notificationManager, _localizer);
        _addConfirmWindow = new AddConfirmWindow(Store, _worldResolver, notificationManager, _localizer, Config);
        _mainWindow = new MainWindow(Store, _worldResolver, Config, _localizer, _addConfirmWindow.BeginBlank, _blacklistImportWindow.BeginImport);
        _blacklistImportWindow.MainWindowRect = () => _mainWindow.GetRect();
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_addConfirmWindow);
        _windowSystem.AddWindow(_blacklistImportWindow);

        _safetyGate = new SafetyGate(condition, partyList, Config);
        _chatFilter = new ChatFilter(chatGui, objectTable, Matcher, Config, partyList, condition, log);
        _partyFinderFilter = new PartyFinderFilter(partyFinderGui, Matcher, Config, log);
        _nameplateFilter = new NameplateFilter(nameplateGui, objectTable, Matcher, _safetyGate, Config, log);
        _characterHideFilter = new CharacterHideFilter(framework, objectTable, Matcher, _safetyGate, Config, log);
#if DEBUG
        _patchHealthCheck = new PatchHealthCheck(framework, objectTable, gameGui, Config, log);
        _commandHandler = new CommandHandler(commandManager, Store, _worldResolver, chatGui, ToggleMain, targetManager, partyList, Config, _localizer,
                                            _addConfirmWindow.BeginDraft,
                                            () => _patchHealthCheck.Run("/xivignore health"));
#else
        _commandHandler = new CommandHandler(commandManager, Store, _worldResolver, chatGui, ToggleMain, targetManager, partyList, Config, _localizer,
                                            _addConfirmWindow.BeginDraft);
#endif
        _contextMenuHandler = new ContextMenuHandler(contextMenu, Store, _worldResolver, notificationManager, objectTable, Config, _localizer, log,
                                                    _addConfirmWindow.BeginDraft);
        _partyAwareness = new PartyAwareness(framework, partyList, Matcher, Store, Config,
                                            notificationManager, chatGui, _worldResolver, _localizer, log);
        _partyListMarker = new PartyListMarker(addonLifecycle, partyList, Matcher, Config, log);
        _socialPartyMarker = new SocialPartyMarker(addonLifecycle, partyList, Matcher, Config, log);
        _friendListMarker = new SocialListMarker(addonLifecycle, "FriendList", Store, Config, log);
        _playerSearchMarker = new SocialListMarker(addonLifecycle, "SocialList", Store, Config, log);

        _updateNotifier = new UpdateNotifier(pluginInterface, chatGui, framework, objectTable, Config, _localizer, log);
        pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += ToggleMain;
        pluginInterface.UiBuilder.OpenConfigUi += OpenSettings;
        framework.Update += OnFrameworkUpdate;

        log.Information("XIVIgnore loaded.");
    }

    private void ToggleMain() => _mainWindow.ToggleAndFocusList();

    // Dalamud gear ("Settings"): open the window + jump straight to the settings tab.
    private void OpenSettings() => _mainWindow.OpenSettings();

    private void OnLanguageChanged(string langCode)
        => _localizer.SetActiveLanguage(LocalizationLoader.MapLanguage(langCode));

    private void OnFrameworkUpdate(IFramework fw)
    {
        var now = _clock.Now;
        if ((now - _lastPrune).TotalSeconds < PruneIntervalSeconds)
        {
            return;
        }

        _lastPrune = now;
        Store.PruneExpired();
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        _pluginInterface.LanguageChanged -= OnLanguageChanged;
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenMainUi -= ToggleMain;
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenSettings;
        _windowSystem.RemoveAllWindows();

#if DEBUG
        _patchHealthCheck.Dispose();
#endif
        _updateNotifier.Dispose();
        _playerSearchMarker.Dispose();
        _friendListMarker.Dispose();
        _socialPartyMarker.Dispose();
        _partyListMarker.Dispose();
        _partyAwareness.Dispose();
        _contextMenuHandler.Dispose();
        _commandHandler.Dispose();
        _characterHideFilter.Dispose();
        _nameplateFilter.Dispose();
        _partyFinderFilter.Dispose();
        _chatFilter.Dispose();
        Matcher.Dispose();
    }
}
