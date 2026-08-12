# Deploy-Konsole fuer das BiDashboard

Stand: 2026-08-11

**Produktivnachtrag:** Der Runner wurde am 11.08.2026 erstmals produktiv eingesetzt,
kopflos ueber `.tmp_tools/DeployHeadless`. Ergebnis ohne Alarm: `461/461` Release-Tests,
Server-DLL bitgleich zum lokalen Build, sechs HTTPS-Routen mit Status 200, keine Datei
verschwunden und alle geschuetzten DB-/WAL-/SHM-/BAK-Dateien im Publishvergleich
unveraendert. Vorher wurde eine konsistente SQLite-Sicherung angelegt. Vollstaendiger
Nachweis: `docs/rag/DEPLOYMENT.md`.

## Warum

Der Produktivdeploy haengt an Merken und Disziplin. Zwei Fallen sind dokumentiert und
beide sind schon eingetreten:

- Publish ueber `/p:PublishProfile=FolderProfile` haette `DeleteExistingFiles=true`
  ausgeloest — im Zielverzeichnis liegen `trafag_exporter.db` (339 MB) und alle
  `.bak`-Sicherungen.
- Das Umbenennen von `app_offline.htm` scheiterte am 2026-08-07, weil
  `app_offline.htm.disabled` aus einem frueheren Deploy noch dalag.

`Tools/DeployConsole` schreibt den Ablauf einmal richtig fest und beweist hinterher,
dass nur Build-Ausgabe angefasst wurde.

## Starten

```powershell
dotnet build Tools\DeployConsole\DeployConsole.csproj
.\Tools\DeployConsole\bin\Debug\net8.0-windows\DeployConsole.exe
```

Die Einstellungen liegen als `deploy.settings.json` **neben der EXE** (aus dem
Projektordner mitkopiert): Ziel-UNC, Pfad der csproj, lokale Release-DLL fuer den
Bitvergleich, Name der Produktivdatenbank, Basis-URL und die abzurufenden Routen.
`BaseUrl` und `Routes` muessen gefuellt sein, sonst ist die Abrufpruefung wirkungslos
— das Protokoll schreibt dann „Erreichbarkeit ist damit NICHT belegt".

Beim Start sind **Prueflauf** und **Seiten abrufen** angehakt. Der erste Klick fuehrt
also lesende `GET`s gegen die Produktiv-URLs aus und schreibt nichts. Erst das
Abwaehlen von „Nur Prueflauf" faerbt den Knopf rot und verlangt eine Bestaetigung
mit dem Zielpfad.

## Was das Werkzeug tut

1. **Vorpruefung** — Branch, Commit, sauberes Arbeitsverzeichnis; Ziel erreichbar;
   Abbruch, wenn im Ziel kein `BiDashboard.dll` liegt (dann ist es nicht das
   Publish-Verzeichnis).
2. **Bestandsaufnahme** — jede Datei im Ziel mit Groesse und Schreibzeit, rekursiv.
   Die geschuetzten Muster (`*.db`, `*.db-wal`, `*.db-shm`, `*.bak`) werden einzeln
   protokolliert.
3. **`app_offline.htm`** — unmittelbar vor dem Publish gesetzt, unmittelbar danach
   entfernt. Eine alte `app_offline.htm.disabled` wird mit Groesse und Zeitstempel
   protokolliert, bevor sie weicht; das ist der einzige Loeschvorgang des Werkzeugs.
   Das Entfernen liegt in `finally` — auch ein gescheiterter Publish laesst die
   Anwendung nicht offline stehen.
4. **Publish** — fest verdrahtet als Argumentliste
   `publish <csproj> -c Release -o <Ziel> --nologo`, nie als Kommandozeilentext.
   `AssertSafeArguments` weist jedes Argument ab, das `publishprofile`, `pubxml`,
   `deleteexistingfiles`, `/p:` oder `-p:` enthaelt. Der gefaehrliche Weg ist nicht
   abgewaehlt, er ist nicht erreichbar.
5. **Nachweis** — zweite Bestandsaufnahme und Vergleich: verschwundene Dateien und
   veraenderte Schutzdateien sind Alarm. Dazu Groesse, Schreibzeit und SHA256 der
   ausgelieferten DLL, Vergleich mit dem lokalen Release-Build.
6. **Wirknachweis** — frei eingegebene Typnamen und Texte werden in der DLL gesucht,
   in beiden Kodierungen: Metadatennamen liegen UTF-8, Zeichenkettenliterale UTF-16
   im `#US`-Heap. Zweite Liste: was NICHT mehr enthalten sein darf.
7. **Abrufpruefung** — konfigurierte Routen mit Windows-Anmeldung, Status, Bytes,
   Dauer.
8. **Protokoll** — fertiger Absatz im Stil von `docs/rag/DEPLOYMENT.md`, per Knopf in
   der Zwischenablage.

Der Prueflauf-Schalter (Standard: an) macht alles ausser `app_offline` und Publish.

## Bewusst NICHT enthalten

**Der Testlauf.** Die Konsole fuehrt `dotnet test` nicht aus. Es gibt nur ein
Haekchen „Tests gruen" samt Anzahl, das der Mensch setzt und das so ins Protokoll
wandert; ohne Haekchen schreibt das Protokoll ausdruecklich „Testlauf NICHT
bestaetigt". Grund: 455 Tests im Release-Lauf sind eine gewachsene Gewohnheit, die
funktioniert — sie in eine Ausgabe-Parserei zu giessen, die bei jedem Wechsel des
Testrunners nachgezogen werden muss, kauft nichts.

