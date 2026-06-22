// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Awareness;

// Marks ignored members in the HUD party list (_PartyList) by tinting the name red.
// Covers both regular AND cross-world parties.
//
// IMPORTANT (verified in-game):
//  - The game RE-sets the name color on state changes (dead=gray, buffs/shield=re-render),
//    in the update BEFORE the draw. So register on `PostUpdate` (every frame, after the game update,
//    before the draw) and re-apply the red color EVERY frame — then our red gets drawn.
//    (PostDraw would be too late; a throttled update lets the game win on death/buff/shield.)
//  - The expensive part (reading slot names + matching) is throttled (every ~200 ms); per frame only
//    the color is set (by slot index, cheap).
//  - `PostRequestedUpdate` does not fire reliably for `_PartyList` (cross-world/open-world).
//  - The party list shows names ABBREVIATED ("Firstname L.") → match against several name variants.
public sealed unsafe class PartyListMarker : IDisposable
{
    private static readonly ByteColor MarkerColor = new() { R = 255, G = 80, B = 80, A = 255 };

    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IPartyList _party;
    private readonly PlayerMatcher _matcher;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    // Per slot (0–7): marked? and original color (for restoring).
    private readonly bool[] _marked = new bool[8];
    private readonly ByteColor[] _origColor = new ByteColor[8];
    // Which slots are currently ignored (recomputed on a throttle).
    private readonly HashSet<int> _ignoredSlots = new();
    private DateTimeOffset _lastRebuild = DateTimeOffset.MinValue;

    public PartyListMarker(IAddonLifecycle addonLifecycle, IPartyList party,
                           PlayerMatcher matcher, Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(addonLifecycle);
        _addonLifecycle = addonLifecycle;
        _party = party;
        _matcher = matcher;
        _config = config;
        _log = log;
        // PostUpdate: every frame, BEFORE drawing → our color actually gets drawn.
        // (PostDraw would be too late: the game update re-sets the color before the next draw.)
        addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "_PartyList", OnPartyListUpdate);
    }

    private void OnPartyListUpdate(AddonEvent type, AddonArgs args)
    {
        try
        {
            var addon = (AddonPartyList*)args.Addon.Address;
            if (addon == null)
            {
                return;
            }

            int slots = Math.Clamp(addon->MemberCount, 0, 8);

            // Throttled: which slots are ignored? (Reading text + matching is the costlier part.)
            var now = DateTimeOffset.UtcNow;
            if ((now - _lastRebuild).TotalMilliseconds >= 200)
            {
                _lastRebuild = now;
                _ignoredSlots.Clear();
                if (_config.PartyListMarkerEnabled)
                {
                    var ignored = BuildIgnoredNameSet();
                    for (int i = 0; i < slots; i++)
                    {
                        var node = addon->PartyMembers[i].Name;
                        if (node == null)
                        {
                            continue;
                        }

                        var key = NormalizeHudName(node->NodeText.ToString());
                        if (key.Length > 0 && ignored.Contains(key))
                        {
                            _ignoredSlots.Add(i);
                        }
                    }
                }
            }

            // Apply every frame (overrides the game color on death/buffs/shield).
            for (int i = 0; i < 8; i++)
            {
                var node = addon->PartyMembers[i].Name;
                if (node == null)
                {
                    continue;
                }

                bool wantRed = _config.PartyListMarkerEnabled && _ignoredSlots.Contains(i);
                if (wantRed)
                {
                    if (!_marked[i]) { _origColor[i] = node->TextColor; _marked[i] = true; }
                    node->TextColor = MarkerColor;
                }
                else if (_marked[i])
                {
                    node->TextColor = _origColor[i];
                    _marked[i] = false;
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "PartyListMarker error");
        }
    }

    // Set of HUD name variants for ignored party members (regular + cross-world).
    private HashSet<string> BuildIgnoredNameSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var m in _party)
        {
            var name = m.Name.TextValue;
            if (!string.IsNullOrEmpty(name) && _matcher.IsListed(name, m.World.RowId))
            {
                AddNameVariants(set, name);
            }
        }

        var proxy = InfoProxyCrossRealm.Instance();
        if (proxy != null && proxy->IsCrossRealm)
        {
            int groupCount = proxy->GroupCount;
            for (int g = 0; g < groupCount; g++)
            {
                byte memberCount = InfoProxyCrossRealm.GetGroupMemberCount(g);
                for (uint i = 0; i < memberCount; i++)
                {
                    CrossRealmMember* cm = InfoProxyCrossRealm.GetGroupMember(i, g);
                    if (cm == null)
                    {
                        continue;
                    }

                    var name = cm->NameString;
                    if (!string.IsNullOrEmpty(name) && _matcher.IsListed(name, (uint)cm->HomeWorld))
                    {
                        AddNameVariants(set, name);
                    }
                }
            }
        }
        return set;
    }

    private static string NormalizeHudName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var s = raw.Trim();
        int start = 0;
        while (start < s.Length && !char.IsLetter(s[start]))
        {
            start++;
        }

        return (start < s.Length ? s[start..] : s).ToLowerInvariant();
    }

    private static void AddNameVariants(HashSet<string> set, string full)
    {
        full = full.Trim();
        if (full.Length == 0)
        {
            return;
        }

        set.Add(full.ToLowerInvariant());

        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var first = parts[0];
            var last = parts[^1];
            set.Add($"{first} {last[0]}.".ToLowerInvariant());     // "Firstname L."
            set.Add($"{first[0]}. {last}".ToLowerInvariant());     // "F. Lastname"
            set.Add($"{first[0]}. {last[0]}.".ToLowerInvariant()); // "F. L."
        }
    }

    public void Dispose()
    {
        _addonLifecycle.UnregisterListener(OnPartyListUpdate);
        // Marked slots may stay red briefly until the game re-colors the party list
        // (happens on every HP tick/state change) — self-healing.
        Array.Clear(_marked);
    }
}
