// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.UI;

public enum EntryFormMode { New, Edit }

// Reusable entry form (name, world, category, note, effect, expiry).
// Edit mode: edit modal in ListTab (shows current expiry + "reset").
// New mode:  confirm dialog (AddConfirmWindow), shows duration directly, pre-filled from a preset.
// Holds the working state; Draw() renders the fields, TryBuild() validates + builds the entry.
public sealed class EntryEditForm
{
    private readonly IgnoreStore _store;
    private readonly WorldResolver _worlds;
    private readonly Localizer _loc;
    private readonly Configuration _config;

    private EntryFormMode _mode;
    private Guid _id;
    private string _name = string.Empty;
    private string _world = string.Empty;
    private string _worldFilter = string.Empty;
    private Guid? _category;
    private string _note = string.Empty;
    private bool _useCategory;
    private bool _chat, _partyFinder, _nameplate, _characterHide;
    private DateTimeOffset? _originalExpiresAt;
    private bool _resetExpiry;
    private bool _permanent;
    private int _expiryAmount = 1;
    private DurationUnit _expiryUnit = DurationUnit.Days;
    private DateTimeOffset _originalCreatedAt;
    private string _worldError = string.Empty;

    public EntryEditForm(IgnoreStore store, WorldResolver worlds, Localizer loc, Configuration config)
    {
        _store = store;
        _worlds = worlds;
        _loc = loc;
        _config = config;
    }

    public bool CanSave => _name.Trim().Length > 0;

    // Fill from an existing entry (edit). Corresponds to the former OpenEditModal.
    public void LoadEdit(IgnoreEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _mode = EntryFormMode.Edit;
        _id = entry.Id;
        _name = entry.Name;
        _world = entry.WorldName;
        _category = entry.CategoryId;
        _note = entry.Note;
        _originalCreatedAt = entry.CreatedAt;
        _originalExpiresAt = entry.ExpiresAt;
        _worldError = string.Empty;
        _worldFilter = string.Empty;

        FilterAction effective;
        if (entry.ActionsOverride is { } ov)
        {
            _useCategory = false;
            effective = FilterActionRules.WithImpliedNameplate(ov);
        }
        else
        {
            _useCategory = true;
            effective = FilterActionRules.WithImpliedNameplate(
                _store.FindCategory(entry.CategoryId)?.DefaultActions ?? FilterAction.None);
        }
        // Character implies Nameplate → show it normalized right at load (no 1-frame flicker).
        _chat = effective.HasFlag(FilterAction.Chat);
        _partyFinder = effective.HasFlag(FilterAction.PartyFinder);
        _nameplate = effective.HasFlag(FilterAction.Nameplate);
        _characterHide = effective.HasFlag(FilterAction.CharacterHide);

        _resetExpiry = false;
        _permanent = entry.ExpiresAt is null;
        _expiryAmount = 1;
        _expiryUnit = DurationUnit.Days;
    }

    // Fill from a draft (new, before confirming).
    public void LoadNew(DraftSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        _mode = EntryFormMode.New;
        _id = Guid.Empty;                       // new entry → fresh Id at build time
        _name = seed.Name;
        _world = seed.WorldName;
        _category = seed.CategoryId;
        _note = seed.Note;
        _originalCreatedAt = default;           // the store sets CreatedAt on insert.
        _originalExpiresAt = null;
        _worldError = string.Empty;
        _worldFilter = string.Empty;

        // Effect explicit from context: so WHAT gets ignored is visible/changeable.
        // The user can deliberately switch to "use category default" in the dialog (same behavior as the edit modal).
        _useCategory = false;
        _chat = seed.Actions.HasFlag(FilterAction.Chat);
        _partyFinder = seed.Actions.HasFlag(FilterAction.PartyFinder);
        _nameplate = seed.Actions.HasFlag(FilterAction.Nameplate);
        _characterHide = seed.Actions.HasFlag(FilterAction.CharacterHide);

        // Expiry directly from the chosen preset.
        _permanent = seed.ExpiryAmount <= 0;
        _expiryAmount = seed.ExpiryAmount <= 0 ? 1 : seed.ExpiryAmount;
        _expiryUnit = seed.ExpiryUnit;
        _resetExpiry = true;                    // new: always recompute fresh
    }

