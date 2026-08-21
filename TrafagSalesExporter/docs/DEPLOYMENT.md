# Deployment: Server, Publish, Deploy-Konsole und Fallen

Stand: 2026-08-17. Zusammengefuehrt aus `DEPLOYMENT_IIS_HANDOFF_2026-05-19.md`,
`DEPLOY_KONSOLE_2026-08-07.md`, `DEPLOY_GESAMTSTAND_2026-08-11.md` und
`LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md`.

**Der aktuell verifizierte Produktivstand steht in `docs/rag/DEPLOYMENT.md`.** Diese Datei
beschreibt Verfahren, Werkzeug und die bekannten Fallen — nicht den Tagesstand.

## 1. Server und Pfade

| Was | Wert |
| --- | --- |
| DNS | `trch-webapp-bidashboard.trafagch.local` (CNAME auf `tragvapp401.trafagch.local`, `10.120.1.17`) |
| Publish-Share | `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\` |
| URL | `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/` |
| Assembly | `BiDashboard.dll` |

`TrafagSalesExporter` ist das fuehrende Projekt und wird als `BiDashboard` veroeffentlicht.

**Betrieb unter `/BiDashboard`:** `web.config` setzt `ASPNETCORE_PATHBASE`, `Program.cs`
liest es und ruft `UsePathBase(...)`, `Components/App.razor` setzt `<base href>` dynamisch
(lokal `/`, Server `/BiDashboard/`).

**Wichtig fuer Fernzugriff:** Auf `tragvapp401` sind `Invoke-Command`, `schtasks` und `C$`
gesperrt, RDP gibt es nicht — der Share ist aber beschreibbar. Mit dem Aliasnamen
`trch-webapp-bidashboard` scheitert schon Kerberos. Deshalb laufen serverseitige Abfragen
ueber den Trigger-Mechanismus der laufenden Anwendung, siehe `docs/router/plattform.md`.

## 2. Veroeffentlichen

**Der empfohlene Weg ist die Deploy-Konsole** (Abschnitt 3). Manuell:

```powershell
dotnet publish .\TrafagSalesExporter.csproj -c Release -o <Ziel> --nologo
```

**Niemals `/p:PublishProfile=FolderProfile` gegen das Produktivziel.** Das Profil setzt
`DeleteExistingFiles=true`, und im Zielverzeichnis liegen `trafag_exporter.db` (rund 339 MB)
sowie alle `.bak`-Sicherungen.

## 3. Deploy-Konsole

```powershell
dotnet build Tools\DeployConsole\DeployConsole.csproj
.\Tools\DeployConsole\bin\Debug\net8.0-windows\DeployConsole.exe
```

Einstellungen als `deploy.settings.json` **neben der EXE**: Ziel-UNC, csproj-Pfad, lokale
Release-DLL fuer den Bitvergleich, Name der Produktivdatenbank, Basis-URL und Routen.
`BaseUrl` und `Routes` muessen gefuellt sein, sonst schreibt das Protokoll
„Erreichbarkeit ist damit NICHT belegt".

Beim Start sind **Prueflauf** und **Seiten abrufen** angehakt: der erste Klick fuehrt nur
lesende `GET`s aus und schreibt nichts. Erst das Abwaehlen von „Nur Prueflauf" faerbt den
Knopf rot und verlangt eine Bestaetigung mit dem Zielpfad.

Ablauf:

1. **Vorpruefung** — Branch, Commit, sauberes Arbeitsverzeichnis, Ziel erreichbar. Abbruch,
   wenn im Ziel kein `BiDashboard.dll` liegt.
2. **Bestandsaufnahme** — jede Datei mit Groesse und Schreibzeit, rekursiv. Geschuetzte
   Muster (`*.db`, `*.db-wal`, `*.db-shm`, `*.bak`) einzeln protokolliert.
3. **`app_offline.htm`** — unmittelbar vor dem Publish gesetzt, danach entfernt. Das
   Entfernen liegt in `finally`, damit ein gescheiterter Publish die Anwendung nicht
   offline stehen laesst.
4. **Publish** — fest verdrahtete Argumentliste, nie als Kommandozeilentext.
   `AssertSafeArguments` weist jedes Argument mit `publishprofile`, `pubxml`,
   `deleteexistingfiles`, `/p:` oder `-p:` ab. **Der gefaehrliche Weg ist nicht abgewaehlt,
   er ist nicht erreichbar.**
5. **Nachweis** — zweite Bestandsaufnahme; verschwundene Dateien und veraenderte
   Schutzdateien sind Alarm. Dazu SHA256 der DLL gegen den lokalen Release-Build.
6. **Wirknachweis** — Typnamen und Texte in der DLL suchen, in **beiden** Kodierungen:
   Metadatennamen UTF-8, Zeichenkettenliterale UTF-16 im `#US`-Heap. Zweite Liste: was
   nicht mehr enthalten sein darf.
7. **Abrufpruefung** — konfigurierte Routen mit Windows-Anmeldung, Status, Bytes, Dauer.
8. **Protokoll** — fertiger Absatz im Stil von `docs/rag/DEPLOYMENT.md`.

