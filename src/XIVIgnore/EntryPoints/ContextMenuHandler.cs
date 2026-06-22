// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.EntryPoints;

// API notes (vs plan):
// - IContextMenu.OnMenuOpened event exists as documented. ✓
// - IMenuOpenedArgs.AddMenuItem(MenuItem item) is the correct method. ✓
// - args.MenuType (not args.MenuType) — actually IMenuArgs.MenuType. ✓
// - args.Target is MenuTarget; cast to MenuTargetDefault for Default menus. ✓
// - target.TargetHomeWorld is RowRef<World>; .RowId gives the uint world id. ✓
// - MenuItem.Name is SeString (implicit conversion from string available). ✓
// - MenuItem.OnClicked is Action<IMenuItemClickedArgs>. ✓
// - MenuItem.IsSubmenu = true shows the submenu arrow (purely visual). ✓
// - IMenuItemClickedArgs.OpenSubmenu(SeString, IReadOnlyList<IMenuItem>) opens a submenu. ✓
//   Verified via Dalamud.xml in %AppData%\XIVLauncher\addon\Hooks\dev\Dalamud.xml.
// - args.AddonName is available on IMenuArgs (confirmed via Dalamud.xml). ✓
//   PF window addon heuristic: contains "LookingForGroup" (logged via debug for in-game verification).
public sealed class ContextMenuHandler : IDisposable
{
    private readonly IContextMenu _contextMenu;
    private readonly IgnoreStore _store;
    private readonly WorldResolver _worlds;
    private readonly INotificationManager _notifications;
    private readonly IObjectTable _objects;
    private readonly Configuration _config;
    private readonly Localizer _loc;
    private readonly IPluginLog _log;
    private readonly Action<DraftSeed> _beginDraft;

    // Preset table: (LabelKey, Amount, Unit). Amount=0 → permanent.
    private static readonly (string LabelKey, int Amount, DurationUnit Unit)[] Presets =
    [
        ("ctx.durPermanent", 0, DurationUnit.Days),
        ("ctx.dur1min",      1, DurationUnit.Minutes),
        ("ctx.dur1hour",     1, DurationUnit.Hours),
        ("ctx.dur1day",      1, DurationUnit.Days),
        ("ctx.dur1week",     1, DurationUnit.Weeks),
        ("ctx.dur1month",    1, DurationUnit.Months),
    ];

    public ContextMenuHandler(IContextMenu contextMenu, IgnoreStore store, WorldResolver worlds,
                              INotificationManager notifications, IObjectTable objects, Configuration config,
                              Localizer loc, IPluginLog log, Action<DraftSeed> beginDraft)
    {
        ArgumentNullException.ThrowIfNull(contextMenu);
        _contextMenu = contextMenu; _store = store; _worlds = worlds;
        _notifications = notifications; _objects = objects; _config = config;
        _loc = loc; _log = log;
        _beginDraft = beginDraft;
        contextMenu.OnMenuOpened += OnMenuOpened;
    }

    /// <summary>
    /// Derives the context-dependent default FilterAction from the addon name.
    /// PF window (contains "LookingForGroup") → Chat | PartyFinder.
    /// ChatLog → Chat.
    /// Otherwise → Chat | PartyFinder (conservative default).
    /// </summary>
    private static FilterAction GetContextActions(string addonName)
    {
        if (addonName.Contains("LookingForGroup", StringComparison.OrdinalIgnoreCase))
        {
            return FilterAction.Chat | FilterAction.PartyFinder;
        }

        if (string.Equals(addonName, "ChatLog", StringComparison.OrdinalIgnoreCase))
        {
            return FilterAction.Chat;
        }

        return FilterAction.Chat | FilterAction.PartyFinder;
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        try
        {
            if (args.MenuType != ContextMenuType.Default)
            {
                return;
            }

            if (args.Target is not MenuTargetDefault target)
            {
                return;
            }

            if (string.IsNullOrEmpty(target.TargetName))
            {
                return;
            }

            // Only ignore real players. If the target is an in-world object that is NOT a player
            // (striking dummy, NPC, monster, companion), we don't show the entry.
            // TargetObject is null in pure name contexts (Party Finder listing, chat name) —
            // those we allow, since they're player contexts anyway (additionally guarded by worldId).
            if (target.TargetObject is { } targetObj && targetObj.ObjectKind != ObjectKind.Pc)
            {
                return;
            }

            var addonName = args.AddonName ?? string.Empty;
#if DEBUG
            _log.Debug($"[XIVIgnore] ContextMenu opened from addon: '{addonName}'");
#endif

            var name = target.TargetName;
            var worldId = target.TargetHomeWorld.RowId;             // RowRef<World>.RowId
            if (worldId == 0)
            {
                return;
            }

            // You can't ignore yourself → hide the entry for your own character (name + home world).
            var self = _objects.LocalPlayer;
            if (self is not null && self.HomeWorld.RowId == worldId &&
                string.Equals(self.Name.TextValue, name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var contextActions = GetContextActions(addonName);

            args.AddMenuItem(new MenuItem
            {
                Name = _loc.Get("ctx.addToList"),                     // implicit string→SeString
                UseDefaultPrefix = true,
                IsSubmenu = true,                                    // shows the arrow
                OnClicked = clicked =>
                {
                    var items = BuildSubmenuItems(name, worldId, contextActions);
                    clicked.OpenSubmenu(items);
                },
            });
        }
        catch (Exception ex)
        {
            _log.Error(ex, "ContextMenu error");
        }
    }

    private List<MenuItem> BuildSubmenuItems(string name, uint worldId, FilterAction contextActions)
    {
        // Note: Dalamud custom submenus support NO back navigation. Picking a duration
        // triggers the action; Escape/clicking elsewhere closes. No "back" entry.
        var items = new List<MenuItem>(Presets.Length);
        foreach (var (labelKey, amount, unit) in Presets)
        {
            items.Add(new MenuItem
            {
                Name = _loc.Get(labelKey),
                OnClicked = _ => OnDurationChosen(name, worldId, amount, unit, contextActions),
            });
        }
        return items;
    }

    private void OnDurationChosen(string name, uint worldId, int amount, DurationUnit unit, FilterAction contextActions)
    {
        var worldName = _worlds.ResolveName(worldId);
        var exists = _store.Entries.Any(e => e.WorldId == worldId &&
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exists) { Notify(_loc.Get("ctx.alreadyOnList", name, worldName), NotificationType.Info); return; }

        // With confirm dialog: open a draft instead of saving directly.
        if (_config.ConfirmBeforeAdd)
        {
            _beginDraft(new DraftSeed
            {
                Name = name,
                WorldId = worldId,
                WorldName = worldName,
                CategoryId = _config.FallbackCategoryId,
                Actions = contextActions,
                ExpiryAmount = amount,
                ExpiryUnit = unit,
            });
            return;
        }

        // Add immediately (legacy behavior).
        _store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = name,
            WorldId = worldId,
            WorldName = worldName,
            CategoryId = _config.FallbackCategoryId,
            ExpiresAt = ExpiryCalculator.Compute(amount, unit, DateTimeOffset.Now),
            ActionsOverride = contextActions,
        });
        Notify(_loc.Get("ctx.added", name, worldName), NotificationType.Success);
    }

    private void Notify(string text, NotificationType type)
        => _notifications.AddNotification(new Notification { Title = "XIVIgnore", Content = text, Type = type });

    public void Dispose() => _contextMenu.OnMenuOpened -= OnMenuOpened;
}
