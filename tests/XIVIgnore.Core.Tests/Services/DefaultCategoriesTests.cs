// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Core.Tests.Services;

public class DefaultCategoriesTests
{
    private static Localizer Loc(string active) => new(new Dictionary<string, Dictionary<string, string>>
    {
        ["en"] = new() { ["category.harassment"] = "Harassment", ["category.spam"] = "Spam", ["category.spoiler"] = "Spoiler", ["category.rmt"] = "RMT advertising", ["category.misc"] = "Miscellaneous" },
        ["de"] = new() { ["category.harassment"] = "Belästigung", ["category.spam"] = "Spam", ["category.spoiler"] = "Spoiler", ["category.rmt"] = "RMT-Werbung", ["category.misc"] = "Sonstiges" },
    }, active);

    [Fact]
    public void Create_UsesLocalizedNames()
    {
        var cats = DefaultCategories.Create(Loc("en"));
        Assert.Contains(cats, c => c.Name == "Miscellaneous");
        Assert.DoesNotContain(cats, c => c.Name == "Sonstiges");
    }

    [Fact]
    public void Create_MiscCategoryIsLast_AndIdentifiable()
    {
        var cats = DefaultCategories.Create(Loc("de"));
        Assert.Equal("Sonstiges", cats[^1].Name); // misc = last entry (convention)
    }
}
