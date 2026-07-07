// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Services;

namespace XIVIgnore.Services;

// One-shot update notice in chat:
//  A "updated":       local version comparison (running vs. last reported).
//  B "please update": Dalamud's own CheckForUpdateAsync() (NO own network call,
//                     NO repo URL/persona in the source).
// Runs ONCE shortly after load, as soon as a player is in the world; then it unsubscribes.
public sealed class UpdateNotifier : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IChatGui _chat;
    private readonly IFramework _framework;
    private readonly IObjectTable _objects;
    private readonly Configuration _config;
    private readonly Localizer _loc;
    private readonly IPluginLog _log;
    private bool _done;

    public UpdateNotifier(IDalamudPluginInterface pluginInterface, IChatGui chat, IFramework framework,
                          IObjectTable objects, Configuration config, Localizer loc, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(framework);
        _pluginInterface = pluginInterface;
        _chat = chat;
        _framework = framework;
        _objects = objects;
        _config = config;
        _loc = loc;
        _log = log;
        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework fw)
    {
        if (_done)
        {
            return;
        }

        if (_objects.LocalPlayer is null)
        {
            return;   // only once logged in
        }

        _done = true;
        _framework.Update -= OnUpdate;

        try
        {
            var running = _pluginInterface.Manifest.AssemblyVersion;

            // A: "updated" (local). ALWAYS advance the baseline, even with the feature disabled,
            // so a later enable doesn't falsely report the current version as an "update".
            // Save only on an actual change.
            var isUpdate = VersionGate.IsUpdate(running, _config.LastNotifiedVersion);
            if (_config.LastNotifiedVersion != running.ToString())
            {
                _config.LastNotifiedVersion = running.ToString();
                _config.Save();
            }

            if (!_config.UpdateMessagesEnabled)
            {
                return;
            }

            if (isUpdate)
            {
                _chat.Print(_loc.Get("update.updated", running.ToString()));
            }

            // B: "please update" (Dalamud's own update check, async). Pass the last nudged state
            // as a snapshot (CheckAvailableAsync runs on a thread-pool thread after the await,
            // so read it here on the framework thread).
            _ = CheckAvailableAsync(running, _config.LastUpdateAvailableVersion);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "UpdateNotifier error");
        }
    }

    private async Task CheckAvailableAsync(Version running, string lastNudged)
    {
        try
        {
            var upd = await _pluginInterface.CheckForUpdateAsync();
            // PluginUpdate.Version is System.Version, no TryParse detour needed.
            if (upd?.Version is not { } latest)
            {
                return;           // no update available
            }

            if (!VersionGate.ShouldNudge(running, latest, lastNudged))
            {
                return;
            }

            // Chat + config save MUST run on the framework thread.
            await _framework.RunOnFrameworkThread(() =>
            {
                _chat.Print(_loc.Get("update.available", latest.ToString()));
                _config.LastUpdateAvailableVersion = latest.ToString();
                _config.Save();
            });
        }
        catch (Exception ex)
        {
            _log.Error(ex, "UpdateNotifier: CheckForUpdateAsync failed");
        }
    }

    public void Dispose() => _framework.Update -= OnUpdate;
}
