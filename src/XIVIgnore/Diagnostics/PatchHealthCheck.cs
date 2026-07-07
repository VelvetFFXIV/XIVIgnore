// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

#if DEBUG
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace XIVIgnore.Diagnostics;

// DEBUG-ONLY patch self-test.
//
// Purpose: after an FFXIV patch, automatically check whether the few low-level touch points
// (FFXIVClientStructs) still work, an early warning BEFORE you manually test every feature.
// The probes can't prove "correct offsets", but they catch exactly the symptoms of a break:
// null instances, absurd counts, empty node walks, exceptions on access.
//
// This whole file is under #if DEBUG → in release builds the type does NOT exist and nothing
// is logged. The probes mirror the FFXIVClientStructs touch points the plugin relies on.
public sealed unsafe class PatchHealthCheck : IDisposable
{
    private readonly IFramework _framework;
    private readonly IObjectTable _objects;
    private readonly IGameGui _gameGui;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    private bool _hasRun;

    public PatchHealthCheck(IFramework framework, IObjectTable objects, IGameGui gameGui,
                            Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(framework);
        _framework = framework;
        _objects = objects;
        _gameGui = gameGui;
        _config = config;
        _log = log;
        framework.Update += OnUpdate;
    }

    // Runs automatically ONCE as soon as a player is in the world (touch points need an in-world context).
    private void OnUpdate(IFramework fw)
    {
        if (_hasRun)
        {
            return;
        }

        if (_objects.LocalPlayer is null)
        {
            return; // still on the loading screen/title
        }

        _hasRun = true;
        _framework.Update -= OnUpdate;
        Run("Auto-run on load");
    }

    /// <summary>Runs the self-test and writes a consolidated report to the Dalamud log.</summary>
    public void Run(string trigger)
    {
        try
        {
            string gv = "?";
            var fwk = Framework.Instance();
            if (fwk != null)
            {
                gv = fwk->GameVersionString.ToString();
            }

            bool firstRun = string.IsNullOrEmpty(_config.LastSeenGameVersion);
            bool patched = !firstRun && _config.LastSeenGameVersion != gv;

            _log.Information("[PatchCheck] ===== XIVIgnore touch-point self-test ({0}) =====", trigger);
            if (patched)
            {
                _log.Warning("[PatchCheck] Game version: {0} -> {1}  (PATCH DETECTED, re-check touch points thoroughly!)",
                            _config.LastSeenGameVersion, gv);
            }
            else
            {
                _log.Information("[PatchCheck] Game version: {0}{1}", gv,
                                firstRun ? "  (first capture)" : "  (unchanged since last run)");
            }

            int warn = 0;
            warn += Probe("CrossRealm proxy (cross-world detection)", ProbeCrossRealm);
            warn += Probe("GameObject.RenderFlags (hide character)", ProbeRenderFlags);
            warn += Probe("AddonPartyList (HUD party list, markers)", ProbePartyListAddon);
            warn += Probe("Generic node walk (social/HUD markers)", ProbeNodeWalk);

            if (warn == 0)
            {
                _log.Information("[PatchCheck] ===== Result: all {0} touch points ok =====", 4);
            }
            else
            {
                _log.Warning("[PatchCheck] ===== Result: {0} warning(s), review the low-level touch points =====", warn);
            }

            // Remember the version only AFTER the run, so the patch banner appears exactly once.
            if (_config.LastSeenGameVersion != gv)
            {
                _config.LastSeenGameVersion = gv;
                _config.Save();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[PatchCheck] self-test aborted");
        }
    }

    // Runs one probe: OK → Information, WARN/exception → Warning. Returns the number of warnings (0/1).
    private int Probe(string name, Func<(bool ok, string detail)> probe)
    {
        try
        {
            var (ok, detail) = probe();
            if (ok) { _log.Information("[PatchCheck] [OK]   {0}: {1}", name, detail); return 0; }
            _log.Warning("[PatchCheck] [WARN] {0}: {1}", name, detail);
            return 1;
        }
        catch (Exception ex)
        {
            _log.Warning("[PatchCheck] [WARN] {0}: exception {1} ({2}), likely struct/offset break",
                        name, ex.GetType().Name, ex.Message);
            return 1;
        }
    }

    // --- Probes (mirror the 4 touch points from the patch runbook) ---

    private (bool, string) ProbeCrossRealm()
    {
        var p = InfoProxyCrossRealm.Instance();
        if (p == null)
        {
            return (false, "Instance() == null");
        }

        int gc = p->GroupCount;
        bool sane = gc is >= 0 and <= 8;
        return (sane, $"Instance ok, IsCrossRealm={p->IsCrossRealm}, GroupCount={gc}"
                      + (sane ? "" : "  <- implausible"));
    }

    private (bool, string) ProbeRenderFlags()
    {
        var lp = _objects.LocalPlayer;
        if (lp is null)
        {
            return (true, "no local player (skipped)");
        }

        var go = (GameObject*)lp.Address;
        if (go == null)
        {
            return (false, "LocalPlayer.Address == null");
        }

        var rf = go->RenderFlags;
        return (true, $"readable (RenderFlags=0x{rf:X})");
    }

    private (bool, string) ProbePartyListAddon()
    {
        var addon = (AddonPartyList*)_gameGui.GetAddonByName("_PartyList").Address;
        if (addon == null)
        {
            return (true, "_PartyList not open (skipped)");
        }

        int mc = addon->MemberCount;
        bool sane = mc is >= 0 and <= 8;
        string nameInfo = string.Empty;
        if (mc > 0)
        {
            var node = addon->PartyMembers[0].Name;
            nameInfo = node == null ? "  <- Name-Node[0]==null!" : $", Name[0]='{node->NodeText.ToString()}'";
        }
        return (sane, $"MemberCount={mc}{nameInfo}" + (sane ? "" : "  <- implausible"));
    }

    private (bool, string) ProbeNodeWalk()
    {
        foreach (var addonName in new[] { "_PartyList", "PartyMemberList" })
        {
            var unit = (AtkUnitBase*)_gameGui.GetAddonByName(addonName).Address;
            if (unit == null)
            {
                continue;
            }

            int textNodes = CountTextNodes(&unit->UldManager, 0);
            return (textNodes > 0, $"{addonName}: {textNodes} TextNodes reachable"
                                   + (textNodes > 0 ? "" : "  <- 0 found, node layout may have changed"));
        }
        return (true, "neither _PartyList nor PartyMemberList open (skipped)");
    }

    // Identical defensive walk logic as in Social-/PartyListMarker (depth/count bounded).
    private int CountTextNodes(AtkUldManager* uld, int depth)
    {
        if (uld == null || depth > 16)
        {
            return 0;
        }

        int count = uld->NodeListCount;
        if (count <= 0 || count > 500)
        {
            return 0;
        }

        var list = uld->NodeList;
        if (list == null)
        {
            return 0;
        }

        int found = 0;
        for (int i = 0; i < count; i++)
        {
            var node = list[i];
            if (node == null)
            {
                continue;
            }

            if (node->Type == NodeType.Text)
            {
                found++;
            }
            else if ((uint)node->Type >= 1000)
            {
                var comp = ((AtkComponentNode*)node)->Component;
                if (comp != null)
                {
                    found += CountTextNodes(&comp->UldManager, depth + 1);
                }
            }
        }
        return found;
    }

    public void Dispose() => _framework.Update -= OnUpdate;
}
#endif
