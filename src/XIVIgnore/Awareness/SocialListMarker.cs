// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Awareness;

// Marks ignored players in the social window's list addons ("FriendList" = friend list,
// "SocialList" = player-search tab) by tinting the name text node red. ONE instance per
// addon (own color state) so two addons don't step on each other.
//
// Difference from SocialPartyMarker: there's no party context here → the displayed names are
// matched against the COMPLETE ignore list (by name only; the world isn't reliably reachable in
// the node walk → possible false hits on same-name players on another world, deliberately
// accepted, this is a "coverage" feature).
//
// CRASH SAFETY (identical to SocialPartyMarker):
//  - Only dereference node pointers from the CURRENT walk; drop stale entries without deref.
//  - Throttled rebuild (~200 ms), null/bounds/depth guards, handler in try/catch.
//  - Dispose: no color restore (pointers may be invalid).
public sealed unsafe class SocialListMarker : IDisposable
{
    private static readonly ByteColor MarkerColor = new() { R = 255, G = 80, B = 80, A = 255 };

    private const int MaxNodeListCount = 500;
    private const int MaxDepth = 16;

    private readonly IAddonLifecycle _addonLifecycle;
    private readonly string _addonName;
    private readonly IgnoreStore _store;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    private readonly Dictionary<nint, ByteColor> _originalColors = new();
    private readonly List<nint> _allTextNodes = new();
    private readonly HashSet<nint> _currentNodes = new();

    // Name set is rebuilt on a throttle (~200 ms, expensive); walk + coloring run EVERY frame.
    // With dynamic lists (friend list loading/scrolling) the game keeps re-coloring → only a
    // per-frame apply keeps the mark stable (otherwise it flickers).
    private HashSet<string>? _cachedNames;
    private DateTimeOffset _lastNameRebuild = DateTimeOffset.MinValue;
#if DEBUG
    private DateTimeOffset _lastDiagLog = DateTimeOffset.MinValue;
#endif

    public SocialListMarker(IAddonLifecycle addonLifecycle, string addonName,
                            IgnoreStore store, Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(addonLifecycle);
        _addonLifecycle = addonLifecycle;
        _addonName = addonName;
        _store = store;
        _config = config;
        _log = log;
        addonLifecycle.RegisterListener(AddonEvent.PostUpdate, addonName, OnAddonUpdate);
    }

    private void OnAddonUpdate(AddonEvent type, AddonArgs args)
    {
        try
        {
            if (!_config.SocialMarkerEnabled && _originalColors.Count == 0)
            {
                return;
            }

            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null)
            {
                return;
            }

            // Rebuild the expensive name set only ~every 200 ms; walk + coloring run every frame.
            var now = DateTimeOffset.UtcNow;
            if (_cachedNames == null || (now - _lastNameRebuild).TotalMilliseconds >= 200)
            {
                _cachedNames = _config.SocialMarkerEnabled ? BuildIgnoredNameSet() : null;
                _lastNameRebuild = now;
            }
            var ignoredNames = _config.SocialMarkerEnabled ? _cachedNames : null;

            // Every frame: fresh TextNodes from THIS addon (valid pointers).
            _allTextNodes.Clear();
            WalkUld(&addon->UldManager, 0);
            _currentNodes.Clear();
            foreach (var p in _allTextNodes)
            {
                _currentNodes.Add(p);
            }

            // Drop stale entries (node no longer present) WITHOUT deref.
            if (_originalColors.Count > 0)
            {
                List<nint>? stale = null;
                foreach (var p in _originalColors.Keys)
                {
                    if (!_currentNodes.Contains(p))
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

#if DEBUG
            if (_config.SocialMarkerEnabled && (now - _lastDiagLog).TotalSeconds >= 3.0)
            {
                _lastDiagLog = now;
                var texts = _allTextNodes
                    .Select(p => ((AtkTextNode*)p)->NodeText.ToString().Trim())
                    .Where(t => t.Length > 0)
                    .ToList();
                _log.Information($"[SLM:{_addonName}] textnodes: {texts.Count}, [{string.Join(", ", texts.Select(t => $"\"{t}\""))}]");
            }
#endif

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
            _log.Error(ex, $"[SLM:{_addonName}] SocialListMarker error in handler");
        }
    }

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

    // Name set from the COMPLETE ignore list (world-independent, with abbreviation variants).
    private HashSet<string> BuildIgnoredNameSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in _store.Entries)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                AddNameVariants(set, e.Name);
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
        _originalColors.Clear();
        _allTextNodes.Clear();
    }
}
