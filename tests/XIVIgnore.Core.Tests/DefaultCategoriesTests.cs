// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Core.Tests.Fakes;

namespace XIVIgnore.Core.Tests;

public class DefaultCategoriesTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"xivignore-def-{Guid.NewGuid():N}.json");
    private readonly TestClock clock = new();
    private readonly NullCoreLog log = new();

    // Minimal DE localizer for the legacy tests (the category names are DE).
    private static Localizer DeLocalizer() => new(new Dictionary<string, Dictionary<string, string>>
    {
        ["en"] = new() { ["category.harassment"] = "Harassment", ["category.spam"] = "Spam", ["category.spoiler"] = "Spoiler", ["category.rmt"] = "RMT advertising", ["category.misc"] = "Miscellaneous" },
        ["de"] = new() { ["category.harassment"] = "Belästigung", ["category.spam"] = "Spam", ["category.spoiler"] = "Spoiler", ["category.rmt"] = "RMT-Werbung", ["category.misc"] = "Sonstiges" },
    }, "de");

    [Fact]
    public void EnsureDefaultCategories_seeds_defaults_only_once()
    {
        var loc = DeLocalizer();
        var store = new IgnoreStore(path, clock, log);
        store.EnsureDefaultCategories(loc);
        var countAfterFirst = store.Categories.Count;
        Assert.True(countAfterFirst >= 5);
        Assert.Contains(store.Categories, c => c.Name == "Sonstiges");

        store.EnsureDefaultCategories(loc);
        Assert.Equal(countAfterFirst, store.Categories.Count);
    }

    [Fact]
    public void EnsureDefaultCategories_skips_when_categories_already_exist()
    {
        var store = new IgnoreStore(path, clock, log);
        store.AddOrUpdateCategory(new IgnoreCategory { Name = "Eigene" });
        store.EnsureDefaultCategories(DeLocalizer());
        Assert.Single(store.Categories);
    }

    // Return-value tests

    [Fact]
    public void EnsureDefaultCategories_fresh_store_returns_id_of_last_category()
    {
        var store = new IgnoreStore(path, clock, log);
        var returned = store.EnsureDefaultCategories(DeLocalizer());
        Assert.NotNull(returned);
        Assert.Equal(store.Categories[^1].Id, returned!.Value);
    }

    [Fact]
    public void EnsureDefaultCategories_returns_id_of_existing_misc_category()
    {
        var store = new IgnoreStore(path, clock, log);
        var misc = new IgnoreCategory { Name = "Sonstiges" };
        store.AddOrUpdateCategory(misc);
        var returned = store.EnsureDefaultCategories(DeLocalizer());
        Assert.NotNull(returned);
        Assert.Equal(misc.Id, returned!.Value);
    }

    [Fact]
    public void EnsureDefaultCategories_returns_null_when_no_legacy_name_present()
    {
        var store = new IgnoreStore(path, clock, log);
        store.AddOrUpdateCategory(new IgnoreCategory { Name = "Eigene Kategorie" });
        var returned = store.EnsureDefaultCategories(DeLocalizer());
        Assert.Null(returned);
    }

    public void Dispose()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
