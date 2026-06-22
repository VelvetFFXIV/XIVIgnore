// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;

namespace XIVIgnore;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool ChatFilterEnabled { get; set; } = true;
    public bool PartyFinderFilterEnabled { get; set; } = true;
    public bool NameplateFilterEnabled { get; set; } = true;
    public bool CharacterHideFilterEnabled { get; set; } // experimental, default OFF
    public HashSet<XivChatType> FilteredChannels { get; set; } = DefaultChannels();

    // Visibility safety rules (only take effect with the Phase-3 filters; persisted already)
    public bool SafetyExemptParty { get; set; } = true;
    public bool SafetyExemptCombat { get; set; } = true;
    public bool SafetyExemptDuty { get; set; } = true;

    // Chat exemption in party/duty (default OFF = chat stays filtered) — decision 0006
    public bool ChatExemptInPartyDuty { get; set; }

    public bool PartyAwarenessNotify { get; set; } = true;

    public bool PartyListMarkerEnabled { get; set; } = true;

    public bool SocialMarkerEnabled { get; set; } = true; // experimental

    // Open a review/edit window before adding (context menu/command).
    // Default ON; in old configs lacking this field ⇒ true (new players get the dialog).
    public bool ConfirmBeforeAdd { get; set; } = true;

    // One-shot update notice in chat: last reported own version ("updated") and
    // last nudged available version ("please update"); toggle (default on).
    public string LastNotifiedVersion { get; set; } = string.Empty;
    public string LastUpdateAvailableVersion { get; set; } = string.Empty;
    public bool UpdateMessagesEnabled { get; set; } = true;

    // Last seen game version — for the debug patch self-test (detecting "a patch happened").
    public string LastSeenGameVersion { get; set; } = string.Empty;

    // Stable Id of the fallback category ("misc") — replaces the former name lookup ("Sonstiges").
    public Guid? FallbackCategoryId { get; set; }

    // Canonical Dalamud pattern: no static plugin reference, but an injected
    // PluginInterface reference (set after load via Initialize).
    [NonSerialized] private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pi) => _pluginInterface = pi;

    public void Save() => _pluginInterface?.SavePluginConfig(this);

    public static HashSet<XivChatType> DefaultChannels() => new()
    {
        XivChatType.Say, XivChatType.Shout, XivChatType.Yell, XivChatType.TellIncoming,
        XivChatType.Party, XivChatType.Alliance, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
        XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4,
        XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8,
        XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8,
    };
}