**Bewusst nicht enthalten: der Testlauf.** Es gibt nur ein Haekchen „Tests gruen" samt
Anzahl, das der Mensch setzt; ohne Haekchen schreibt das Protokoll „Testlauf NICHT
bestaetigt".

## 4. Die vier dokumentierten Fallen

### 4.1 Ein erfolgreicher Publish kann die Haupt-DLL still ueberspringen

Beobachtet: `BiDashboard.dll` im Ziel blieb unveraendert, obwohl `dotnet publish` Erfolg
meldete und 115 andere Dateien schrieb. Ursache ist **PreserveNewest** — ist die Datei im
Ziel neuer als der frisch gebaute Stand, ueberspringt der Copy-Schritt sie kommentarlos
und **die alte Version laeuft weiter**, waehrend der Deploy als gelungen dasteht.

Ausloeser: Serveruhr vor der Uhr des Entwicklungs-PCs, ein angefasster Zeitstempel, ein
halb durchgelaufener frueherer Deploy.

**Konsequenz:** Weicht der SHA256 der ausgelieferten DLL vom lokalen Release-Build ab, ist
das **kein** Nicht-Determinismus zweier Builds, sondern ein nicht erfolgter Kopiervorgang.
Die frueher uebliche weiche Notiz „Build ist nicht deterministisch" haette genau diesen
Fall verdeckt.

### 4.2 Ein SHA-Vergleich allein beweist die Wirkung nicht

Deshalb der Wirknachweis mit Vorher-Messung: ein Token muss **vorher fehlen und nachher
vorhanden** sein. Die Tokens aus dem echten `git diff` waehlen, nicht aus der
Dokumentation. Negativbeispiel: das Wort `Lokal` allein stand schon im alten Stand und
haette einen Treffer vorgetaeuscht, den der Deploy gar nicht erzeugt hat.

### 4.3 `app_offline.htm.disabled` blockiert das Umbenennen

Am 2026-08-07 scheiterte der Deploy, weil eine `app_offline.htm.disabled` aus einem
frueheren Lauf noch dalag. Die Konsole protokolliert sie mit Groesse und Zeitstempel,
bevor sie weicht — das ist ihr **einziger** Loeschvorgang.

### 4.4 Die Arbeitsmappen im Ziel sind Build-Ausgabe, kein Datenbestand

`check.xlsx`, `zdispo_grp.xlsx` und `zdispo_spart.xlsx` liegen im Publish-Verzeichnis und
sehen aus wie gepflegte Daten. Sie stehen aber mit `CopyToPublishDirectory="Always"` in der
csproj — **jeder Publish ueberschreibt sie mit dem Repository-Stand.** Wer eine davon
direkt auf dem Share bearbeitet, verliert die Aenderung ohne Meldung. Sie stehen bewusst
**nicht** in `ProtectedPatterns`, sonst gaebe es bei jedem Deploy Fehlalarm.

## 5. Ablauf eines Produktivdeploys

1. Release-Tests lokal laufen lassen, Anzahl notieren.
2. Konsistente Vorher-Sicherung der Produktivdatenbank ueber die SQLite-`BackupDatabase`-API
   anlegen (`trafag_exporter.db.before-<thema>-<zeitstempel>.bak`) — **aber nur, wenn nicht
   schon eine passende Sicherung existiert, siehe Abschnitt 5a.**
3. Deploy-Konsole im Prueflauf starten, Bestandsaufnahme pruefen.
4. Prueflauf abwaehlen, Zielpfad bestaetigen, Publish.
5. Nachweis pruefen: keine verschwundenen Dateien, Schutzdateien unveraendert, DLL bitgleich.
6. Wirknachweis mit Vorher-/Nachher-Tokens.
7. Routen abrufen, Status `200` erwarten.
8. Protokollabsatz in `docs/rag/DEPLOYMENT.md` und `lastchange.md` uebernehmen,
   `docs/AGENT_COORDINATION.md` aktualisieren.

**Was ein HTTP-`200` nicht belegt:** Routen hinter dem Finance- oder HR-Unlock liefern von
der Entwicklungsmaschine aus das Passwortpanel. Der `200` belegt Erreichbarkeit, nicht die
Anzeige. Dafuer braucht es einen angemeldeten Sichtprueflauf.

## 5a. Keine redundante Sicherung anlegen — VERBINDLICH

**Regel: Vor dem Anlegen einer Sicherung pruefen, ob auf der Freigabe schon eine `.bak`
liegt, deren Groesse UND Schreibzeit zur aktuellen `trafag_exporter.db` passen. Wenn ja,
diese als Vorher-Nachweis verwenden und KEINE neue anlegen.** Im Protokollabsatz dann die
wiederverwendete Datei nennen, damit der Nachweis vollstaendig bleibt.

Begruendung, gemessen am 2026-08-21: An diesem Tag entstanden **drei Sicherungen derselben
unveraenderten Datenbank** (`10:12`, `10:46`, `13:01`), weil drei Deploys hintereinander
liefen und keiner die Datenbank anfasste — drei inhaltsgleiche Dateien, jede voll bezahlt.