## Zwei Befunde aus dem Bau

### 1. Die Arbeitsmappen im Ziel sind Build-Ausgabe, kein Datenbestand

`check.xlsx`, `zdispo_grp.xlsx` und `zdispo_spart.xlsx` liegen im Publish-Verzeichnis
und sehen aus wie gepflegte Daten. Sie stehen aber in `TrafagSalesExporter.csproj` mit
`CopyToPublishDirectory="Always"` — **jeder Publish ueberschreibt sie mit dem
Repository-Stand.** Gemessen im Prueflauf, nicht angenommen.

Folge: Wer eine dieser Dateien direkt auf dem Share bearbeitet, verliert die Aenderung
beim naechsten Deploy ohne jede Meldung. Aenderungen gehoeren ins Repository. Deshalb
stehen die XLSX bewusst **nicht** in `ProtectedPatterns` — sonst schlaege bei jedem
normalen Deploy Fehlalarm an.

### 2. Ein erfolgreicher Publish kann die Haupt-DLL stillschweigend ueberspringen

Beim Prueflauf blieb `BiDashboard.dll` im Ziel unveraendert, obwohl `dotnet publish`
Erfolg meldete und 115 andere Dateien schrieb. Ursache: die Hauptbaugruppe wird mit
**PreserveNewest** kopiert. Ist die Datei im Ziel **neuer** als der frisch gebaute
Stand, ueberspringt der Copy-Schritt sie kommentarlos — und die **alte Version laeuft
weiter**, waehrend der Deploy als gelungen dasteht.

Realistische Ausloeser: Uhr des Servers vor der Uhr des Entwicklungs-PCs, ein
angefasster Zeitstempel, ein halb durchgelaufener frueherer Deploy.

Die Konsole faengt das ab. Nach einem Publish ist die ausgelieferte DLL eine
Byte-Kopie des lokalen Release-Builds; weichen die SHA256 ab, ist das **kein**
Nicht-Determinismus zweier Builds, sondern ein nicht erfolgter Kopiervorgang. Das ist
jetzt ein Alarm mit Klartext statt der frueheren weichen Notiz „Build ist nicht
deterministisch" — genau die Formulierung haette diesen Fall verdeckt.

## Nachweis

`Tools/DeployConsole.Probe` baut einen Share im Scratch-Verzeichnis nach — Datenbank,
WAL/SHM, zwei `.bak`, die beiden XLSX, eine eigene Datei unter `wwwroot` und eine alte
`app_offline.htm.disabled` — und laesst einen **echten** `dotnet publish` dagegen
laufen. Nie gegen den Produktiv-Share.

Drei Szenarien, 25 Pruefungen, alle gruen (2026-08-07):

- **A, normaler Deploy:** Publish laeuft, DLL ersetzt und bitgleich mit dem lokalen
  Build (`4'320'768` Bytes), Datenbank byteidentisch (SHA256 unveraendert), alle
  Nicht-Build-Dateien unveraendert vorhanden, `app_offline` gesetzt und wieder
  entfernt, alte `.disabled` protokolliert entfernt, erwartete Typen und Literale
  gefunden (`ManagementCockpitService` und `FinanceCountryStatuses` UTF-8,
  `Nicht geprueft` UTF-16), verbotene Texte nicht enthalten, kein Alarm.
- **B, Ziel-DLL neuer als der Build:** Publish meldet Erfolg, die DLL bleibt
  nachweislich der 37-Byte-Platzhalter — und die Konsole schlaegt Alarm und schreibt
  es ins Protokoll, statt es durchgehen zu lassen.

- **C, das Fenster selbst:** `MainForm` wird gebaut, ausserhalb des Bildschirms
  angezeigt und der Startknopf wirklich geklickt (Prueflauf, Abruf aus). Fenster baut
  ohne Layoutfehler auf, der Lauf geht durch, das Protokollfeld ist gefuellt, der
  Kopierknopf schaltet frei.

Dazu: der Guard weist alle drei Profilvarianten ab, der Prueflauf schreibt
nachweislich nichts, und die Datenbank wird ueber ihren **Namen** bestimmt, nicht
ueber ein `*.db`-Muster — im Testziel liegt eine zweite, alphabetisch fruehere `.db`,
die den frueheren Mustertreffer auffliegen laesst.

`dotnet build` der Projektmappe erfolgreich, `dotnet test` unveraendert `455/455`
nach der Aufnahme beider Projekte in die .sln.

## Ausdruecklich NICHT belegt

- Der historische Probe-Lauf vom 07.08. lief nur gegen einen nachgebauten Share. Der erste
  Produktivlauf wurde am 11.08.2026 erfolgreich nachgeholt; Details stehen im Nachtrag oben
  und in `docs/rag/DEPLOYMENT.md`.
- Wie gehabt belegt ein HTTPS `200` nur Erreichbarkeit. Routen hinter dem
  Finance-Unlock liefern das Passwortpanel; das Protokoll sagt das selbst.
- Die Konsole prueft nicht, ob IIS den neuen Stand geladen hat, und startet den
  Anwendungspool nicht.
