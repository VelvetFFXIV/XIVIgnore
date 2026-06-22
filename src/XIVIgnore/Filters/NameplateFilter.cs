// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.Filters;

// Nameplate hiding via INamePlateGui.
//
// IMPORTANT (verified against the Dalamud source): handler.VisibilityFlags writes directly to
// ObjectData->VisibilityFlags. The game does NOT reset this value per frame
// (unlike e.g. MarkerIconId), and ResetState() doesn't touch it → the value is STICKY.
// Set it to 0 (hidden) and it stays 0 until you change it yourself again.
// In non-full updates OnNamePlateUpdate only delivers CHANGED plates → a hidden
// plate would never be restored (e.g. on entering a duty, where the safety rule kicks in).
//
// Solution: use OnDataUpdate (delivers ALL active plates on EVERY update). Remember the original
// (visible) VisibilityFlags value on first hiding and restore it once hiding should stop
// (safety kicks in / filter off / person leaves the list).
public sealed class NameplateFilter : IDisposable
{
    private readonly INamePlateGui _nameplateGui;
    private readonly IObjectTable _objectTable;
    private readonly PlayerMatcher _matcher;
    private readonly SafetyGate _safetyGate;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    // GameObjectId -> original (visible) VisibilityFlags value while we hide the plate.
    private readonly Dictionary<ulong, int> _hidden = new();
    // Reused per update (no allocation per frame) — IDs of the currently active nameplates.
    private readonly HashSet<ulong> _seen = new();

    public NameplateFilter(INamePlateGui nameplateGui, IObjectTable objectTable, PlayerMatcher matcher,
                           SafetyGate safetyGate, Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(nameplateGui);
        _nameplateGui = nameplateGui;
        _objectTable = objectTable;
        _matcher = matcher;
        _safetyGate = safetyGate;
        _config = config;
        _log = log;
        nameplateGui.OnDataUpdate += OnUpdate;
    }

    private void OnUpdate(INamePlateUpdateContext ctx, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        try
        {
            var enabled = _config.NameplateFilterEnabled;
            if (!enabled && _hidden.Count == 0)
            {
                return; // nothing to hide and nothing to restore
            }

            var localId = _objectTable.LocalPlayer?.GameObjectId;
            _seen.Clear();

            foreach (var handler in handlers)
            {
                _seen.Add(handler.GameObjectId);   // even without a PlayerCharacter — for the despawn cleanup below
                if (handler.PlayerCharacter is not { } pc)
                {
                    continue;
                }

                var id = pc.GameObjectId;

                var shouldHide = enabled
                    && (!localId.HasValue || id != localId.Value)   // never your own player
                    && _matcher.GetActions(pc.Name.TextValue, pc.HomeWorld.RowId).HasFlag(FilterAction.Nameplate)
                    && _safetyGate.IsHidingSafe(pc);

                if (shouldHide)
                {
                    // Remember the original (visible) value only on the FIRST hide.
                    if (!_hidden.ContainsKey(id))
                    {
                        _hidden[id] = handler.VisibilityFlags;
                    }

                    handler.VisibilityFlags = 0;
                }
                else if (_hidden.TryGetValue(id, out var original))
                {
                    // Restore: duty/combat/party safety kicks in, filter off, or person no longer listed.
                    handler.VisibilityFlags = original;
                    _hidden.Remove(id);
                }
            }

            // Despawn cleanup: remove entries whose nameplate is no longer active (object gone/out of
            // range). Safe, because a returning object gets a fresh, visible nameplate and is hidden
            // again if needed. This keeps 'hidden' bounded to the active nameplates (no growth).
            if (_hidden.Count > 0)
            {
                List<ulong>? stale = null;
                foreach (var key in _hidden.Keys)
                {
                    if (!_seen.Contains(key))
                    {
                        (stale ??= new()).Add(key);
                    }
                }

                if (stale != null)
                {
                    foreach (var key in stale)
                    {
                        _hidden.Remove(key);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "NameplateFilter error");
        }
    }

    public void Dispose()
    {
        _nameplateGui.OnDataUpdate -= OnUpdate;
        // Best-effort: trigger a full update so hidden nameplates are recomputed/visible again on
        // unload (otherwise not until the next zone/refresh).
        try { _nameplateGui.RequestRedraw(); } catch { /* doesn't matter on unload */ }
    }
}
