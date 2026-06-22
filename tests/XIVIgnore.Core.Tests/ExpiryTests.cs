// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Core.Tests.Fakes;

namespace XIVIgnore.Core.Tests;

public class ExpiryTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"xivignore-exp-{Guid.NewGuid():N}.json");
    private readonly TestClock clock = new();
    private readonly NullCoreLog log = new();

    [Fact]
    public void PruneExpired_removes_expired_and_keeps_permanent_and_future()
    {
        clock.Now = DateTimeOffset.Parse("2026-06-20T12:00:00+00:00");
        var store = new IgnoreStore(path, clock, log);

        var permanent = new IgnoreEntry { Name = "Perm", WorldId = 1, ExpiresAt = null };
        var future = new IgnoreEntry { Name = "Future", WorldId = 1, ExpiresAt = clock.Now.AddDays(1) };
        var expired = new IgnoreEntry { Name = "Expired", WorldId = 1, ExpiresAt = clock.Now.AddSeconds(-1) };
        store.AddOrUpdateEntry(permanent);
        store.AddOrUpdateEntry(future);
        store.AddOrUpdateEntry(expired);

        var removed = store.PruneExpired();

        Assert.Single(removed);
        Assert.Equal("Expired", removed[0].Name);
        Assert.Equal(2, store.Entries.Count);
        Assert.DoesNotContain(store.Entries, e => e.Name == "Expired");
    }

    [Fact]
    public void PruneExpired_without_expired_fires_no_Changed()
    {
        var store = new IgnoreStore(path, clock, log);
        store.AddOrUpdateEntry(new IgnoreEntry { Name = "Perm", WorldId = 1 });
        int calls = 0;
        store.Changed += (_, _) => calls++;

        var removed = store.PruneExpired();

        Assert.Empty(removed);
        Assert.Equal(0, calls);
    }

    public void Dispose()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
