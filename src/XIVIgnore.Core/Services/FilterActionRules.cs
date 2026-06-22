// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Services;

// Business rules for FilterAction combinations.
public static class FilterActionRules
{
    // Character-Hide implies Nameplate: the nameplate is attached to the character model, so a
    // hidden character should show no plate either. One-way only (Nameplate alone is allowed).
    public static FilterAction WithImpliedNameplate(FilterAction actions)
        => actions.HasFlag(FilterAction.CharacterHide)
            ? actions | FilterAction.Nameplate
            : actions;
}
