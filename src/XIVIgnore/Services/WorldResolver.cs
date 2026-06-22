// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Globalization;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace XIVIgnore.Services;

public sealed class WorldResolver
{
    private readonly IDataManager _data;
    public WorldResolver(IDataManager data) => _data = data;

    /// <summary>World name for an Id; falls back to the Id as text if the row is missing.</summary>
    public string ResolveName(uint worldId)
    {
        // API note: GetExcelSheet<World>() returns ExcelSheet<World> (non-nullable in v15).
        // TryGetRow is the safe lookup; GetRow throws if the row doesn't exist.
        var sheet = _data.GetExcelSheet<World>();
        return sheet.TryGetRow(worldId, out var row)
            ? row.Name.ExtractText()
            : worldId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Id for a public world name (case-insensitive), or null.</summary>
    public uint? TryResolveId(string worldName)
    {
        var sheet = _data.GetExcelSheet<World>();
        foreach (var w in sheet)
        {
            if (string.Equals(w.Name.ExtractText(), worldName, StringComparison.OrdinalIgnoreCase) && w.IsPublic)
            {
                return w.RowId;
            }
        }
        return null;
    }

    private IReadOnlyList<(uint Id, string Name)>? _publicWorldsCache;

    /// <summary>Public world names (for the UI dropdown), alphabetical. Cached once
    /// (the World sheet doesn't change at runtime) → no LINQ per frame in the open combo.</summary>
    public IReadOnlyList<(uint Id, string Name)> PublicWorlds()
        => _publicWorldsCache ??= _data.GetExcelSheet<World>()
            .Where(w => w.IsPublic && !string.IsNullOrEmpty(w.Name.ExtractText()))
            .Select(w => (w.RowId, w.Name.ExtractText()))
            .OrderBy(t => t.Item2)
            .ToList();
}
