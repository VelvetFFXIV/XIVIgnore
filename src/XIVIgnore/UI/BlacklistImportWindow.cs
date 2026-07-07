// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using XIVIgnore.Awareness;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;

namespace XIVIgnore.UI;

// Import preview: polls whether the blacklist is open (otherwise a hint), reads the candidates,
// shows them with checkboxes (duplicates grayed out) + category dropdown, imports the selection.
public sealed class BlacklistImportWindow : Window
{
    private sealed class Candidate
    {
        public uint WorldId;
        public string WorldName = string.Empty;
        public string Name = string.Empty;
        public string Note = string.Empty;
        public bool IsDuplicate;
        public bool Checked;
    }

    private readonly BlacklistReader _reader;
    private readonly IgnoreStore _store;
    private readonly Configuration _config;
    private readonly INotificationManager _notifications;
    private readonly Localizer _loc;

    private readonly List<Candidate> _candidates = new();
    private Guid? _selectedCategory;
    private bool _hasRead;

    /// <summary>Returns the position and size of the main window (wired up externally).</summary>
    public Func<(Vector2 Pos, Vector2 Size)>? MainWindowRect;
    // Set on open; triggers the one-time positioning next to the main window.
    private bool _positionPending;

    public BlacklistImportWindow(BlacklistReader reader, IgnoreStore store, Configuration config,
                                 INotificationManager notifications, Localizer loc)
        : base("XIVIgnore###XIVIgnoreImport", ImGuiWindowFlags.AlwaysAutoResize)
    {
        _reader = reader;
        _store = store;
        _config = config;
        _notifications = notifications;
        _loc = loc;
        IsOpen = false;
        // Minimum width so the hint text (TextWrapped) doesn't collapse to one character
        // per line under AlwaysAutoResize.
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 0),
            MaximumSize = new Vector2(2000, 2000),
        };
    }

    /// <summary>Opens the import preview next to the main window and reads fresh on the next frame.</summary>
    public void BeginImport()
    {
        _candidates.Clear();
        _hasRead = false;
        _positionPending = true;
        _selectedCategory = _config.FallbackCategoryId;
        // WindowName is read-only in this Dalamud version; the title is driven by the stable suffix ###XIVIgnoreImport.
        IsOpen = true;
    }

    public override void PreDraw()
    {
        if (!_positionPending)
        {
            return;
        }

        _positionPending = false;

        var vp = ImGui.GetMainViewport();
        var rect = MainWindowRect?.Invoke();
        if (rect is not { } r || r.Size.X <= 1f)
        {
            // Fallback: center if the main window position is (still) unknown.
            ImGui.SetNextWindowPos(vp.GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            return;
        }

        // To the right of the main window if there's room; otherwise to its left.
        const float gap = 12f, estWidth = 460f;
        float rightX = r.Pos.X + r.Size.X + gap;
        float vpRight = vp.WorkPos.X + vp.WorkSize.X;
        float x = (rightX + estWidth <= vpRight) ? rightX : Math.Max(vp.WorkPos.X, r.Pos.X - estWidth - gap);
        ImGui.SetNextWindowPos(new Vector2(x, r.Pos.Y), ImGuiCond.Always);
    }

    public override void Draw()
    {
        // Blacklist not open → hint, keep polling (Draw runs every frame).
        if (!_reader.IsBlacklistOpen())
        {
            // On close, clear the (stale) candidates once.
            if (_hasRead) { _candidates.Clear(); _hasRead = false; }
            ImGui.TextWrapped(_loc.Get("import.openHint"));
            return;
        }

        if (!_hasRead)
        {
            Refresh();
        }

        // Important: players who stay on the game blacklist are blocked by the game itself,
        // XIVIgnore then can't fully capture them. Remove them from the game blacklist after import.
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.82f, 0.25f, 1f));
        ImGui.TextWrapped(_loc.Get("import.trackNotice"));
        ImGui.PopStyleColor();
        ImGui.Separator();

        FormWidgets.DrawCategoryCombo(_store, "##importcat", ref _selectedCategory, _loc.Get("list.catNone"));
        ImGui.SameLine();
        if (ImGui.Button(_loc.Get("import.refresh")))
        {
            Refresh();
        }

        int dupes = _candidates.Count(c => c.IsDuplicate);
        ImGui.TextUnformatted(_loc.Get("import.summary", _candidates.Count, dupes));
        ImGui.TextDisabled(_loc.Get("import.scrollHint"));

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY;
        if (ImGui.BeginTable("##importtable", 4, flags, new Vector2(560, 260)))
        {
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableSetupColumn(_loc.Get("common.name"));
            ImGui.TableSetupColumn(_loc.Get("common.world"));
            ImGui.TableSetupColumn(_loc.Get("common.note"));
            ImGui.TableHeadersRow();

            for (int i = 0; i < _candidates.Count; i++)
            {
                var c = _candidates[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (c.IsDuplicate)
                {
                    ImGui.BeginDisabled(true);
                }
                // Candidate is a class → ref writes back directly into the heap object's field.
                ImGui.Checkbox($"##chk{i}", ref c.Checked);
                if (c.IsDuplicate)
                {
                    ImGui.EndDisabled();
                }

                ImGui.TableNextColumn(); ImGui.TextUnformatted(c.Name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(c.IsDuplicate ? $"{c.WorldName} ({_loc.Get("import.alreadyOnList")})" : c.WorldName);
                ImGui.TableNextColumn();
                if (c.Note.Length > 0)
                {
                    ImGui.TextUnformatted(c.Note);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(c.Note);   // full note in case the column cuts it off
                    }
                }
            }
            ImGui.EndTable();
        }

        int selectable = _candidates.Count(c => c.Checked && !c.IsDuplicate);
        if (selectable == 0)
        {
            ImGui.BeginDisabled(true);
        }

        if (ImGui.Button(_loc.Get("import.importSelected")))
        {
            DoImport();
        }

        if (selectable == 0)
        {
            ImGui.EndDisabled();
        }
    }

    private void Refresh()
    {
        _candidates.Clear();
        var seen = new HashSet<(uint, string)>();
        foreach (var (worldId, worldName, name, note) in _reader.ReadCandidates())
        {
            // Don't take duplicate rows more than once (e.g. a render artifact while scrolling).
            if (!seen.Add((worldId, name.ToLowerInvariant())))
            {
                continue;
            }

            bool dup = _store.Entries.Any(e => e.WorldId == worldId &&
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            _candidates.Add(new Candidate
            {
                WorldId = worldId,
                WorldName = worldName,
                Name = name,
                Note = note,
                IsDuplicate = dup,
                Checked = !dup,
            });
        }
        // Only latch in once rows were actually read. Right after the blacklist opens the addon
        // is visible, but the rows may not be built yet (0 hits) → then read again next frame
        // until the entries are there (otherwise the list stays empty).
        _hasRead = _candidates.Count > 0;
    }

    private void DoImport()
    {
        int n = 0;
        foreach (var c in _candidates)
        {
            if (!c.Checked || c.IsDuplicate)
            {
                continue;
            }

            _store.AddOrUpdateEntry(new IgnoreEntry
            {
                Name = c.Name,
                WorldId = c.WorldId,
                WorldName = c.WorldName,
                Note = c.Note,   // carry over the game note
                CategoryId = _selectedCategory ?? _config.FallbackCategoryId,
                ExpiresAt = null,   // permanent; no ActionsOverride → inherits the category effect
            });
            n++;
        }
        _notifications.AddNotification(new Notification
        {
            Title = "XIVIgnore",
            Content = _loc.Get("import.done", n),
            Type = NotificationType.Success,
        });
        IsOpen = false;
    }
}
