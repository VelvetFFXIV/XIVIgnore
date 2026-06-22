🇬🇧 English | 🇩🇪 [Deutsch](README.de.md)

# XIVIgnore

> Virtually extends the in-game ignore list — client-side, in your client only.

FFXIV's real ignore list is small and server-side. **XIVIgnore** keeps your own local
"extended ignore list" and filters those players **on your client**: hide their chat,
Party Finder listings, nameplates, or even their character model — organised with
categories, notes and automatic expiry.

## Features
- **Hide chat** from listed players (per-channel configurable)
- **Hide Party Finder listings** from listed hosts
- **Hide nameplates** of listed players
- **Hide character model** (experimental, off by default)
- **Categories** (Harassment, Spam, Spoiler, RMT, Misc) with default actions + per-entry overrides
- **Notes** and **auto-expiry** (minutes → months, or permanent)
- **Party awareness** — warns you in chat and marks a listed player in the party list
  and the Social window if one joins your party (incl. cross-world)
- **Watch-only entries** — add someone with **no** effect checked to just *flag* them: they're
  still shown in red and trigger the party warning, but nothing is hidden or filtered
  (a community-requested feature)
- **Safety first** — ignored players stay **visible in combat, duties and your party**,
  so you never lose track of mechanics
- **English & German UI**, follows your Dalamud language (switches live)

## Not a competitor to Visibility
XIVIgnore is **not** a competitor to, or a replacement for,
[Visibility](https://github.com/SheepGoMeh/VisibilityPlugin) — and it makes **no claim** to be
"better". They solve different problems:

- **Visibility** declutters your view and helps performance — hide players, pets and chocobos
  *by group* (party, friends, free company, dead), plus its VoidList. Huge respect to that project.
- **XIVIgnore** works one player at a time — an ignore/moderation tool aimed at a specific
  person rather than whole groups.

Want group-based hiding, or hiding pets/chocobos? Use **Visibility** — it does that well, and
XIVIgnore intentionally doesn't try to.

**If you run both:** both plugins hide a character's *model* via the same game render flag, so
telling *both* to hide the *same* player can cause flicker. For model-hiding, let just one
plugin handle a given player.

## Adding players
- **Right-click** a player → *Add to virtual ignore list* → pick a duration
- The plugin window (`/xivignore`)
- Slash command: `/xivignore add First Last@World`

> **Tip:** By default, adding (right-click or command) first opens a **review window** — check
> or adjust what will be ignored (filters, category, note, duration), then confirm to add it
> or close it to add nothing. Prefer instant one-click adds? Turn this off under
> **Settings → "Confirm before adding"**.

## Commands
| Command | Action |
|---|---|
| `/xivignore` | Open the window |
| `/xivignore add First Last@World` | Add a player |
| `/xivignore remove First Last@World` | Remove a player |
| `/xivignore list` | List entries |

## Installation
1. In Dalamud: **Settings → Experimental → Custom Plugin Repositories**
2. Add this URL:
   ```
   https://raw.githubusercontent.com/VelvetFFXIV/DalamudPlugins/main/pluginmaster.json
   ```
3. **Save** → open the Plugin Installer → search **XIVIgnore** → **Install**

## Privacy & fair play
- **No tracking IDs.** Listed players are keyed only by the public character **name + home world**
  you already see — XIVIgnore stores **no ContentId** or any unique character/account identifier, and
  cannot follow anyone across a name change or world transfer.
- Your list is stored **locally** on your machine; nothing is sent anywhere.
- Purely **client-side filtering** — it does not communicate with the game server, change the
  real ignore list, or automate any gameplay.

## Known limitations
- **Chat sound isn't filtered yet** — when a listed player's chat is hidden, the game still
  plays the incoming-tell/whisper *sound*; only the text is suppressed. Clean per-player sound
  suppression isn't currently possible, but it's on the radar.

## Source code
XIVIgnore is **open source** under the **GNU AGPL-3.0** license — the full source lives in
this repository.

## Support
Found a bug or have a request? [Open an issue](https://github.com/VelvetFFXIV/XIVIgnore/issues).
