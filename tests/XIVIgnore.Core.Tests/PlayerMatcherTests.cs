// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Core.Tests.Fakes;

namespace XIVIgnore.Core.Tests;

public class PlayerMatcherTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"xivignore-match-{Guid.NewGuid():N}.json");
    private readonly TestClock clock = new();
    private readonly NullCoreLog log = new();
    private IgnoreStore store = null!;

    private PlayerMatcher NewMatcher()
    {
        store = new IgnoreStore(path, clock, log);
        return new PlayerMatcher(store, clock);
    }

    [Fact]
    public void Unknown_player_returns_None()
    {
        var m = NewMatcher();
        Assert.Equal(FilterAction.None, m.GetActions("Niemand", 73));
        Assert.False(m.IsIgnored("Niemand", 73));
    }

    [Fact]
    public void Override_takes_precedence_over_category()
    {
        var m = NewMatcher();
        var cat = new IgnoreCategory { Name = "Spam", DefaultActions = FilterAction.Chat };
        store.AddOrUpdateCategory(cat);
        store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = "Foo Bar",
            WorldId = 73,
            CategoryId = cat.Id,
            ActionsOverride = FilterAction.Chat | FilterAction.CharacterHide,
        });

        // Character implies Nameplate (FilterActionRules): CharacterHide pulls Nameplate in.
        Assert.Equal(FilterAction.Chat | FilterAction.CharacterHide | FilterAction.Nameplate, m.GetActions("Foo Bar", 73));
    }

    [Fact]
    public void Without_override_category_default_applies()
    {
        var m = NewMatcher();
        var cat = new IgnoreCategory { Name = "Nervig", DefaultActions = FilterAction.Chat };
        store.AddOrUpdateCategory(cat);
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73, CategoryId = cat.Id });

        Assert.Equal(FilterAction.Chat, m.GetActions("Foo Bar", 73));
    }

    [Fact]
    public void Category_default_CharacterHide_implies_Nameplate()
    {
        var m = NewMatcher();
        var cat = new IgnoreCategory { Name = "Hide", DefaultActions = FilterAction.CharacterHide };
        store.AddOrUpdateCategory(cat);
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73, CategoryId = cat.Id });

        // Category default with CharacterHide pulls in Nameplate (via FilterActionRules in the matcher).
        Assert.Equal(FilterAction.CharacterHide | FilterAction.Nameplate, m.GetActions("Foo Bar", 73));
    }

    [Fact]
    public void Without_category_and_without_override_is_None()
    {
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73 });
        Assert.Equal(FilterAction.None, m.GetActions("Foo Bar", 73));
    }

    [Fact]
    public void Matching_is_case_insensitive_and_world_specific()
    {
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73, ActionsOverride = FilterAction.Chat });

        Assert.Equal(FilterAction.Chat, m.GetActions("foo bar", 73));
        Assert.Equal(FilterAction.None, m.GetActions("Foo Bar", 99));
    }

    [Fact]
    public void Cache_updates_on_store_change()
    {
        var m = NewMatcher();
        Assert.Equal(FilterAction.None, m.GetActions("Foo Bar", 73));
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73, ActionsOverride = FilterAction.Chat });
        Assert.Equal(FilterAction.Chat, m.GetActions("Foo Bar", 73));
    }

    [Fact]
    public void Expired_entries_do_not_count()
    {
        clock.Now = DateTimeOffset.Parse("2026-06-20T12:00:00+00:00");
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = "Foo Bar",
            WorldId = 73,
            ActionsOverride = FilterAction.Chat,
            ExpiresAt = clock.Now.AddSeconds(-1),
        });
        Assert.Equal(FilterAction.None, m.GetActions("Foo Bar", 73));
    }

    // ── IsListed: Awareness (marker/notification) depends on LIST membership,
    //    NOT on the effective action — a "watch only" entry (action None) counts. ──

    [Fact]
    public void Entry_without_action_is_listed_but_not_ignored()
    {
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = "Foo Bar",
            WorldId = 73,
            ActionsOverride = FilterAction.None,
        });

        Assert.True(m.IsListed("Foo Bar", 73));      // on the list → awareness
        Assert.False(m.IsIgnored("Foo Bar", 73));    // no action → no filter
        Assert.Equal(FilterAction.None, m.GetActions("Foo Bar", 73));
    }

    [Fact]
    public void Entry_without_category_and_override_is_still_listed()
    {
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73 });

        Assert.True(m.IsListed("Foo Bar", 73));
        Assert.False(m.IsIgnored("Foo Bar", 73));
    }

    [Fact]
    public void Entry_with_action_is_listed_and_ignored()
    {
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73, ActionsOverride = FilterAction.Chat });

        Assert.True(m.IsListed("Foo Bar", 73));
        Assert.True(m.IsIgnored("Foo Bar", 73));
    }

    [Fact]
    public void Expired_entry_is_not_listed()
    {
        clock.Now = DateTimeOffset.Parse("2026-06-20T12:00:00+00:00");
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = "Foo Bar",
            WorldId = 73,
            ActionsOverride = FilterAction.None,
            ExpiresAt = clock.Now.AddSeconds(-1),
        });

        Assert.False(m.IsListed("Foo Bar", 73));
    }

    [Fact]
    public void IsListed_is_case_insensitive_and_world_specific()
    {
        var m = NewMatcher();
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Foo Bar", WorldId = 73 });

        Assert.True(m.IsListed("foo bar", 73));
        Assert.False(m.IsListed("Foo Bar", 99));
        Assert.False(m.IsListed("Niemand", 73));
    }

    public void Dispose()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
