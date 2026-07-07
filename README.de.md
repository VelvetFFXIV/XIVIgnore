🇬🇧 [English](README.md) | 🇩🇪 Deutsch

# XIVIgnore

> Eine größere Ignore-Liste für FFXIV, lokal auf deinem Rechner.

Die eingebaute Ignore-Liste von FFXIV ist kurz und liegt auf dem Server. XIVIgnore führt
lokal eine längere und filtert die Leute darauf still im Hintergrund: ihren Chat, ihre
Party-Finder-Einträge, ihre Namensschilder und auf Wunsch ihr Charaktermodell. Einträge
lassen sich in Kategorien sortieren, mit einer Notiz versehen und laufen von selbst wieder ab.

## Funktionen
- **Chat ausblenden** von gelisteten Spielern, pro Kanal einstellbar.
- **Party-Finder-Einträge ausblenden.**
- **Namensschilder ausblenden.**
- **Charaktermodell ausblenden** (experimentell, standardmäßig aus).
- **Kategorien** (Belästigung, Spam, Spoiler, RMT, Sonstiges): jede mit einer Standard-Wirkung,
  die du pro Person überschreiben kannst.
- **Notizen und Ablauf.** Gib einem Eintrag eine Notiz und eine Laufzeit, von Minuten bis
  Monaten, oder lass ihn dauerhaft stehen.
- **Party-Hinweis.** Kommt jemand von deiner Liste in deine Gruppe, bekommst du eine Notiz im
  Chat und die Person wird in der Partyliste und im Social-Fenster markiert. Auch Cross-World.
- **Nur-Beobachten-Einträge.** Füge jemanden ohne angehakte Wirkung hinzu, dann ist er bloß
  markiert: rot hervorgehoben, löst weiterhin den Party-Hinweis aus, aber nichts wird
  ausgeblendet. (Von der Community gewünscht.)
- **Sicher in Instanzen.** Gelistete Spieler werden im Kampf, in Duties und in deiner eigenen
  Gruppe nie ausgeblendet, damit dir keine Mechanik abhandenkommt.
- **Zweisprachig.** Deutsch und Englisch, folgt deiner Dalamud-Sprache und wechselt im
  laufenden Betrieb.

## Wie es sich zu Visibility verhält
[Visibility](https://github.com/SheepGoMeh/VisibilityPlugin) und XIVIgnore sind keine
Konkurrenten; sie machen verschiedene Dinge. Visibility arbeitet gruppenweise: Party, Freunde,
Free Company oder Tote ausblenden, dazu Pets und Chocobos, und nebenbei die Framerate entlasten
(es hat außerdem eine VoidList). XIVIgnore geht den umgekehrten Weg, immer eine namentlich
genannte Person. Du willst gruppenweises Ausblenden oder Pets und Chocobos loswerden? Das ist
Visibilitys Revier, nicht dieses hier.

Wenn du beide nutzt, denk an eines: Beide blenden ein Modell über dasselbe Spiel-Flag aus,
dieselbe Person in beiden anzusteuern kann das Modell also flackern lassen. Lass fürs
Modell-Ausblenden einfach eines von beiden den jeweiligen Spieler übernehmen.

## Jemanden hinzufügen
- Rechtsklick auf die Person, *Zur virtuellen Ignore-Liste*, Dauer wählen.
- Das Fenster mit `/xivignore` öffnen.
- Oder `/xivignore add Vorname Nachname@Welt`.

Standardmäßig öffnet sich beim Hinzufügen erst ein kurzes Prüf-Fenster: schau dir an, was gleich
ignoriert wird (Wirkung, Kategorie, Notiz, Dauer), und bestätige es, oder schließ es, ohne etwas
hinzuzufügen. Lieber mit einem Klick? Schalte „Vor dem Hinzufügen bestätigen" in den
Einstellungen ab.

## Befehle
| Befehl | Wirkung |
|---|---|
| `/xivignore` | Fenster öffnen |
| `/xivignore add Vorname Nachname@Welt` | Spieler hinzufügen |
| `/xivignore remove Vorname Nachname@Welt` | Spieler entfernen |
| `/xivignore list` | Einträge auflisten |

## Installation
1. In Dalamud: Einstellungen → Experimentell → Custom Plugin Repositories.
2. Diese URL hinzufügen:
   ```
   https://raw.githubusercontent.com/VelvetFFXIV/DalamudPlugins/main/pluginmaster.json
   ```
3. Speichern, Plugin-Installer öffnen, nach XIVIgnore suchen, installieren.

## Datenschutz
Die Liste speichert nur den Charakternamen und die Heimatwelt, die du im Spiel ohnehin siehst.
Keine ContentId, kein verstecktes Account-Kennzeichen, also kann sie niemandem über eine
Namensänderung oder einen Welttransfer folgen. Sie liegt in einer lokalen Datei auf deinem PC
und verlässt ihn nie. Alles läuft auf deinem Client: kein Kontakt zum Spielserver, kein Eingriff
in die echte Ignore-Liste, keinerlei Automatisierung.

## Bekannte Einschränkungen
Wer den Chat einer gelisteten Person ausblendet, hört trotzdem noch den Tell-Sound. Der Text ist
weg, aber das Spiel spielt den Wisper-Ton weiterhin ab. Sauber pro Person stummschalten lässt
sich das aktuell nicht; ich würde das gern noch lösen.

## Quellcode
Open Source unter der GNU AGPL-3.0-Lizenz. Alles liegt in diesem Repository.

## Support
Bug gefunden oder einen Wunsch? [Mach ein Issue auf.](https://github.com/VelvetFFXIV/XIVIgnore/issues)
