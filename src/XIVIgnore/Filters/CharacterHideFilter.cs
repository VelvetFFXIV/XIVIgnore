// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.Filters;

// Hides whole player CHARACTERS client-side.
//
// Mechanics (verified against FFXIVClientStructs):
//   GameObject.RenderFlags (offset 0x118, [Flags] VisibilityFlags) is a PERSISTENT render flag
//   the game respects every frame. Setting the model bit (VisibilityFlags.Model = 1<<1) = the model
//   isn't drawn; clearing the bit = visible again.
//   (DisableDraw()/EnableDraw() would be a ONE-OFF call that the game immediately overrides next
//    frame → the character only briefly flickers out. Hence RenderFlags instead of a draw toggle.)
//   We re-set the bit for characters to hide every frame (idempotent, no destroy/recreate of the
//   model) and clear it on restore.
//
// What is NOT hidden:
//   The character stays present in game logic (AoE markers, hitbox, party frames, target, sound, chat).
//   Only the visual model is suppressed. Mounts/minions are separate objects and unaffected.
//
// Restore obligation:
//   On unload/toggle-off the bit must be cleared again, otherwise the character stays
//   invisible until the next zone change.
public sealed unsafe class CharacterHideFilter : IDisposable
{
    private const VisibilityFlags HideBit = VisibilityFlags.Model;

    private readonly IFramework _framework;
    private readonly IObjectTable _objectTable;
    private readonly PlayerMatcher _matcher;
    private readonly SafetyGate _safetyGate;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    // GameObjectIds whose character we're currently hiding (for restore).
    private readonly HashSet<ulong> _hidden = new();

    public CharacterHideFilter(IFramework framework, IObjectTable objectTable, PlayerMatcher matcher,
                                SafetyGate safetyGate, Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(framework);
        _framework = framework;
        _objectTable = objectTable;
        _matcher = matcher;
        _safetyGate = safetyGate;
        _config = config;
        _log = log;
        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        try
        {
            if (!_config.CharacterHideFilterEnabled && _hidden.Count == 0)
            {
                return; // nothing to do
            }

            var localId = _objectTable.LocalPlayer?.GameObjectId;
            var seen = new HashSet<ulong>();

            foreach (var obj in _objectTable.PlayerObjects)
            {
                if (obj is not IPlayerCharacter pc)
                {
                    continue;
                }

                var id = pc.GameObjectId;
                seen.Add(id);

                var shouldHide = _config.CharacterHideFilterEnabled
                    && (!localId.HasValue || id != localId.Value)   // never your own player
                    && _matcher.GetActions(pc.Name.TextValue, pc.HomeWorld.RowId).HasFlag(FilterAction.CharacterHide)
                    && _safetyGate.IsHidingSafe(pc);

                var go = (GameObject*)pc.Address;
                if (go == null)
                {
                    continue;
                }

                if (shouldHide)
                {
                    go->RenderFlags |= HideBit;   // re-set every frame (idempotent, persistent)
                    _hidden.Add(id);
                }
                else if (_hidden.Contains(id))
                {
                    go->RenderFlags &= ~HideBit;   // restore
                    _hidden.Remove(id);
                }
            }

            // Drop despawned objects from the set (no longer accessible; the flag is gone with the object).
            _hidden.RemoveWhere(id => !seen.Contains(id));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "CharacterHideFilter error");
        }
    }

    public void Dispose()
    {
        _framework.Update -= OnUpdate; // unsubscribe from the event first

        // Restore: clear the bit again on all still-hidden, present characters.
        try
        {
            foreach (var obj in _objectTable.PlayerObjects)
            {
                if (obj is not IPlayerCharacter pc)
                {
                    continue;
                }

                if (!_hidden.Contains(pc.GameObjectId))
                {
                    continue;
                }

                var go = (GameObject*)pc.Address;
                if (go == null)
                {
                    continue;
                }

                go->RenderFlags &= ~HideBit;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "CharacterHideFilter Dispose restore error");
        }

        _hidden.Clear();
    }
}
