// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.UI.Tabs;

public sealed class ListTab
{
    private readonly IgnoreStore _store;
    private readonly Localizer _loc;
    private readonly Action _openAddDialog;
    private readonly Action _openBlacklistImport;

    private string _search = string.Empty;
    private Guid? _filterCategory;

    // ── Edit modal ────────────────────────────────────────────────────────────
    // editId != null → modal open / should open. Field state lives in EntryEditForm.
    private Guid? _editId;
    private readonly EntryEditForm _editForm;

    private string EditPopupId => _loc.Get("edit.title") + "###editpopup";

    public ListTab(IgnoreStore store, WorldResolver worlds, Configuration config, Localizer loc,
                   Action openAddDialog, Action openBlacklistImport)
    {
        _store = store;
        _loc = loc;
        _openAddDialog = openAddDialog;
        _openBlacklistImport = openBlacklistImport;
        _editForm = new EntryEditForm(store, worlds, loc, config);
    }

    public void Draw()
    {
        DrawAddForm();
        ImGui.Separator();
        DrawFilters();
        DrawTable();
        DrawEditPopup();
    }

    private void DrawAddForm()
    {
        // Full dialog instead of a cramped inline row: just the entry buttons here.
        // The dialog (AddConfirmWindow) offers name, world, category, effect, expiry and note.
        if (ImGui.Button(_loc.Get("common.add")))
        {
            _openAddDialog();
        }

        ImGui.SameLine();
        if (ImGui.Button(_loc.Get("import.button")))
        {
            _openBlacklistImport();
        }
    }

    private void DrawFilters()
    {
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##search", _loc.Get("list.hintSearch"), ref _search, 64);
        ImGui.SameLine();
        FormWidgets.DrawCategoryCombo(_store, _loc.Get("list.categoryFilter") + "##filter", ref _filterCategory, _loc.Get("list.catAll"));
    }

