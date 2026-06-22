// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;
using XIVIgnore.UI.Tabs;

namespace XIVIgnore.UI;

public sealed class MainWindow : Window
{
    private readonly ListTab _listTab;
    private readonly CategoryTab _categoryTab;
    private readonly SettingsTab _settingsTab;
    private readonly Localizer _loc;
    private bool _jumpToList;
    private bool _jumpToSettings;
    private Vector2 _lastPos;
    private Vector2 _lastSize;

    /// <summary>Returns the last rendered position and size of the main window.</summary>
    public (Vector2 Pos, Vector2 Size) GetRect() => (_lastPos, _lastSize);

    public MainWindow(IgnoreStore store, WorldResolver worlds, Configuration config, Localizer loc,
                      Action openAddDialog, Action openBlacklistImport)
        : base("XIVIgnore###XIVIgnoreMain")
    {
        _loc = loc;
        // Default size on first open deliberately wide AND tall (7 columns + several rows without
        // immediate scrolling). After that ImGui remembers the size the user chose.
        Size = new Vector2(920, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            // Lower bound so the table never gets too narrow (buttons/timestamps would otherwise clip).
            MinimumSize = new(880, 420),
            MaximumSize = new(2000, 2000),
        };
        _listTab = new ListTab(store, worlds, config, loc, openAddDialog, openBlacklistImport);
        _categoryTab = new CategoryTab(store, loc, config);
        _settingsTab = new SettingsTab(config, loc);
    }

    /// <summary>Opens/closes the window (Dalamud "Open" + /xivignore with no argument); always the list tab on open.</summary>
    public void ToggleAndFocusList()
    {
        bool wasOpen = IsOpen;
        Toggle();
        if (!wasOpen) { _jumpToList = true; _jumpToSettings = false; }
    }

    /// <summary>Opens the window and jumps straight to the settings tab (Dalamud gear "Settings").</summary>
    public void OpenSettings()
    {
        IsOpen = true;
        _jumpToSettings = true;
        _jumpToList = false;
    }

    public override void Draw()
    {
        // Capture the window rect at the start of each frame so the import window can be positioned next to it.
        _lastPos = ImGui.GetWindowPos();
        _lastSize = ImGui.GetWindowSize();

        if (!ImGui.BeginTabBar("##xivignore-tabs"))
        {
            return;
        }

        // "Open" forces the list tab once, the gear forces the settings tab.
        var listFlags = _jumpToList ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        _jumpToList = false;
        if (ImGui.BeginTabItem(_loc.Get("tab.list"), listFlags)) { _listTab.Draw(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem(_loc.Get("tab.categories"))) { _categoryTab.Draw(); ImGui.EndTabItem(); }

        var settingsFlags = _jumpToSettings ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        _jumpToSettings = false;
        if (ImGui.BeginTabItem(_loc.Get("tab.settings"), settingsFlags)) { _settingsTab.Draw(); ImGui.EndTabItem(); }

        ImGui.EndTabBar();
    }
}
