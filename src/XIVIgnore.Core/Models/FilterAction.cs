// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Models;

[Flags]
public enum FilterAction
{
    None = 0,
    Chat = 1 << 0,
    PartyFinder = 1 << 1,
    Nameplate = 1 << 2,
    CharacterHide = 1 << 3,
    All = Chat | PartyFinder | Nameplate | CharacterHide,
}
