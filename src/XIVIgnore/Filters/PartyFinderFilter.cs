// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Filters;

// API notes (verified via ILSpy against installed Dalamud.dll v15):
// IPartyFinderGui.ReceiveListing fires with delegate:
//   PartyFinderListingEventDelegate(IPartyFinderListing listing, IPartyFinderListingEventArgs args)
// IPartyFinderListing.Name      → SeString  → .TextValue for plain string
// IPartyFinderListing.HomeWorld → RowRef<World> → .RowId for uint world id
// IPartyFinderListingEventArgs.Visible → bool { get; set; }, setting false hides the listing
public sealed class PartyFinderFilter : IDisposable
{
    private readonly IPartyFinderGui _partyFinderGui;
    private readonly PlayerMatcher _matcher;
    private readonly Configuration _config;
    private readonly IPluginLog _log;

    public PartyFinderFilter(IPartyFinderGui partyFinderGui, PlayerMatcher matcher,
                             Configuration config, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(partyFinderGui);
        _partyFinderGui = partyFinderGui;
        _matcher = matcher;
        _config = config;
        _log = log;
        partyFinderGui.ReceiveListing += OnReceiveListing;
    }

    private void OnReceiveListing(IPartyFinderListing listing, IPartyFinderListingEventArgs args)
    {
        try
        {
            if (!_config.PartyFinderFilterEnabled)
            {
                return;
            }

            var name = listing.Name.TextValue;
            var worldId = listing.HomeWorld.RowId;

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (_matcher.GetActions(name, worldId).HasFlag(FilterAction.PartyFinder))
            {
                args.Visible = false;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "PartyFinderFilter error");
        }
    }

    public void Dispose() => _partyFinderGui.ReceiveListing -= OnReceiveListing;
}
