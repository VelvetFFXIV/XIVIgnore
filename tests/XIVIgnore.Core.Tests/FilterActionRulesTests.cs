// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Core.Tests;

public class FilterActionRulesTests
{
    [Fact]
    public void Character_implies_Nameplate()
        => Assert.Equal(FilterAction.CharacterHide | FilterAction.Nameplate,
                        FilterActionRules.WithImpliedNameplate(FilterAction.CharacterHide));

    [Fact]
    public void Character_plus_Chat_keeps_Chat_and_adds_Nameplate()
        => Assert.Equal(FilterAction.CharacterHide | FilterAction.Nameplate | FilterAction.Chat,
                        FilterActionRules.WithImpliedNameplate(FilterAction.CharacterHide | FilterAction.Chat));

    [Fact]
    public void Nameplate_alone_stays_unchanged()
        => Assert.Equal(FilterAction.Nameplate, FilterActionRules.WithImpliedNameplate(FilterAction.Nameplate));

    [Fact]
    public void Without_Character_no_change()
        => Assert.Equal(FilterAction.Chat | FilterAction.PartyFinder,
                        FilterActionRules.WithImpliedNameplate(FilterAction.Chat | FilterAction.PartyFinder));

    [Fact]
    public void None_stays_None()
        => Assert.Equal(FilterAction.None, FilterActionRules.WithImpliedNameplate(FilterAction.None));
}
