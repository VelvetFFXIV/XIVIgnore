// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Awareness;

// EXPERIMENTAL: Marks ignored members in the social window (addon "PartyMemberList") by tinting
// the name text node red. The addon has no typed struct → we traverse the generic node tree via
// UldManager.NodeList.
//
// CRASH SAFETY (critical — a wrong pointer crashes the game):
//  - We dereference ONLY node pointers found in the CURRENT walk of this addon.
//  - Remembered original colors for nodes no longer in the current walk (window reopened,
//    tab switched) are dropped WITHOUT dereferencing → no stale-pointer access.
//  - All in the throttled rebuild (~200 ms); the social window is static (no per-frame needed).
//  - null checks, NodeListCount/depth bounded, entire handler in try/catch.
//  - Diagnostic log (~3 s) lists the found TextNode texts for in-game debugging.
public sealed unsafe class SocialPartyMarker : IDisposable
{
    private static readonly ByteColor MarkerColor = new() { R = 255, G = 80, B = 80, A = 255 };

    private const int MaxNodeListCount = 500;
    private const int MaxDepth = 16;

    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IPartyList _party;
    private readonly PlayerMatcher _matcher;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    // Per marked TextNode pointer: original color (only pointers from the respective current walk).
    private readonly Dictionary<nint, ByteColor> _originalColors = new();
    // TextNode pointers found in the current rebuild (reused).
    private readonly List<nint> _allTextNodes = new();

    private DateTimeOffset _lastRebuild = DateTimeOffset.MinValue;
#if DEBUG
    private DateTimeOffset _lastDiagLog = DateTimeOffset.MinValue;
#endif

    public SocialPartyMarker(IAddonLifecycle addonLifecycle, IPartyList party,
                             PlayerMatcher matcher, Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(addonLifecycle);
        _addonLifecycle = addonLifecycle;
        _party = party;
        _matcher = matcher;
        _config = config;
        _log = log;
        addonLifecycle.RegisterListener(AddonEvent.PostUpdate, "PartyMemberList", OnAddonUpdate);
    }

    private void OnAddonUpdate(AddonEvent type, AddonArgs args)
    {
        try
        {
            if (!_config.SocialMarkerEnabled && _originalColors.Count == 0)
            {
                return;
            }

            // Throttled: only then do we dereference pointers (always on a current addon).
            var now = DateTimeOffset.UtcNow;
            if ((now - _lastRebuild).TotalMilliseconds < 200)
            {
                return;
            }

            _lastRebuild = now;

            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null)
            {
                return;
            }

            // 1. Collect current TextNodes (fresh, valid pointers from THIS addon).
            _allTextNodes.Clear();
            WalkUld(&addon->UldManager, 0);
            var current = new HashSet<nint>(_allTextNodes);

            // 2. Drop stale entries (node no longer present) WITHOUT deref.
            if (_originalColors.Count > 0)
            {
                List<nint>? stale = null;
                foreach (var p in _originalColors.Keys)
                {
                    if (!current.Contains(p))
                    {
                        (stale ??= new()).Add(p);
                    }
                }

                if (stale != null)
                {
                    foreach (var p in stale)
                    {
                        _originalColors.Remove(p);
                    }
                }
            }

            // 3. Ignored-names set (empty/null when the feature is off → everything is restored below).
            var ignoredNames = _config.SocialMarkerEnabled ? BuildIgnoredNameSet() : null;

#if DEBUG
            // 4. Diagnostic log (debug build only).
            if (_config.SocialMarkerEnabled && (now - _lastDiagLog).TotalSeconds >= 3.0)
            {
                _lastDiagLog = now;
                var texts = _allTextNodes
                    .Select(p => ((AtkTextNode*)p)->NodeText.ToString().Trim())
                    .Where(t => t.Length > 0)
                    .ToList();
                _log.Information($"[SPM] found textnodes: {texts.Count} — [{string.Join(", ", texts.Select(t => $"\"{t}\""))}]");
            }
#endif

            // 5. Mark/restore — exclusively via CURRENT nodes (deref is safe).
            foreach (var ptr in _allTextNodes)
            {
                var tn = (AtkTextNode*)ptr;
                if (tn == null)
                {
                    continue;
                }

                bool wantRed = ignoredNames != null &&
                               ignoredNames.Contains(NormalizeHudName(tn->NodeText.ToString()));

                if (wantRed)
                {
                    if (!_originalColors.ContainsKey(ptr))
                    {
                        _originalColors[ptr] = tn->TextColor;
                    }

                    tn->TextColor = MarkerColor;
                }
                else if (_originalColors.TryGetValue(ptr, out var orig))
                {
                    tn->TextColor = orig;
                    _originalColors.Remove(ptr);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[SPM] SocialPartyMarker error in handler");
        }
    }

    // Defensive node walk: collects all AtkTextNode pointers via UldManager.NodeList,
    // recurses into component nodes (Type >= 1000). Null/bounds/depth guards.
    private void WalkUld(AtkUldManager* uld, int depth)
    {
        if (uld == null || depth > MaxDepth)
        {
            return;
        }

        int count = uld->NodeListCount;
        if (count <= 0 || count > MaxNodeListCount)
        {
            return;
        }

        var nodeList = uld->NodeList;
        if (nodeList == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var node = nodeList[i];
            if (node == null)
            {
                continue;
            }

            if (node->Type == NodeType.Text)
            {
                _allTextNodes.Add((nint)node);
            }
            else if ((uint)node->Type >= 1000)
            {
                var compNode = (AtkComponentNode*)node;
                var comp = compNode->Component;
                if (comp != null)
                {
                    WalkUld(&comp->UldManager, depth + 1);
                }
            }
        }
    }

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
        _addonLifecycle.UnregisterListener(OnAddonUpdate);
        // Deliberately NO color restore on dispose: the addon may no longer exist at unload
        // (invalid pointer → crash). On the next open the game renders the names fresh.
        _originalColors.Clear();
        _allTextNodes.Clear();
    }
}
