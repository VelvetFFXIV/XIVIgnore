// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Abstractions;

namespace XIVIgnore.Core.Tests.Fakes;

public sealed class TestClock : IClock
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2026-01-01T12:00:00+00:00");
}
