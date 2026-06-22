// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using System.Text.Json;
using XIVIgnore.Core.Models;

namespace XIVIgnore.Core.Tests;

public class SerializationTests
{
    [Fact]
    public void IgnoreListData_survives_JSON_roundtrip()
    {
        var catId = Guid.NewGuid();
        var data = new IgnoreListData
        {
            SchemaVersion = 1,
            Categories = { new IgnoreCategory { Id = catId, Name = "Spam", DefaultActions = FilterAction.Chat } },
            Entries =
            {
                new IgnoreEntry
                {
                    Name = "Test Player", WorldId = 73, WorldName = "Phoenix",
                    Note = "reason", CategoryId = catId,
                    ActionsOverride = FilterAction.Chat | FilterAction.PartyFinder,
                    CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"),
                    ExpiresAt = DateTimeOffset.Parse("2026-02-01T00:00:00+00:00"),
                }
            }
        };

        var json = JsonSerializer.Serialize(data);
        var back = JsonSerializer.Deserialize<IgnoreListData>(json)!;

        Assert.Equal(1, back.SchemaVersion);
        Assert.Single(back.Categories);
        Assert.Equal("Spam", back.Categories[0].Name);
        var e = Assert.Single(back.Entries);
        Assert.Equal("Test Player", e.Name);
        Assert.Equal(73u, e.WorldId);
        Assert.Equal(catId, e.CategoryId);
        Assert.Equal(FilterAction.Chat | FilterAction.PartyFinder, e.ActionsOverride);
        Assert.Equal(DateTimeOffset.Parse("2026-02-01T00:00:00+00:00"), e.ExpiresAt);
    }
}
