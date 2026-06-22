// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Bindings.ImGui;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.UI;

// Shared small ImGui widgets for entry forms (add form in ListTab,
// EntryEditForm in the edit modal and in the confirm dialog).
public static class FormWidgets
{
    public static string UnitKey(DurationUnit u) => u switch
    {
        DurationUnit.Minutes => "unit.minutes",
        DurationUnit.Hours => "unit.hours",
        DurationUnit.Days => "unit.days",
        DurationUnit.Weeks => "unit.weeks",
        DurationUnit.Months => "unit.months",
        _ => "unit.days",
    };

    public static void DrawUnitCombo(Localizer loc, string label, ref DurationUnit unit)
    {
        ArgumentNullException.ThrowIfNull(loc);
        var currentLabel = loc.Get(UnitKey(unit));
        if (!ImGui.BeginCombo(label, currentLabel))
        {
            return;
        }

        foreach (var u in new[] { DurationUnit.Minutes, DurationUnit.Hours, DurationUnit.Days, DurationUnit.Weeks, DurationUnit.Months })
        {
            var lbl = loc.Get(UnitKey(u));
            if (ImGui.Selectable(lbl, unit == u))
            {
                unit = u;
            }
        }
        ImGui.EndCombo();
    }

    public static void DrawCategoryCombo(IgnoreStore store, string label, ref Guid? selected, string noneLabel)
    {
        ArgumentNullException.ThrowIfNull(store);
        var current = selected is null ? noneLabel : (store.FindCategory(selected)?.Name ?? noneLabel);
        ImGui.SetNextItemWidth(150);
        if (!ImGui.BeginCombo(label, current))
        {
            return;
        }

        if (ImGui.Selectable(noneLabel, selected is null))
        {
            selected = null;
        }

        foreach (var c in store.Categories)
        {
            if (ImGui.Selectable($"{c.Name}##{c.Id}", selected == c.Id))
            {
                selected = c.Id;
            }
        }

        ImGui.EndCombo();
    }

    // Searchable world dropdown. selectedName = chosen world name ("" = nothing selected).
    // filter = persistent search text of this combo (held by the caller).
    public static void DrawWorldCombo(WorldResolver worlds, string id, ref string selectedName,
                                      ref string filter, Localizer loc)
    {
        ArgumentNullException.ThrowIfNull(worlds);
        ArgumentNullException.ThrowIfNull(loc);
        var current = string.IsNullOrEmpty(selectedName) ? loc.Get("common.worldSelect") : selectedName;
        if (!ImGui.BeginCombo(id, current))
        {
            return;
        }

        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere();   // focus the search field on open
        }

        ImGui.InputTextWithHint(id + "##filter", loc.Get("common.search"), ref filter, 32);
        foreach (var (_, name) in worlds.PublicWorlds())
        {
            if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ImGui.Selectable(name, name == selectedName))
            {
                selectedName = name;
                filter = string.Empty;   // reset filter after selection
            }
        }
        ImGui.EndCombo();
    }
}
