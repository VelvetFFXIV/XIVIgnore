// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Services;

public static class DefaultCategories
{
    /// <summary>Creates the localized default categories; by convention the fallback category "misc" comes last.</summary>
    public static IReadOnlyList<IgnoreCategory> Create(Localizer loc)
    {
        ArgumentNullException.ThrowIfNull(loc);
        return new[]
        {
            new IgnoreCategory { Name = loc.Get("category.harassment"), DefaultActions = FilterAction.Chat },
            new IgnoreCategory { Name = loc.Get("category.spam"),       DefaultActions = FilterAction.Chat },
            new IgnoreCategory { Name = loc.Get("category.spoiler"),    DefaultActions = FilterAction.Chat },
            new IgnoreCategory { Name = loc.Get("category.rmt"),        DefaultActions = FilterAction.Chat | FilterAction.PartyFinder },
            new IgnoreCategory { Name = loc.Get("category.misc"),       DefaultActions = FilterAction.Chat },
        };
    }
}
