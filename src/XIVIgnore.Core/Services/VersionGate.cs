// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Services;

// Pure version-comparison logic for the one-time update notice (Dalamud-free, testable).
public static class VersionGate
{
    // A: "updated" — the running version is newer than the last reported one.
    // Empty/invalid baseline (fresh install) -> false.
    public static bool IsUpdate(Version running, string? lastNotified)
        => Version.TryParse(lastNotified, out var prev) && running > prev;

    // B: "available" — a newer version exists that hasn't been nudged about yet.
    public static bool ShouldNudge(Version running, Version latest, string? lastNudged)
        => latest > running && (!Version.TryParse(lastNudged, out var n) || n < latest);
}