### Warum eine Sicherung direkt auf die Freigabe so lange dauert

**Gemessen am 2026-08-21 am laufenden Deploy: `145 KB/s`.** In 40 Sekunden wuchs die
Sicherung um `5,9 MB` (`188,6` auf `194,5 MB`). Fuer die vollen `353 MB` sind das
**gegen 40 Minuten**, nicht die zuvor geschaetzten zehn.

Die Ursache ist **Latenz, nicht Bandbreite**. Die `BackupDatabase`-API kopiert seitenweise,
und jede Seite ist ein eigener Schreibvorgang mit eigener SMB-Rundreise. Das Netz ist dabei
nicht ausgelastet.

**Loesung, umgesetzt in `DatabaseBackup`:** zuerst lokal ins Temp-Verzeichnis sichern, dann
die fertige Datei in einem Zug auf die Freigabe kopieren. Lokal entfaellt die Rundreise je
Seite, und der abschliessende `File.Copy` uebertraegt in grossen Bloecken mit
Netzgeschwindigkeit.

**Die Konsistenz bleibt dabei voll erhalten**, denn sie stammt aus der `BackupDatabase`-API
selbst und haengt nicht davon ab, wohin geschrieben wird. **Ein reiner `File.Copy` der
LIVE-Datenbank waere dagegen unzulaessig** — paralleles Schreiben und der WAL-Zustand
koennten eine zerrissene Kopie ergeben. Deshalb bleibt die API der erste Schritt und der
Bulk-Copy nur der zweite.

**NOCH NICHT GEMESSEN:** wie schnell die neue Fassung tatsaechlich ist. Die `145 KB/s` sind
belegt, der Gewinn durch das lokale Zwischenspeichern ist bisher nur begruendet. Beim
naechsten Deploy nachmessen und die Zahl hier eintragen.

Der Effekt verschaerft sich mit der Zeit, weil die Datenbank waechst: **Juni 234 MB, am
2026-08-21 bereits 353 MB**, also plus 51 Prozent. Die Sicherungsdauer skaliert direkt
damit. Wer den Schritt unveraendert laesst, wartet in einem halben Jahr entsprechend
laenger.

Umgesetzt in `Tools/DeployConsole` (`DatabaseBackup.EnsureBackup`), damit die Regel fuer
jeden Weg gilt und nicht in jedem Wegwerf-Werkzeug unter `.tmp_tools/` neu erfunden wird.

**Wann eine neue Sicherung trotzdem zwingend ist:** sobald die Datenbank seit der letzten
Sicherung geschrieben wurde, also bei Schemaaenderungen, Seed-Aenderungen, Migrationen und
nach jedem Import oder Lauf, der Daten schreibt. Genau das erkennt der Vergleich von Groesse
und Schreibzeit automatisch — weicht eines davon ab, wird gesichert.

### Zwei weitere Zeitfresser

- **Routen sparsam pruefen.** `/einkauf` braucht allein rund `90 s`, `/einkauf/aufriss` je
  nach Cachezustand bis `88 s`. Diese Routen nur in die Abrufpruefung nehmen, wenn die
  Aenderung den Einkauf betrifft. Startseite und `/management-cockpit` antworten in
  Bruchteilen einer Sekunde.
- **Alte Sicherungen aufraeumen.** Am 2026-08-21 lagen **38 `.bak`-Dateien mit 7,8 GB** auf
  der Freigabe, die aelteste vom 5. Juni; die Freigabe insgesamt 1585 Dateien und 13,5 GB.
  Aufbewahrung: die letzten fuenf plus je eine pro Monat genuegen. **Loeschen nur nach
  ausdruecklicher Freigabe durch Ingo und nur von ihm selbst ausgefuehrt**, nie beilaeufig
  im Rahmen eines Deploys.

## 6. Firewall

Der **Webserver** muss die Ziele erreichen, nicht der Entwickler-PC:

| Ziel | Adresse |
| --- | --- |
| BI1/HANA intern | `10.194.65.22:30015` |
| India HANA | `20.197.20.60:30015` |
| SAP OData / ZSCHWEIZ | `10.194.64.29:8000` |
| SharePoint / Microsoft 365 | ausgehend HTTPS 443 |

## 7. Historisch: lokaler Uebergangsserver

Bis zur Freigabe des zentralen IIS lief die Anwendung als Notbetrieb auf dem
Entwicklungs-PC, erreichbar im internen Netz ueber eine Firewallregel auf dem
Kestrel-Port. **Das ist nicht mehr der Produktionsweg** und darf nicht als solcher
verwendet werden. Der Abschnitt bleibt nur, damit alte Verweise darauf einzuordnen sind.

## Querverweise

- Aktuell verifizierter Produktivstand: `docs/rag/DEPLOYMENT.md`
- Serverseitige Abfragen ohne RDP: `docs/router/plattform.md`
- Architektur: `docs/rag/ARCHITECTURE.md`