    public void Draw()
    {
        ImGui.TextUnformatted(_loc.Get("common.name"));
        ImGui.SetNextItemWidth(300);
        ImGui.InputText("##editname", ref _name, 64);

        ImGui.TextUnformatted(_loc.Get("common.world"));
        ImGui.SetNextItemWidth(180);
        FormWidgets.DrawWorldCombo(_worlds, "##editworld", ref _world, ref _worldFilter, _loc);
        if (_worldError.Length > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), _worldError);
        }

        ImGui.TextUnformatted(_loc.Get("common.category"));
        FormWidgets.DrawCategoryCombo(_store, "##editcat", ref _category, _loc.Get("list.catNone"));

        ImGui.TextUnformatted(_loc.Get("common.note"));
        ImGui.SetNextItemWidth(460);
        ImGui.InputText("##editnote", ref _note, 256);

        ImGui.Separator();
        ImGui.TextUnformatted(_loc.Get("edit.effectOverride"));
        ImGui.Checkbox(_loc.Get("edit.useCategoryDefault") + "##editUseCategory", ref _useCategory);

        // Character and Nameplate are each only selectable while their global switch in the settings is on.
        var charHideAllowed = _config.CharacterHideFilterEnabled;
        var nameplateAllowed = _config.NameplateFilterEnabled;

        // Character implies Nameplate: while Character is on, Nameplate is forced + locked
        // (the nameplate is tied to the character model).
        if (!_useCategory && _characterHide)
        {
            _nameplate = true;
        }

        var npLocked = !_useCategory && _characterHide;

        if (_useCategory)
        {
            ImGui.BeginDisabled(true);
        }

        ImGui.Checkbox(_loc.Get("common.chat") + "##editChat", ref _chat);
        ImGui.SameLine();
        ImGui.Checkbox(_loc.Get("common.partyFinder") + "##editPF", ref _partyFinder);
        ImGui.SameLine();
        if (npLocked || !nameplateAllowed)
        {
            ImGui.BeginDisabled(true);
        }

        ImGui.Checkbox(_loc.Get("common.nameplate") + "##editNP", ref _nameplate);
        if (npLocked || !nameplateAllowed)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        if (!charHideAllowed)
        {
            ImGui.BeginDisabled(true);
        }

        ImGui.Checkbox(_loc.Get("common.character") + "##editChar", ref _characterHide);
        if (!charHideAllowed)
        {
            ImGui.EndDisabled();
        }

        if (_useCategory)
        {
            ImGui.EndDisabled();
        }

        // Switch off: the box above is disabled — point the user to the setting.
        if (!charHideAllowed)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.82f, 0.25f, 1f));
            ImGui.TextWrapped(_loc.Get("edit.charHideOff"));
            ImGui.PopStyleColor();
        }

        if (!nameplateAllowed)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.82f, 0.25f, 1f));
            ImGui.TextWrapped(_loc.Get("edit.nameplateOff"));
            ImGui.PopStyleColor();
        }

        ImGui.Separator();
        ImGui.TextUnformatted(_loc.Get("edit.expiry"));

        if (_mode == EntryFormMode.Edit)
        {
            var currentExpiry = _originalExpiresAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? _loc.Get("common.permanent");
            ImGui.TextUnformatted(_loc.Get("edit.current", currentExpiry));
            ImGui.Checkbox(_loc.Get("edit.resetExpiry") + "##editResetExpiry", ref _resetExpiry);
        }

        if (_mode == EntryFormMode.New || _resetExpiry)
        {
            ImGui.Checkbox(_loc.Get("common.permanentCap") + "##editPermanent", ref _permanent);
            if (!_permanent)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(70);
                ImGui.InputInt("##editexpamount", ref _expiryAmount);
                if (_expiryAmount < 1)
                {
                    _expiryAmount = 1;   // field only visible when NOT permanent → min. 1
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(110);
                FormWidgets.DrawUnitCombo(_loc, "##editexpunit", ref _expiryUnit);
            }
        }
    }

    // Validates (name, world) and builds the entry. false ⇒ worldError/name insufficient, keep the window open.
    public bool TryBuild(out IgnoreEntry result)
    {
        result = null!;
        var trimmedName = _name.Trim();
        if (trimmedName.Length == 0)
        {
            return false;
        }

        var worldId = _worlds.TryResolveId(_world.Trim());
        if (worldId is null)
        {
            _worldError = _loc.Get("edit.unknownWorld");
            return false;
        }
        _worldError = string.Empty;

        // Determine ActionsOverride
        FilterAction? actionsOverride = null;
        if (!_useCategory)
        {
            var flags = FilterAction.None;
            if (_chat)
            {
                flags |= FilterAction.Chat;
            }

            if (_partyFinder)
            {
                flags |= FilterAction.PartyFinder;
            }

            if (_nameplate)
            {
                flags |= FilterAction.Nameplate;
            }

            if (_characterHide)
            {
                flags |= FilterAction.CharacterHide;
            }

            actionsOverride = FilterActionRules.WithImpliedNameplate(flags);
        }

        // Determine expiry
        DateTimeOffset? expiresAt;
        if (_mode == EntryFormMode.New)
        {
            expiresAt = _permanent ? null : ExpiryCalculator.Compute(_expiryAmount, _expiryUnit, DateTimeOffset.Now);
        }
        else
        {
            expiresAt = _originalExpiresAt;
            if (_resetExpiry)
            {
                expiresAt = _permanent ? null : ExpiryCalculator.Compute(_expiryAmount, _expiryUnit, DateTimeOffset.Now);
            }
        }

        result = new IgnoreEntry
        {
            Name = trimmedName,
            WorldId = worldId.Value,
            WorldName = _worlds.ResolveName(worldId.Value),
            CategoryId = _category,
            Note = _note.Trim(),
            ActionsOverride = actionsOverride,
            ExpiresAt = expiresAt,
        };
        if (_mode == EntryFormMode.Edit)
        {
            result.Id = _id;
            result.CreatedAt = _originalCreatedAt;
        }
        // New: IgnoreEntry.Id = fresh Guid (default), the store sets CreatedAt.
        return true;
    }
}
