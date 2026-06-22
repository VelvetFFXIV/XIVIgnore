// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using XIVIgnore.Core.Localization;

namespace XIVIgnore.UI.Tabs;

public sealed class SettingsTab
{
    private static readonly XivChatType[] AllChannels =
    {
        XivChatType.Say, XivChatType.Shout, XivChatType.Yell, XivChatType.TellIncoming,
        XivChatType.Party, XivChatType.Alliance, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
        XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4,
        XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8,
        XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8,
    };

    private readonly Configuration _config;
    private readonly Localizer _loc;

    public SettingsTab(Configuration config, Localizer loc)
    {
        _config = config;
        _loc = loc;
    }

    public void Draw()
    {
        var changed = false;

        var chatOn = _config.ChatFilterEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.chatFilter"), ref chatOn)) { _config.ChatFilterEnabled = chatOn; changed = true; }

        var pfOn = _config.PartyFinderFilterEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.pfFilter"), ref pfOn)) { _config.PartyFinderFilterEnabled = pfOn; changed = true; }

        var npOn = _config.NameplateFilterEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.nameplate"), ref npOn)) { _config.NameplateFilterEnabled = npOn; changed = true; }

        var chOn = _config.CharacterHideFilterEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.charHide"), ref chOn)) { _config.CharacterHideFilterEnabled = chOn; changed = true; }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(_loc.Get("settings.charHideHint"));
        }

        var chatExempt = _config.ChatExemptInPartyDuty;
        if (ImGui.Checkbox(_loc.Get("settings.chatExempt"), ref chatExempt))
        { _config.ChatExemptInPartyDuty = chatExempt; changed = true; }

        if (ImGui.CollapsingHeader(_loc.Get("settings.channelsHeader")))
        {
            foreach (var ch in AllChannels)
            {
                var on = _config.FilteredChannels.Contains(ch);
                if (ImGui.Checkbox(ch.ToString(), ref on))
                {
                    if (on)
                    {
                        _config.FilteredChannels.Add(ch);
                    }
                    else
                    {
                        _config.FilteredChannels.Remove(ch);
                    }

                    changed = true;
                }
            }
        }

        ImGui.Separator();
        ImGui.TextUnformatted(_loc.Get("settings.safetyHeader"));
        var sp = _config.SafetyExemptParty;
        if (ImGui.Checkbox(_loc.Get("settings.safetyParty"), ref sp)) { _config.SafetyExemptParty = sp; changed = true; }
        var sc = _config.SafetyExemptCombat;
        if (ImGui.Checkbox(_loc.Get("settings.safetyCombat"), ref sc)) { _config.SafetyExemptCombat = sc; changed = true; }
        var sd = _config.SafetyExemptDuty;
        if (ImGui.Checkbox(_loc.Get("settings.safetyDuty"), ref sd)) { _config.SafetyExemptDuty = sd; changed = true; }

        ImGui.Separator();
        var pa = _config.PartyAwarenessNotify;
        if (ImGui.Checkbox(_loc.Get("settings.partyAwareness"), ref pa))
        { _config.PartyAwarenessNotify = pa; changed = true; }

        var plm = _config.PartyListMarkerEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.partyListMarker"), ref plm))
        { _config.PartyListMarkerEnabled = plm; changed = true; }

        var spm = _config.SocialMarkerEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.socialMarker"), ref spm))
        { _config.SocialMarkerEnabled = spm; changed = true; }

        ImGui.Separator();
        var confirmAdd = _config.ConfirmBeforeAdd;
        if (ImGui.Checkbox(_loc.Get("settings.confirmBeforeAdd"), ref confirmAdd))
        { _config.ConfirmBeforeAdd = confirmAdd; changed = true; }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(_loc.Get("settings.confirmBeforeAddHint"));
        }

        var updMsg = _config.UpdateMessagesEnabled;
        if (ImGui.Checkbox(_loc.Get("settings.updateMessages"), ref updMsg))
        { _config.UpdateMessagesEnabled = updMsg; changed = true; }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(_loc.Get("settings.updateMessagesHint"));
        }

        ImGui.Separator();
        ImGui.TextDisabled(_loc.Get("settings.autoPruneHint"));

        if (changed)
        {
            _config.Save();
        }
    }
}
