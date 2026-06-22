// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Services;

namespace XIVIgnore.Core.Tests;

public class VersionGateTests
{
    [Fact]
    public void IsUpdate_empty_baseline_is_false()
        => Assert.False(VersionGate.IsUpdate(new Version(0, 0, 0, 5), ""));

    [Fact]
    public void IsUpdate_older_baseline_is_true()
        => Assert.True(VersionGate.IsUpdate(new Version(0, 0, 0, 5), "0.0.0.4"));

    [Fact]
    public void IsUpdate_same_version_is_false()
        => Assert.False(VersionGate.IsUpdate(new Version(0, 0, 0, 5), "0.0.0.5"));

    [Fact]
    public void IsUpdate_newer_baseline_is_false()
        => Assert.False(VersionGate.IsUpdate(new Version(0, 0, 0, 5), "0.0.0.6"));

    [Fact]
    public void ShouldNudge_newer_available_not_yet_nudged_is_true()
        => Assert.True(VersionGate.ShouldNudge(new Version(0, 0, 0, 4), new Version(0, 0, 0, 6), ""));

    [Fact]
    public void ShouldNudge_already_nudged_is_false()
        => Assert.False(VersionGate.ShouldNudge(new Version(0, 0, 0, 4), new Version(0, 0, 0, 6), "0.0.0.6"));

    [Fact]
    public void ShouldNudge_even_newer_available_is_true()
        => Assert.True(VersionGate.ShouldNudge(new Version(0, 0, 0, 4), new Version(0, 0, 0, 7), "0.0.0.6"));

    [Fact]
    public void ShouldNudge_available_not_newer_than_running_is_false()
        => Assert.False(VersionGate.ShouldNudge(new Version(0, 0, 0, 6), new Version(0, 0, 0, 6), ""));
}
