// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Collections.ObjectModel;
using System.Text.Json;
using XIVIgnore.Core.Abstractions;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Services;

public sealed class IgnoreStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly IClock _clock;
    private readonly ICoreLog _log;
    private IgnoreListData _data;
    private readonly ReadOnlyCollection<IgnoreEntry> _entriesView;
    private readonly ReadOnlyCollection<IgnoreCategory> _categoriesView;

    /// <summary>Raised after every saved change to entries or categories.</summary>
    public event EventHandler? Changed;

    /// <summary>Loads the list straight from <paramref name="filePath"/> on construction (or empty if the file does not exist).</summary>
    public IgnoreStore(string filePath, IClock clock, ICoreLog log)
    {
        _filePath = filePath;
        _clock = clock;
        _log = log;
        _data = Load();
        _entriesView = _data.Entries.AsReadOnly();
        _categoriesView = _data.Categories.AsReadOnly();
    }

    public IReadOnlyList<IgnoreEntry> Entries => _entriesView;
    public IReadOnlyList<IgnoreCategory> Categories => _categoriesView;

    /// <summary>Adds an entry, or replaces an existing one with the same Id; fills in a missing CreatedAt.</summary>
    public void AddOrUpdateEntry(IgnoreEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.CreatedAt == default)
        {
            entry.CreatedAt = _clock.Now;
        }

        var idx = _data.Entries.FindIndex(e => e.Id == entry.Id);
        if (idx >= 0)
        {
            _data.Entries[idx] = entry;
        }
        else
        {
            _data.Entries.Add(entry);
        }

        SaveAndNotify();
    }

    public void RemoveEntry(Guid id)
    {
        if (_data.Entries.RemoveAll(e => e.Id == id) > 0)
        {
            SaveAndNotify();
        }
    }

    /// <summary>Removes all entries whose ExpiresAt is ≤ now. Returns the removed ones.</summary>
    public IReadOnlyList<IgnoreEntry> PruneExpired()
    {
        var now = _clock.Now;
        var expired = _data.Entries.Where(e => e.ExpiresAt is { } exp && exp <= now).ToList();
        if (expired.Count == 0)
        {
            return Array.Empty<IgnoreEntry>();
        }

        foreach (var e in expired)
        {
            _data.Entries.Remove(e);
            _log.Info($"Expired entry removed: {e.Name}@{e.WorldName} (ExpiresAt {e.ExpiresAt:u}).");
        }
        SaveAndNotify();
        return expired;
    }

    /// <summary>Adds a category, or replaces an existing one with the same Id.</summary>
    public void AddOrUpdateCategory(IgnoreCategory category)
    {
        var idx = _data.Categories.FindIndex(c => c.Id == category.Id);
        if (idx >= 0)
        {
            _data.Categories[idx] = category;
        }
        else
        {
            _data.Categories.Add(category);
        }

        SaveAndNotify();
    }

    /// <summary>Deletes the category and detaches every entry that was assigned to it (CategoryId → null).</summary>
    public void RemoveCategory(Guid id)
    {
        if (_data.Categories.RemoveAll(c => c.Id == id) > 0)
        {
            foreach (var e in _data.Entries.Where(e => e.CategoryId == id).ToList())
            {
                e.CategoryId = null;
            }

            SaveAndNotify();
        }
    }

    /// <summary>
    /// Removes the Character effect from every entry override (Nameplate stays). Category defaults are
    /// left untouched, they simply have no effect while the global switch is off. Called when it is turned off.
    /// </summary>
    public void StripCharacterHide()
    {
        var changed = false;

        foreach (var e in _data.Entries)
        {
            if (e.ActionsOverride is { } ov && ov.HasFlag(FilterAction.CharacterHide))
            {
                e.ActionsOverride = ov & ~FilterAction.CharacterHide;
                changed = true;
            }
        }

        if (changed)
        {
            SaveAndNotify();
        }
    }

    public IgnoreCategory? FindCategory(Guid? id)
        => id is null ? null : _data.Categories.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Creates the default categories if none exist yet (localized).
    /// Returns the Id of the fallback category ("misc", the last entry), on first
    /// seeding the freshly created one; for existing data the one matched by legacy name; otherwise null.
    /// </summary>
    public Guid? EnsureDefaultCategories(Localizer loc)
    {
        if (_data.Categories.Count == 0)
        {
            var defaults = DefaultCategories.Create(loc);
            _data.Categories.AddRange(defaults);
            SaveAndNotify();
            return defaults[^1].Id; // misc = last
        }

        // Existing data: find the fallback by legacy name (no renaming!).
        string[] legacy = { "Sonstiges", "Miscellaneous", "Misc", "Other" };
        var misc = _data.Categories.FirstOrDefault(c => legacy.Contains(c.Name, StringComparer.OrdinalIgnoreCase));
        return misc?.Id;
    }

    private void SaveAndNotify()
    {
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private IgnoreListData Load()
    {
        if (!File.Exists(_filePath))
        {
            return new IgnoreListData();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<IgnoreListData>(json, JsonOptions) ?? new IgnoreListData();
        }
        catch (Exception ex)
        {
            _log.Error($"ignorelist.json is unreadable, starting empty and backing up .bak: {ex.Message}", ex);
            try { File.Copy(_filePath, _filePath + ".bak", overwrite: true); }
            catch (Exception copyEx) { _log.Warning($"Could not write .bak: {copyEx.Message}"); }
            return new IgnoreListData();
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tmp = _filePath + ".tmp";
            try
            {
                File.WriteAllText(tmp, JsonSerializer.Serialize(_data, JsonOptions));
                File.Move(tmp, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Saving ignorelist.json failed: {ex.Message}", ex);
        }
    }
}
