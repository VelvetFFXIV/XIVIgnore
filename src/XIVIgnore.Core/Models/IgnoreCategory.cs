// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Models;

public sealed class IgnoreCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public uint? Color { get; set; }
    public FilterAction DefaultActions { get; set; } = FilterAction.Chat;
}
