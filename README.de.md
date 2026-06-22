🇬🇧 [English](README.md) | 🇩🇪 Deutsch

# XIVIgnore

> Erweitert die Ignore-Liste virtuell — clientseitig, nur in deinem Client.

Die echte Ignore-Liste von FFXIV ist klein und serverseitig. **XIVIgnore** führt deine
eigene lokale „erweiterte Ignore-Liste" und filtert diese Spieler **in deinem Client**:
Chat, Party-Finder-Einträge, Namensschilder oder sogar das Charaktermodell ausblenden —
mit Kategorien, Notizen und automatischem Ablauf.

## Funktionen
- **Chat ausblenden** von gelisteten Spielern (pro Kanal einstellbar)
- **Party-Finder-Einträge ausblenden** von gelisteten Erstellern
- **Namensschilder ausblenden** gelisteter Spieler
- **Charaktermodell ausblenden** (experimentell, standardmäßig aus)
- **Kategorien** (Belästigung, Spam, Spoiler, RMT, Sonstiges) mit Standard-Wirkung + Eintrag-Override
- **Notizen** und **Auto-Ablauf** (Minuten → Monate, oder dauerhaft)
- **Party-Hinweis** — warnt im Chat und markiert einen gelisteten Spieler in der Partyliste
  und im Social-Fenster, wenn er in deine Gruppe kommt (auch Cross-World)
- **Nur-Beobachten-Einträge** — jemanden **ohne** angehakte Wirkung hinzufügen, um ihn nur zu
  *markieren*: weiterhin rot hervorgehoben + Party-Hinweis, aber nichts wird ausgeblendet oder
  gefiltert (von der Community gewünscht)
- **Sicherheit zuerst** — ignorierte Spieler bleiben **im Kampf, in Duties und in deiner
  Gruppe sichtbar**, damit du Mechaniken nie aus den Augen verlierst
- **Englische & deutsche UI**, folgt deiner Dalamud-Sprache (wechselt live)

## Keine Konkurrenz zu Visibility
XIVIgnore ist **kein** Konkurrent und **kein** Ersatz für
[Visibility](https://github.com/SheepGoMeh/VisibilityPlugin) — und erhebt **nicht** den Anspruch,
„besser" zu sein. Beide lösen unterschiedliche Probleme:

- **Visibility** entrümpelt die Sicht und hilft der Performance — Spieler, Pets und Chocobos
  *gruppenweise* ausblenden (Party, Freunde, Free Company, Tote), plus die VoidList. Großer
  Respekt an das Projekt.
- **XIVIgnore** ist ein *per-Spieler* Ignore-/Moderations-Tool — Chat, Party-Finder-Einträge und
  Namensschilder einer bestimmten Person ausblenden (optional das Modell), mit Kategorien,
  Notizen und Auto-Ablauf.

Du willst gruppenweises Ausblenden oder Pets/Chocobos verstecken? Dafür ist **Visibility** da —
das macht es gut, und XIVIgnore versucht das bewusst gar nicht erst.

**Wenn du beide nutzt:** Beide blenden das *Modell* über dasselbe Render-Flag des Spiels aus —
denselben Spieler in *beiden* auszublenden kann daher flackern. Lass fürs Modell-Ausblenden
nur **ein** Plugin den jeweiligen Spieler übernehmen. (XIVIgnores Modell-Ausblenden ist
experimentell und standardmäßig aus.)

## Spieler hinzufügen
- **Rechtsklick** auf einen Spieler → *Zur virtuellen Ignore-Liste* → Dauer wählen
- Das Plugin-Fenster (`/xivignore`)
- Slash-Befehl: `/xivignore add Vorname Nachname@Welt`

> **Hinweis:** Standardmäßig öffnet das Hinzufügen (Rechtsklick oder Befehl) zuerst ein
> **Prüf-Fenster** — kontrolliere oder ändere, was ignoriert wird (Wirkung, Kategorie, Notiz,
> Dauer) und bestätige es zum Hinzufügen oder schließe es, ohne etwas hinzuzufügen. Lieber sofort
> mit einem Klick? Schalte das unter **Einstellungen → „Vor dem Hinzufügen bestätigen"** ab.

## Befehle
| Befehl | Aktion |
|---|---|
| `/xivignore` | Fenster öffnen |
| `/xivignore add Vorname Nachname@Welt` | Spieler hinzufügen |
| `/xivignore remove Vorname Nachname@Welt` | Spieler entfernen |
| `/xivignore list` | Einträge auflisten |

## Installation
1. In Dalamud: **Einstellungen → Experimentell → Custom Plugin Repositories**
2. Diese URL hinzufügen:
   ```
   https://raw.githubusercontent.com/VelvetFFXIV/DalamudPlugins/main/pluginmaster.json
   ```
3. **Speichern** → Plugin-Installer öffnen → **XIVIgnore** suchen → **Installieren**

## Datenschutz & Fair Play
- **Keine Tracking-IDs.** Gelistete Spieler werden nur über den öffentlichen **Charakternamen +
  die Heimatwelt** geführt, die du ohnehin siehst — XIVIgnore speichert **keine ContentId** und
  keinen eindeutigen Charakter-/Account-Identifier und kann niemandem über Namensänderung oder
  Welttransfer folgen.
- Deine Liste liegt **lokal** auf deinem Rechner; nichts wird irgendwohin gesendet.
- Reines **clientseitiges Filtern** — es kommuniziert nicht mit dem Spielserver, ändert nicht
  die echte Ignore-Liste und automatisiert nichts.
- Ignorierte Spieler bleiben in Kampf-/Duty-/Gruppen-Inhalten voll sichtbar.

## Bekannte Einschränkungen
- **Chat-Sound wird noch nicht gefiltert** — wenn der Chat eines gelisteten Spielers
  ausgeblendet wird, spielt das Spiel den Tell-/Wisper-*Sound* trotzdem; nur der Text wird
  unterdrückt. Eine saubere Stummschaltung pro Spieler ist derzeit nicht möglich, wir haben es
  aber auf dem Schirm.

## Quellcode
XIVIgnore ist **Open Source** unter der **GNU AGPL-3.0**-Lizenz — der vollständige Quellcode
liegt in diesem Repository. Es ist ein rein clientseitiger Komfort-Filter — es automatisiert
nichts und kommuniziert nicht mit dem Spielserver.

## Support
Bug gefunden oder einen Wunsch? [Mach ein Issue auf](https://github.com/VelvetFFXIV/XIVIgnore/issues).
