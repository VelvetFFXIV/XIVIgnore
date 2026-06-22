// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Tests;

public class FilterActionTests
{
    [Fact]
    public void All_contains_every_single_flag()
    {
        var all = FilterAction.All;
        Assert.True(all.HasFlag(FilterAction.Chat));
        Assert.True(all.HasFlag(FilterAction.PartyFinder));
        Assert.True(all.HasFlag(FilterAction.Nameplate));
        Assert.True(all.HasFlag(FilterAction.CharacterHide));
    }

    [Fact]
    public void None_has_no_flags()
    {
        Assert.False(FilterAction.None.HasFlag(FilterAction.Chat));
        Assert.Equal(0, (int)FilterAction.None);
    }
}
