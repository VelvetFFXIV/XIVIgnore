// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace XIVIgnore.Services;

// SafetyGate: safety rules for visual filters (Nameplate, CharacterHide).
// Returns false when hiding is unsafe (combat / duty / party).
//
// Party detection covers:
//  1. Regular party (IPartyList), same-world / in the same instance context.
//  2. Cross-world party AND cross-world alliance (InfoProxyCrossRealm, via EntityId,
//     GetMemberByEntityId searches all sub-groups).
//  Same-world alliance (24-man) happens practically only in duty content → covered by BoundByDuty.
public sealed unsafe class SafetyGate
{
    private readonly ICondition _condition;
    private readonly IPartyList _party;
    private readonly Configuration _config;

    public SafetyGate(ICondition condition, IPartyList party, Configuration config)
    {
        _condition = condition;
        _party = party;
        _config = config;
    }

    /// <summary>
    /// Returns true if hiding the player (pc) is currently safe
    /// (no active safety flag applies).
    /// </summary>
    public bool IsHidingSafe(IPlayerCharacter pc)
    {
        ArgumentNullException.ThrowIfNull(pc);
        if (_config.SafetyExemptCombat && _condition[ConditionFlag.InCombat])
        {
            return false;
        }

        if (_config.SafetyExemptDuty && (
                _condition[ConditionFlag.BoundByDuty] ||
                _condition[ConditionFlag.BoundByDuty56] ||
                _condition[ConditionFlag.BoundByDuty95] ||
                _condition[ConditionFlag.InDeepDungeon]))
        {
            return false;
        }

        if (_config.SafetyExemptParty && IsInAnyParty(pc))
        {
            return false;
        }

        return true;
    }

    private bool IsInAnyParty(IPlayerCharacter pc)
    {
        // 1. Regular party (IPartyList).
        var name = pc.Name.TextValue;
        var worldId = pc.HomeWorld.RowId;
        foreach (var m in _party)
        {
            if (m.World.RowId == worldId &&
                string.Equals(m.Name.TextValue, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 2. Cross-world party/alliance: static lookup via the player's EntityId.
        //    Returns null if you're not in a cross-world party.
        if (InfoProxyCrossRealm.GetMemberByEntityId(pc.EntityId) != null)
        {
            return true;
        }

        return false;
    }
}
