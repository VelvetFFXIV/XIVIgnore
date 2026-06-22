// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Models;

/// <summary>
/// Carrier object from the trigger (context menu/command) to the confirm dialog:
/// the prefill for a NEW entry, before the user confirms it.
/// </summary>
public sealed record DraftSeed
{
    public string Name { get; init; } = string.Empty;
    public uint WorldId { get; init; }
    public string WorldName { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public FilterAction Actions { get; init; }
    public int ExpiryAmount { get; init; }              // <= 0 => permanent
    public DurationUnit ExpiryUnit { get; init; } = DurationUnit.Days;
    public string Note { get; init; } = string.Empty;
}
