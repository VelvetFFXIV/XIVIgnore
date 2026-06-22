🇬🇧 English | 🇩🇪 [Deutsch](README.de.md)

# XIVIgnore

> A bigger ignore list for FFXIV, kept on your own machine.

FFXIV's built-in ignore list is short and lives on the server. XIVIgnore keeps a longer
one locally and quietly filters the people on it: their chat, their Party Finder listings,
their nameplates, and if you want, their character model. Sort entries into categories,
leave yourself a note, and let them expire on their own.

## Features
- **Hide chat** from listed players, configurable per channel.
- **Hide their Party Finder listings.**
- **Hide their nameplates.**
- **Hide the character model** (experimental, off by default).
- **Categories** — Harassment, Spam, Spoiler, RMT, Misc — each with a default action you can
  override for a single person.
- **Notes and expiry.** Give an entry a note and a lifespan, from minutes to months, or make
  it permanent.
- **Party awareness.** If someone on your list joins your group, you get a heads-up in chat and
  they're flagged in the party list and the Social window. Cross-world too.
- **Watch-only entries.** Add someone with nothing ticked and they're just flagged: shown in
  red, still triggering the party heads-up, but nothing hidden. (A community request.)
- **Safe in instances.** Listed players are never hidden in combat, duties or your own party,
  so you won't lose track of mechanics.
- **Bilingual.** English and German, following your Dalamud language and switching live.

## How it compares to Visibility
[Visibility](https://github.com/SheepGoMeh/VisibilityPlugin) and XIVIgnore aren't rivals; they
do different jobs. Visibility works by group: hide party, friends, free company or the dead,
plus pets and chocobos, and clean up your frame rate along the way (it also has a VoidList).
XIVIgnore works the other way round, one named person at a time. Want group-based hiding, or
pets and chocobos gone? That's Visibility's department, not this one.

If you run both, keep one thing in mind: they hide a character's model through the same game
flag, so pointing both at the same person can make the model flicker. Let just one of them
handle model-hiding for any given player.

## Adding someone
- Right-click them, choose *Add to virtual ignore list*, pick a duration.
- Open the window with `/xivignore`.
- Or `/xivignore add First Last@World`.

By default, adding someone opens a quick review window first: check or tweak what's about to be
ignored (filters, category, note, duration), then confirm, or close it to add nothing. Prefer
one-click adds? Turn off "Confirm before adding" in the settings.

## Commands
| Command | What it does |
|---|---|
| `/xivignore` | Open the window |
| `/xivignore add First Last@World` | Add a player |
| `/xivignore remove First Last@World` | Remove a player |
| `/xivignore list` | List entries |

## Installation
1. In Dalamud: Settings → Experimental → Custom Plugin Repositories.
2. Add this URL:
   ```
   https://raw.githubusercontent.com/VelvetFFXIV/DalamudPlugins/main/pluginmaster.json
   ```
3. Save, open the Plugin Installer, search for XIVIgnore, install.

## Privacy
The list stores only the character name and home world you already see in game. No ContentId,
no hidden account identifier, so it can't follow anyone through a rename or a world transfer.
It lives in a local file on your PC and never goes anywhere. Everything runs on your client:
no contact with the game server, no touching the real ignore list, no automation of any kind.

## Known limitations
Hiding a player's chat doesn't silence the tell sound yet. The text is gone, but the game still
plays the incoming-whisper sound. There's no clean way to mute it per player right now; I'd like
to solve that down the line.

## Source
Open source under the GNU AGPL-3.0 license. All of it is in this repository.

## Support
Found a bug, or want something added? [Open an issue.](https://github.com/VelvetFFXIV/XIVIgnore/issues)
