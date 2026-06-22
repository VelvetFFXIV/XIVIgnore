// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Abstractions;

namespace XIVIgnore.Core.Tests.Fakes;

public sealed class NullCoreLog : ICoreLog
{
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
    public void Info(string message) { }
    public void Warning(string message) => Warnings.Add(message);
    public void Error(string message, Exception? ex = null) => Errors.Add(message);
}
