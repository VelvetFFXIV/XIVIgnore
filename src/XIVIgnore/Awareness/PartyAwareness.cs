// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.Awareness;

// API notes (vs plan):
// - IPartyMember.Name is SeString; use .TextValue for plain string. ✓
// - IPartyMember.World is RowRef<World>; use .RowId for uint world id. ✓
// - IFramework.Update event signature: Action<IFramework>. ✓
// - CrossRealmMember.NameString → managed string; HomeWorld is Int16, cast to uint. ✓
public sealed unsafe class PartyAwareness : IDisposable
{
    private readonly IFramework _framework;
    private readonly IPartyList _party;
    private readonly PlayerMatcher _matcher;
    private readonly IgnoreStore _store;
    private readonly Configuration _config;
    private readonly INotificationManager _notifications;
    private readonly IChatGui _chat;
    private readonly WorldResolver _worlds;
    private readonly Localizer _loc;
    private readonly IPluginLog _log;

    private readonly HashSet<(string, uint)> _notified = new();
    private DateTimeOffset _lastScan = DateTimeOffset.MinValue;

    public PartyAwareness(IFramework framework, IPartyList party, PlayerMatcher matcher, IgnoreStore store,
                          Configuration config, INotificationManager notifications, IChatGui chat,
                          WorldResolver worlds, Localizer loc, IPluginLog log)
    {
        ArgumentNullException.ThrowIfNull(framework);
        _framework = framework; _party = party; _matcher = matcher; _store = store;
        _config = config; _notifications = notifications; _chat = chat;
        _worlds = worlds; _loc = loc; _log = log;
        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework fw)
    {
        try
        {
            if (!_config.PartyAwarenessNotify)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            if ((now - _lastScan).TotalSeconds < 3)
            {
                return;     // Throttle
            }

            _lastScan = now;

            var current = new HashSet<(string, uint)>();
            foreach (var m in _party)
            {
                var name = m.Name.TextValue;                    // SeString.TextValue → plain string
                var worldId = m.World.RowId;                   // RowRef<World>.RowId → uint
                var key = (name.ToLowerInvariant(), worldId);
                current.Add(key);
                if (_matcher.IsListed(name, worldId) && _notified.Add(key))
                {
                    Announce(name, worldId);
                }
            }
            // Cross-world party: InfoProxyCrossRealm (IsCrossRealm) ───────────────────────
            // GroupCount lives on the instance; static helpers read the actual data.
            // CrossRealmMember.NameString → string; HomeWorld → Int16 → uint.
            var proxy = InfoProxyCrossRealm.Instance();
            if (proxy != null && proxy->IsCrossRealm)
            {
                int groupCount = proxy->GroupCount;
                for (int g = 0; g < groupCount; g++)
                {
                    byte memberCount = InfoProxyCrossRealm.GetGroupMemberCount(g);
                    for (uint i = 0; i < memberCount; i++)
                    {
                        CrossRealmMember* m = InfoProxyCrossRealm.GetGroupMember(i, g);
                        if (m == null)
                        {
                            continue;
                        }

                        var name = m->NameString;
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        var worldId = (uint)m->HomeWorld;
                        var key = (name.ToLowerInvariant(), worldId);
                        current.Add(key);
                        if (_matcher.IsListed(name, worldId) && _notified.Add(key))
                        {
                            Announce(name, worldId);
                        }
                    }
                }
            }
            // ─────────────────────────────────────────────────────────────────────────────

            _notified.RemoveWhere(k => !current.Contains(k));    // left party → reportable again
        }
        catch (Exception ex)
        {
            _log.Error(ex, "PartyAwareness error");
        }
    }

    private void Announce(string name, uint worldId)
    {
        var entry = _store.Entries.FirstOrDefault(e => e.WorldId == worldId &&
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        var cat = _store.FindCategory(entry?.CategoryId)?.Name ?? "—";
        var note = string.IsNullOrWhiteSpace(entry?.Note) ? "" : $" · {entry!.Note}";
        var worldName = _worlds.ResolveName(worldId);

        _chat.Print(_loc.Get("warn.partyChat", name, worldName, cat, note));
        _notifications.AddNotification(new Notification
        {
            Title = _loc.Get("warn.notifTitle"),
            Content = _loc.Get("warn.notifBody", name, worldName, cat, note),
            Type = NotificationType.Warning,
        });
    }

    public void Dispose() => _framework.Update -= OnUpdate;
}
