// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using XIVIgnore.Services;

namespace XIVIgnore.Awareness;

// Reads the game blacklist read-only and yields importable candidates (world + name + note).
// Source is BlackListStringArray (parallel arrays PlayerNames/Homeworlds/Notes, same index) plus
// BlackListNumberArray.BlackListCount for the count. The real blacklist is NEVER modified.
public sealed unsafe class BlacklistReader
{
    // Fixed array size of the UI arrays (FixedSizeArray200) – a hard upper bound against outliers.
    private const int MaxEntries = 200;

    private readonly IGameGui _gameGui;
    private readonly WorldResolver _worlds;
    private readonly IPluginLog _log;

    public BlacklistReader(IGameGui gameGui, WorldResolver worlds, IPluginLog log)
    {
        _gameGui = gameGui;
        _worlds = worlds;
        _log = log;
    }

    /// <summary>True if the blacklist window is currently open AND visible.</summary>
    public bool IsBlacklistOpen()
    {
        // GetAddonByName returns AtkUnitBasePtr; the addon stays in memory even after closing
        // (hidden) — so check IsVisible, not just the address.
        try
        {
            var addon = (AtkUnitBase*)_gameGui.GetAddonByName("BlackList").Address;
            return addon != null && addon->IsVisible;
        }
        // Called every frame; deliberately treat transient addon states (loading/unloading) as
        // "not open", no per-frame log (spam).
        catch { return false; }
    }

    /// <summary>
    /// Candidates from the blacklist UI arrays. A row only counts if the world is resolvable
    /// AND the name is real (no account blocks: world "Blocked Account" / name "(…)").
    /// The game note is read along with it.
    /// </summary>
    public IReadOnlyList<(uint WorldId, string WorldName, string Name, string Note)> ReadCandidates()
    {
        var result = new List<(uint, string, string, string)>();
        try
        {
            // Only read when the blacklist is visible — otherwise the UI arrays may be stale/empty.
            var addon = (AtkUnitBase*)_gameGui.GetAddonByName("BlackList").Address;
            if (addon == null || !addon->IsVisible)
            {
                return result;
            }

            var num = BlackListNumberArray.Instance();
            var str = BlackListStringArray.Instance();
            if (num == null || str == null)
            {
                return result;
            }

            int count = num->BlackListCount;
            if (count < 0)
            {
                count = 0;
            }

            if (count > MaxEntries)
            {
                count = MaxEntries;
            }

            var names = str->PlayerNames;
            var homeworlds = str->Homeworlds;
            var notes = str->Notes;
            if (names.Length < count || homeworlds.Length < count || notes.Length < count)
            {
                return result;
            }

            for (int i = 0; i < count; i++)
            {
                var name = names[i].ExtractText().Trim();
                if (name.Length == 0 || name.StartsWith('('))
                {
                    continue;   // empty / "(Character name could not be retrieved.)"
                }

                var worldName = homeworlds[i].ExtractText().Trim();
                var wid = _worlds.TryResolveId(worldName);
                if (wid is null)
                {
                    continue;                                // "Blocked Account" / unknown world → skip
                }

                var note = notes[i].ExtractText().Trim();
                result.Add((wid.Value, worldName, name, note));
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "BlacklistReader error");
        }
        return result;
    }
}
