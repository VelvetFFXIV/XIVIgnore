// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Plugin.Services;
using XIVIgnore.Core.Abstractions;

namespace XIVIgnore.Logging;

public sealed class PluginCoreLog : ICoreLog
{
    private readonly IPluginLog _log;
    public PluginCoreLog(IPluginLog log) => _log = log;
    public void Info(string message) => _log.Information(message);
    public void Warning(string message) => _log.Warning(message);
    public void Error(string message, Exception? ex = null)
    {
        if (ex is null)
        {
            _log.Error(message);
        }
        else
        {
            _log.Error(ex, message);
        }
    }
}
