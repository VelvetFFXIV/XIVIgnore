// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

namespace XIVIgnore.Core.Abstractions;

public interface ICoreLog
{
    public void Info(string message);
    public void Warning(string message);
    public void Error(string message, Exception? ex = null);
}
