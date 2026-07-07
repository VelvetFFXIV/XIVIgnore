// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Abstractions;
using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Services;

public sealed class PlayerMatcher : IDisposable
{
    private readonly IgnoreStore _store;
    private readonly IClock _clock;
    private Dictionary<(string Name, uint WorldId), FilterAction> _cache = new();
    // Membership: every non-expired entry, REGARDLESS of its effect.
    // For awareness (marker/notification) merely being listed counts, even with effect None
    // ("watch only"). Filters, by contrast, still gate on GetActions (= the effective action).
    private HashSet<(string Name, uint WorldId)> _listed = new();

    public PlayerMatcher(IgnoreStore store, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);
        _store = store;
        _clock = clock;
        Rebuild();
        store.Changed += OnStoreChanged;
    }

    /// <summary>Returns the effective filter action for (Name, World), or <see cref="FilterAction.None"/> if nothing applies.</summary>
    public FilterAction GetActions(string name, uint worldId)
        => _cache.TryGetValue(PlayerKey.Of(name, worldId), out var a) ? a : FilterAction.None;

    public bool IsIgnored(string name, uint worldId) => GetActions(name, worldId) != FilterAction.None;

    /// <summary>
    /// True if a non-expired entry exists for (Name, World), regardless of the
    /// effective action. Meant for awareness (marker/notification); a "watch only"
    /// entry (action None) is listed but hidden by no filter.
    /// </summary>
    public bool IsListed(string name, uint worldId) => _listed.Contains(PlayerKey.Of(name, worldId));

    private void Rebuild()
    {
        var now = _clock.Now;
        var next = new Dictionary<(string, uint), FilterAction>();
        var nextListed = new HashSet<(string, uint)>();
        foreach (var e in _store.Entries)
        {
            if (e.ExpiresAt is { } exp && exp <= now)
            {
                continue;
            }

            var key = PlayerKey.Of(e.Name, e.WorldId);
            nextListed.Add(key);   // listed = awareness, regardless of the effect

            var actions = FilterActionRules.WithImpliedNameplate(
                e.ActionsOverride ?? _store.FindCategory(e.CategoryId)?.DefaultActions ?? FilterAction.None);
            if (actions == FilterAction.None)
            {
                continue;
            }

            next[key] = actions;   // effective action = filter
        }
        _cache = next;
        _listed = nextListed;
    }

    private void OnStoreChanged(object? sender, EventArgs e) => Rebuild();

    public void Dispose() => _store.Changed -= OnStoreChanged;
}
