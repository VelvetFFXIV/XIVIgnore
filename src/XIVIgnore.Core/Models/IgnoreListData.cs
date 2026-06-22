// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Models;

public sealed class IgnoreListData
{
    public int SchemaVersion { get; set; } = 1;
    public List<IgnoreCategory> Categories { get; set; } = new();
    public List<IgnoreEntry> Entries { get; set; } = new();
}
