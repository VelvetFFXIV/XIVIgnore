// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 VelvetFFXIV

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using XIVIgnore.Core.Localization;
using XIVIgnore.Core.Models;
using XIVIgnore.Core.Services;
using XIVIgnore.Services;

namespace XIVIgnore.EntryPoints;

public sealed class CommandHandler : IDisposable
{
    private const string Command = "/xivignore";
    private readonly ICommandManager _commands;
    private readonly IgnoreStore _store;
    private readonly WorldResolver _worlds;
    private readonly IChatGui _chat;
    private readonly Action _toggleWindow;
    private readonly ITargetManager _targets;
    private readonly IPartyList _party;
    private readonly Configuration _config;
    private readonly Localizer _loc;
    private readonly Action<DraftSeed> _beginDraft;
    private readonly Action? _onHealth; // only set in debug builds (patch self-test)

    public CommandHandler(ICommandManager commands, IgnoreStore store, WorldResolver worlds,
                          IChatGui chat, Action toggleWindow, ITargetManager targets, IPartyList party,
                          Configuration config, Localizer loc, Action<DraftSeed> beginDraft,
                          Action? onHealth = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(loc);
        _commands = commands; _store = store; _worlds = worlds;
        _chat = chat; _toggleWindow = toggleWindow;
        _targets = targets; _party = party;
        _config = config; _loc = loc; _onHealth = onHealth;
        _beginDraft = beginDraft;
        commands.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = loc.Get("cmd.help"),
        });
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Length == 0) { _toggleWindow(); return; }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        switch (sub)
        {
            case "add": HandleAdd(rest); break;
            case "remove": HandleRemove(rest); break;
            case "list": HandleList(); break;
            case "health":
                if (_onHealth != null) { _onHealth(); _chat.Print("[XIVIgnore] Patch self-test run, result in /xllog."); }
                else
                {
                    _chat.Print("[XIVIgnore] Patch self-test is only available in debug builds.");
                }

                break;
            default: _chat.Print(_loc.Get("cmd.unknown", sub)); break;
        }
    }

    private bool TryParse(string nameAtWorld, out string name, out uint worldId)
    {
        name = string.Empty; worldId = 0;
        var at = nameAtWorld.LastIndexOf('@');
        if (at <= 0 || at >= nameAtWorld.Length - 1)
        {
            return false;
        }

        name = nameAtWorld[..at].Trim();
        var id = _worlds.TryResolveId(nameAtWorld[(at + 1)..].Trim());
        if (id is null)
        {
            return false;
        }

        worldId = id.Value;
        return true;
    }

    // Accepts FFXIV placeholders (<t>, <f>, <mo>, <2>..<8>) OR "Name@World".
    private bool TryResolveTarget(string input, out string name, out uint worldId)
    {
        input = input.Trim();
        if (input.StartsWith('<') && input.EndsWith('>'))
        {
            return TryResolvePlaceholder(input, out name, out worldId);
        }

        return TryParse(input, out name, out worldId);
    }

    // Resolves FFXIV placeholders via Dalamud APIs (no PronounModule needed):
    // <t>/<f>/<mo> via ITargetManager, <2>..<8> via the party list. Only real players are taken.
    private bool TryResolvePlaceholder(string token, out string name, out uint worldId)
    {
        name = string.Empty; worldId = 0;
        var t = token.ToLowerInvariant();

        // Party slots <2>..<8> (slot 1 = yourself → not ignorable).
        if (t.Length == 3 && t[0] == '<' && t[2] == '>' && t[1] is >= '2' and <= '8')
        {
            int slot = t[1] - '1';                         // <2> → index 1
            var m = slot < _party.Length ? _party[slot] : null;
            if (m != null) { name = m.Name.TextValue; worldId = m.World.RowId; }
            return !string.IsNullOrEmpty(name) && worldId != 0;
        }

        // Target placeholders; only real players yield name + home world.
        IGameObject? obj = t switch
        {
            "<t>" => _targets.Target,
            "<f>" => _targets.FocusTarget,
            "<mo>" => _targets.MouseOverTarget,
            _ => null,
        };
        if (obj is IPlayerCharacter pc)
        {
            name = pc.Name.TextValue;
            worldId = pc.HomeWorld.RowId;
        }
        return !string.IsNullOrEmpty(name) && worldId != 0;
    }

    private void HandleAdd(string rest)
    {
        if (!TryResolveTarget(rest, out var name, out var worldId))
        { _chat.Print(_loc.Get("cmd.addFormat")); return; }

        var worldName = _worlds.ResolveName(worldId);
        var exists = _store.Entries.Any(e => e.WorldId == worldId &&
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exists) { _chat.Print(_loc.Get("ctx.alreadyOnList", name, worldName)); return; }

        // With confirm dialog: open a draft (conservative default effect, no expiry).
        if (_config.ConfirmBeforeAdd)
        {
            _beginDraft(new DraftSeed
            {
                Name = name,
                WorldId = worldId,
                WorldName = worldName,
                CategoryId = _config.FallbackCategoryId,
                Actions = FilterAction.Chat | FilterAction.PartyFinder,
                ExpiryAmount = 0,
                ExpiryUnit = DurationUnit.Days,
            });
            return;
        }

        // Add immediately (legacy behavior).
        _store.AddOrUpdateEntry(new IgnoreEntry
        {
            Name = name,
            WorldId = worldId,
            WorldName = worldName,
            CategoryId = _config.FallbackCategoryId,
        });
        _chat.Print(_loc.Get("cmd.added", name, worldName));
    }

    private void HandleRemove(string rest)
    {
        if (!TryResolveTarget(rest, out var name, out var worldId))
        { _chat.Print(_loc.Get("cmd.removeFormat")); return; }

        var entry = _store.Entries.FirstOrDefault(e =>
            e.WorldId == worldId && string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (entry is null) { _chat.Print(_loc.Get("cmd.notFound")); return; }
        _store.RemoveEntry(entry.Id);
        _chat.Print(_loc.Get("cmd.removed", name, _worlds.ResolveName(worldId)));
    }

    private void HandleList()
    {
        if (_store.Entries.Count == 0) { _chat.Print(_loc.Get("cmd.listEmpty")); return; }
        _chat.Print(_loc.Get("cmd.listHeader", _store.Entries.Count));
        foreach (var e in _store.Entries)
        {
            _chat.Print($"  • {e.Name}@{e.WorldName}");
        }
    }

    public void Dispose() => _commands.RemoveHandler(Command);
}
