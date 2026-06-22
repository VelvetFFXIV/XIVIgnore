// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Numerics;
using Dalamud.Bindings.ImGui;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;

namespace XIVIgnore.UI.Tabs;

public sealed class CategoryTab
{
    private readonly IgnoreStore _store;
    private readonly Localizer _loc;
    private readonly Configuration _config;
    private string _newName = string.Empty;

    public CategoryTab(IgnoreStore store, Localizer loc, Configuration config)
    {
        _store = store;
        _loc = loc;
        _config = config;
    }

    public void Draw()
    {
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##newcat", _loc.Get("cat.newHint"), ref _newName, 48);
        ImGui.SameLine();
        if (ImGui.Button(_loc.Get("cat.create")) && _newName.Trim().Length > 0)
        {
            _store.AddOrUpdateCategory(new IgnoreCategory { Name = _newName.Trim() });
            _newName = string.Empty;
        }
        ImGui.Separator();

        // Character is only selectable while the global switch is on; one hint for the whole list.
        if (!_config.CharacterHideFilterEnabled)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.82f, 0.25f, 1f));
            ImGui.TextWrapped(_loc.Get("edit.charHideOff"));
            ImGui.PopStyleColor();
        }

        // Same for Nameplate.
        if (!_config.NameplateFilterEnabled)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.82f, 0.25f, 1f));
            ImGui.TextWrapped(_loc.Get("edit.nameplateOff"));
            ImGui.PopStyleColor();
        }

        Guid? toRemove = null;
        foreach (var c in _store.Categories.ToList())
        {
            ImGui.PushID(c.Id.ToString());

            var name = c.Name;
            ImGui.SetNextItemWidth(180);
            if (ImGui.InputText(_loc.Get("common.name"), ref name, 48) && name.Trim().Length > 0)
            {
                c.Name = name.Trim();
                _store.AddOrUpdateCategory(c);
            }

            ImGui.TextUnformatted(_loc.Get("cat.defaultEffect"));
            DrawActionFlag(_loc.Get("common.chat"), c, FilterAction.Chat);
            ImGui.SameLine(); DrawActionFlag(_loc.Get("common.partyFinder"), c, FilterAction.PartyFinder);
            ImGui.SameLine(); DrawNameplateFlag(_loc.Get("common.nameplate"), c);
            ImGui.SameLine(); DrawCharacterFlag(_loc.Get("common.character"), c);

            ImGui.SameLine();
            if (ImGui.SmallButton(_loc.Get("common.delete")))
            {
                toRemove = c.Id;
            }

            ImGui.Separator();
            ImGui.PopID();
        }
        if (toRemove is { } id)
        {
            _store.RemoveCategory(id);
        }
    }

    private void DrawActionFlag(string label, IgnoreCategory c, FilterAction flag)
    {
        var on = c.DefaultActions.HasFlag(flag);
        if (ImGui.Checkbox(label, ref on))
        {
            c.DefaultActions = on ? c.DefaultActions | flag : c.DefaultActions & ~flag;
            _store.AddOrUpdateCategory(c);
        }
    }

    // Nameplate is locked + forced as long as Character (CharacterHide) is set, and disabled
    // while the global Nameplate switch is off.
    private void DrawNameplateFlag(string label, IgnoreCategory c)
    {
        var charOn = c.DefaultActions.HasFlag(FilterAction.CharacterHide);
        var locked = charOn || !_config.NameplateFilterEnabled;
        var on = charOn || c.DefaultActions.HasFlag(FilterAction.Nameplate);
        if (locked)
        {
            ImGui.BeginDisabled(true);
        }

        if (ImGui.Checkbox(label, ref on))
        {
            c.DefaultActions = on ? c.DefaultActions | FilterAction.Nameplate
                                  : c.DefaultActions & ~FilterAction.Nameplate;
            _store.AddOrUpdateCategory(c);
        }
        if (locked)
        {
            ImGui.EndDisabled();
        }
    }

    // Character implies Nameplate: set Nameplate along with it when enabling.
    // Disabled while the global switch is off.
    private void DrawCharacterFlag(string label, IgnoreCategory c)
    {
        var allowed = _config.CharacterHideFilterEnabled;
        var on = c.DefaultActions.HasFlag(FilterAction.CharacterHide);
        if (!allowed)
        {
            ImGui.BeginDisabled(true);
        }

        if (ImGui.Checkbox(label, ref on))
        {
            c.DefaultActions = on
                ? FilterActionRules.WithImpliedNameplate(c.DefaultActions | FilterAction.CharacterHide)
                : c.DefaultActions & ~FilterAction.CharacterHide;
            _store.AddOrUpdateCategory(c);
        }
        if (!allowed)
        {
            ImGui.EndDisabled();
        }
    }
}
