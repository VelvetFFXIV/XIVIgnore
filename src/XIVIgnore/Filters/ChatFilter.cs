// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Filters;

// API notes (vs plan):
// Suppression MUST happen on IChatGui.ChatMessage (verified against the Dalamud source):
//   "ChatMessage" fires when the message arrives in chat; PreventOriginal() suppresses it.
//   "CheckMessageHandled" fires ONLY if the message was NOT suppressed in ChatMessage
//   → so it's the wrong place to suppress.
// Both events use OnHandleableChatMessageDelegate(IHandleableChatMessage message).
// IHandleableChatMessage : IChatMessage; IChatMessage has LogKind (XivChatType), Sender (SeString),
//   IsHandled (bool); IHandleableChatMessage adds PreventOriginal().
// Sender: cross-world senders carry a PlayerPayload (name + WorldId). Same-world senders
// (incl. YOUR OWN messages) often carry NO payload / WorldId 0 → then world = own home world.
// IPartyMember.World is RowRef<World>; .RowId gives uint. ✓
// IPartyMember.Name is SeString; .TextValue gives plain string. ✓
// LocalPlayer: IClientState.LocalPlayer removed in Dalamud v15 → use IObjectTable.LocalPlayer.
public sealed class ChatFilter : IDisposable
{
    private readonly IChatGui _chat;
    private readonly IObjectTable _objectTable;
    private readonly PlayerMatcher _matcher;
    private readonly Configuration _config;
    private readonly IPartyList _party;
    private readonly ICondition _condition;
    private readonly IPluginLog _log;

    public ChatFilter(IChatGui chat, IObjectTable objectTable, PlayerMatcher matcher, Configuration config,
                      IPartyList party, ICondition condition, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(chat);
        _chat = chat; _objectTable = objectTable; _matcher = matcher; _config = config;
        _party = party; _condition = condition; _log = log;
        chat.ChatMessage += OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        try
        {
            if (message.IsHandled)
            {
                return;                           // already suppressed upstream
            }

            if (!_config.ChatFilterEnabled)
            {
                return;
            }

            if (!_config.FilteredChannels.Contains(message.LogKind))
            {
                return;
            }

            // Sender → (name, home world).
            var player = message.Sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
            string name;
            uint worldId;
            if (player is not null && player.World.RowId != 0)
            {
                name = player.PlayerName;
                worldId = player.World.RowId;                        // cross-world: RowRef<World>.RowId
            }
            else
            {
                // Same-world or own message: no/empty payload → name from sender text, world = own home world.
                var local = _objectTable.LocalPlayer;
                if (local is null)
                {
                    return;                           // not logged in
                }

                worldId = local.HomeWorld.RowId;
                name = message.Sender.TextValue;
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;         // system message without a player name → don't filter
                }
            }

            if (!_matcher.GetActions(name, worldId).HasFlag(FilterAction.Chat))
            {
                return;
            }

            if (_config.ChatExemptInPartyDuty && IsInPartyOrDuty(name, worldId))
            {
                return;
            }

            message.PreventOriginal();                               // suppress — same as plan ✓
        }
        catch (Exception ex)
        {
            _log.Error(ex, "ChatFilter error");
        }
    }

    private bool IsInPartyOrDuty(string name, uint worldId)
    {
        if (_condition[ConditionFlag.BoundByDuty])
        {
            return true;
        }

        foreach (var m in _party)
        {
            if (m.World.RowId == worldId &&
                string.Equals(m.Name.TextValue, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public void Dispose() => _chat.ChatMessage -= OnChatMessage;
}
