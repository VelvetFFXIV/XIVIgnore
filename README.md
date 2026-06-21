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
- Your list is stored **locally** on your machine; nothing is sent anywhere.
- Purely **client-side filtering** — it does not change the game server, the real ignore
  list, or automate any gameplay.
- Ignored players stay fully visible during combat/duty/party content.

## Source code
Closed for now. XIVIgnore is a purely client-side quality-of-life filter — it doesn't
automate gameplay or communicate with the game server.

## Support
Found a bug or have a request? [Open an issue](https://github.com/VelvetFFXIV/XIVIgnore/issues).
