// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Core.Tests.Fakes;

namespace XIVIgnore.Core.Tests;

public class IgnoreStoreTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"xivignore-test-{Guid.NewGuid():N}.json");
    private readonly TestClock clock = new();
    private readonly NullCoreLog log = new();

    private IgnoreStore NewStore() => new(path, clock, log);

    [Fact]
    public void New_store_without_file_is_empty()
    {
        var store = NewStore();
        Assert.Empty(store.Entries);
        Assert.Empty(store.Categories);
    }

    [Fact]
    public void AddEntry_persists_and_is_reloaded()
    {
        var store = NewStore();
        var entry = new IgnoreEntry { Name = "Foo Bar", WorldId = 73, WorldName = "Phoenix" };
        store.AddOrUpdateEntry(entry);

        var reloaded = NewStore();
        var e = Assert.Single(reloaded.Entries);
        Assert.Equal("Foo Bar", e.Name);
        Assert.Equal(73u, e.WorldId);
    }

    [Fact]
    public void AddOrUpdateEntry_updates_on_same_id()
    {
        var store = NewStore();
        var entry = new IgnoreEntry { Name = "Foo Bar", WorldId = 73 };
        store.AddOrUpdateEntry(entry);
        entry.Note = "changed";
        store.AddOrUpdateEntry(entry);

        Assert.Single(store.Entries);
        Assert.Equal("changed", store.Entries[0].Note);
    }

    [Fact]
    public void StripCharacterHide_clears_entry_overrides_only_and_leaves_categories()
    {
        var store = NewStore();
        store.AddOrUpdateCategory(new IgnoreCategory
        {
            Name = "Cat",
            DefaultActions = FilterAction.Nameplate | FilterAction.CharacterHide,
        });
        store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = "Foo Bar",
            WorldId = 73,
            ActionsOverride = FilterAction.Chat | FilterAction.Nameplate | FilterAction.CharacterHide,
        });

        store.StripCharacterHide();

        var reloaded = NewStore();
        var ov = reloaded.Entries[0].ActionsOverride!.Value;
        Assert.False(ov.HasFlag(FilterAction.CharacterHide)); // entry override loses it
        Assert.True(ov.HasFlag(FilterAction.Nameplate));      // other effects stay
        Assert.True(reloaded.Categories[0].DefaultActions.HasFlag(FilterAction.CharacterHide)); // category unchanged
    }

    [Fact]
    public void RemoveEntry_removes_and_persists()
    {
        var store = NewStore();
        var entry = new IgnoreEntry { Name = "Foo Bar", WorldId = 73 };
        store.AddOrUpdateEntry(entry);
        store.RemoveEntry(entry.Id);

        Assert.Empty(store.Entries);
        Assert.Empty(NewStore().Entries);
    }

    [Fact]
    public void AddOrUpdateEntry_sets_CreatedAt_from_clock_when_default()
    {
        clock.Now = DateTimeOffset.Parse("2026-03-03T10:00:00+00:00");
        var store = NewStore();
        var entry = new IgnoreEntry { Name = "Foo", WorldId = 1 };
        store.AddOrUpdateEntry(entry);
        Assert.Equal(clock.Now, store.Entries[0].CreatedAt);
    }

    [Fact]
    public void Changed_event_fires_on_mutation()
    {
        var store = NewStore();
        int calls = 0;
        store.Changed += (_, _) => calls++;
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo", WorldId = 1 });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Broken_file_leads_to_empty_store_plus_bak()
    {
        File.WriteAllText(path, "{ kaputtes json");
        var store = NewStore();
        Assert.Empty(store.Entries);
        Assert.True(File.Exists(path + ".bak"));
        Assert.NotEmpty(log.Errors);
    }

    [Fact]
    public void RemoveCategory_sets_CategoryId_of_affected_entries_to_null()
    {
        var store = NewStore();
        var cat = new IgnoreCategory { Name = "Spam" };
        store.AddOrUpdateCategory(cat);
        var entry = new IgnoreEntry { Name = "Foo Bar", WorldId = 73, CategoryId = cat.Id };
        store.AddOrUpdateEntry(entry);

        store.RemoveCategory(cat.Id);

        Assert.Single(store.Entries);
        Assert.Null(store.Entries[0].CategoryId);
        Assert.Empty(store.Categories);
    }

    [Fact]
    public void Save_leaves_no_tmp_file()
    {
        var store = NewStore();
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo", WorldId = 1 });
        Assert.False(File.Exists(path + ".tmp"));
    }

    public void Dispose()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        if (File.Exists(path + ".bak"))
        {
            File.Delete(path + ".bak");
        }
    }
}
