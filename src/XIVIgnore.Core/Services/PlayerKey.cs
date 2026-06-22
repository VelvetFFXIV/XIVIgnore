// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Services;

public static class PlayerKey
{
    /// <summary>Normalizes a player name for comparison/dedup: trimmed and lowercased invariantly.</summary>
    public static string Normalize(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Trim().ToLowerInvariant();
    }

    public static (string Name, uint WorldId) Of(string name, uint worldId)
        => (Normalize(name), worldId);
}
