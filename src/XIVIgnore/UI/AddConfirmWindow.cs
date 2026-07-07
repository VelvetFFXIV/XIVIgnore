// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.UI;

// Confirm/edit window before adding. Closed by default; BeginDraft opens it
// pre-filled over the game (the main window need not be open). Confirm/Enter saves,
// Cancel/Escape/X discards.
public sealed class AddConfirmWindow : Window
{
    private readonly IgnoreStore _store;
    private readonly INotificationManager _notifications;
    private readonly Localizer _loc;
    private readonly Configuration _config;
    private readonly EntryEditForm _form;
    private bool _justOpened;

    public AddConfirmWindow(IgnoreStore store, WorldResolver worlds,
                            INotificationManager notifications, Localizer loc, Configuration config)
        : base("XIVIgnore###XIVIgnoreAddConfirm",
               ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        _store = store;
        _notifications = notifications;
        _loc = loc;
        _config = config;
        _form = new EntryEditForm(store, worlds, loc, config);
        RespectCloseHotkey = true;   // Escape closes = cancel
        IsOpen = false;
    }

    /// <summary>Opens the dialog with the given pre-fill (trigger: context menu/command).</summary>
    public void BeginDraft(DraftSeed seed)
    {
        _form.LoadNew(seed);
        _justOpened = true;
        IsOpen = true;
    }

    /// <summary>
    /// Opens the dialog empty (main "Add"): fallback category + its default effect
    /// pre-filled, name/world empty, permanent. The user fills it in and confirms, so the
    /// effect is chosen deliberately too (instead of just the category like in the old inline row).
    /// </summary>
    public void BeginBlank()
    {
        var catId = _config.FallbackCategoryId;
        var actions = catId is { } id ? _store.FindCategory(id)?.DefaultActions ?? FilterAction.None : FilterAction.None;
        BeginDraft(new DraftSeed { CategoryId = catId, Actions = actions });
    }

    public override void PreDraw()
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(520, 0), ImGuiCond.Appearing);
    }

    public override void Draw()
    {
        // On open, bring to front + focus (even when a new draft arrives while the window is already
        // open) → Enter confirms immediately. SetWindowFocus(WindowName) targets this window.
        if (_justOpened) { ImGui.SetWindowFocus(WindowName); _justOpened = false; }

        ImGui.TextUnformatted(_loc.Get("addconfirm.title"));
        ImGui.Separator();

        _form.Draw();

        ImGui.Separator();
        bool canSave = _form.CanSave;
        bool enterPressed = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) &&
                            (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter));

        if (!canSave)
        {
            ImGui.BeginDisabled(true);
        }

        bool confirm = ImGui.Button(_loc.Get("common.add"));
        if (!canSave)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        if (ImGui.Button(_loc.Get("common.cancel")))
        {
            IsOpen = false;
            return;
        }

        if ((confirm || (canSave && enterPressed)) && _form.TryBuild(out var entry))
        {
            _store.AddOrUpdateEntry(entry);
            _notifications.AddNotification(new Notification
            {
                Title = "XIVIgnore",
                Content = _loc.Get("ctx.added", entry.Name, entry.WorldName),
                Type = NotificationType.Success,
            });
            IsOpen = false;
        }
    }
}
