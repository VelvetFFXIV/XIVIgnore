// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Services;

namespace XIVIgnore.Core.Tests;

public class PlayerKeyTests
{
    [Theory]
    [InlineData("Test Player", "test player")]
    [InlineData("  Test Player  ", "test player")]
    [InlineData("TEST PLAYER", "test player")]
    public void Normalize_trims_and_lowercases(string input, string expected)
        => Assert.Equal(expected, PlayerKey.Normalize(input));

    [Fact]
    public void Of_combines_normalized_name_and_WorldId()
        => Assert.Equal(("test player", 73u), PlayerKey.Of("Test Player", 73));
}