    private void DrawTable()
    {
        // NO Resizable: in this ImGui binding NoSavedSettings didn't take — the column widths were
        // saved anyway and overrode the code widths. Without Resizable there's nothing to save →
        // the fixed action/timestamp widths always apply, the rest stretches with the window.
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders
                  | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings;
        // 7 columns: Name, World, Category, Effect, Note, Expires, Action
        // ID v2: forces a fresh column state (old saved widths under the old ID are ignored).
        // Table fills the remaining window height (taller window → more rows), own scrollbar.
        var tableHeight = ImGui.GetContentRegionAvail().Y;
        if (!ImGui.BeginTable("##entries2", 7, flags, new Vector2(0, tableHeight)))
        {
            return;
        }

        // Fixed widths from the real text widths + a generous buffer (frame/cell padding + spacing
        // between the two buttons). Deliberately NOT dependent on ImGui.GetStyle() values — those can
        // be 0 in the plugin context; then the padding was missing and "Remove" got cut off.
        float actionWidth = ImGui.CalcTextSize(_loc.Get("common.edit")).X
                          + ImGui.CalcTextSize(_loc.Get("common.remove")).X + 64f;
        float expiresWidth = ImGui.CalcTextSize("2026-12-31 23:59:59").X + 24f;

        ImGui.TableSetupColumn(_loc.Get("list.colName"));
        ImGui.TableSetupColumn(_loc.Get("list.colWorld"));
        ImGui.TableSetupColumn(_loc.Get("list.colCategory"));
        ImGui.TableSetupColumn(_loc.Get("list.colEffect"));
        // Note gets double stretch weight (more room) and is wrapped below instead of cut off.
        ImGui.TableSetupColumn(_loc.Get("list.colNote"), ImGuiTableColumnFlags.WidthStretch, 2f);
        ImGui.TableSetupColumn(_loc.Get("list.colExpires"), ImGuiTableColumnFlags.WidthFixed, expiresWidth);
        ImGui.TableSetupColumn(_loc.Get("list.colAction"), ImGuiTableColumnFlags.WidthFixed, actionWidth);
        ImGui.TableHeadersRow();

        var rows = _store.Entries.AsEnumerable();
        var s = _search.Trim();
        if (s.Length > 0)
        {
            rows = rows.Where(e => e.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (_filterCategory is { } fc)
        {
            rows = rows.Where(e => e.CategoryId == fc);
        }

        Guid? toRemove = null;
        Guid? toEdit = null;
        foreach (var e in rows.ToList())
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted(e.Name);
            ImGui.TableNextColumn(); ImGui.TextUnformatted(e.WorldName);
            ImGui.TableNextColumn(); ImGui.TextUnformatted(_store.FindCategory(e.CategoryId)?.Name ?? "—");
            ImGui.TableNextColumn(); ImGui.TextUnformatted(FormatEffectiveActions(e));
            ImGui.TableNextColumn();
            ImGui.TextWrapped(e.Note);   // multi-line instead of cut off; the row grows with it
            ImGui.TableNextColumn(); ImGui.TextUnformatted(e.ExpiresAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? _loc.Get("common.permanent"));
            ImGui.TableNextColumn();
            if (ImGui.SmallButton(_loc.Get("common.edit") + $"##{e.Id}"))
            {
                toEdit = e.Id;
            }

            ImGui.SameLine();
            if (ImGui.SmallButton(_loc.Get("common.remove") + $"##{e.Id}"))
            {
                toRemove = e.Id;
            }
        }
        ImGui.EndTable();

        if (toRemove is { } removeId)
        {
            _store.RemoveEntry(removeId);
        }

        if (toEdit is { } editEntryId)
        {
            OpenEditModal(editEntryId);
        }
    }

    // ── Effect column ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the entry's effective FilterAction:
    /// ActionsOverride ?? category default ?? None.
    /// </summary>
    private FilterAction EffectiveActions(IgnoreEntry e)
        => FilterActionRules.WithImpliedNameplate(
            e.ActionsOverride
            ?? _store.FindCategory(e.CategoryId)?.DefaultActions
            ?? FilterAction.None);

    private string FormatEffectiveActions(IgnoreEntry e)
    {
        var a = EffectiveActions(e);
        if (a == FilterAction.None)
        {
            return "—";
        }

        var parts = new List<string>(4);
        if (a.HasFlag(FilterAction.Chat))
        {
            parts.Add("Chat");
        }

        if (a.HasFlag(FilterAction.PartyFinder))
        {
            parts.Add("PF");
        }

        if (a.HasFlag(FilterAction.Nameplate))
        {
            parts.Add("NP");
        }

        if (a.HasFlag(FilterAction.CharacterHide))
        {
            parts.Add("Char");
        }

        return string.Join(" · ", parts);
    }

    // ── Edit modal ────────────────────────────────────────────────────────────

    private void OpenEditModal(Guid id)
    {
        var entry = _store.Entries.FirstOrDefault(e => e.Id == id);
        if (entry is null)
        {
            return;
        }

        _editId = id;
        _editForm.LoadEdit(entry);
        ImGui.OpenPopup(EditPopupId);
    }

    private void DrawEditPopup()
    {
        var viewport = ImGui.GetMainViewport();
        var center = viewport.GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(520, 0), ImGuiCond.Appearing);

        if (!ImGui.BeginPopupModal(EditPopupId, ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        // If the entry has since been deleted, close the modal.
        if (_editId is null || _store.Entries.All(e => e.Id != _editId))
        {
            ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
            return;
        }

        _editForm.Draw();

        ImGui.Separator();
        bool canSave = _editForm.CanSave;
        if (!canSave)
        {
            ImGui.BeginDisabled(true);
        }

        if (ImGui.Button(_loc.Get("common.save")))
        {
            if (_editForm.TryBuild(out var entry))
            {
                _store.AddOrUpdateEntry(entry);
                _editId = null;
                ImGui.CloseCurrentPopup();
            }
        }
        if (!canSave)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        if (ImGui.Button(_loc.Get("common.cancel")))
        {
            _editId = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }
}
