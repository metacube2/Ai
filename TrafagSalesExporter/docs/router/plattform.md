# Unterrouter Plattform

Zurueck: `router.md`. Stand: 2026-08-17.

Architektur, Deployment, Admin, Requirements, Werkzeuge, Serveranalyse.

## Dateien

| Bedarf | Datei |
| --- | --- |
| **Aktuell verifizierter Produktivstand** | `docs/rag/DEPLOYMENT.md` |
| **Verfahren, Deploy-Konsole, die vier Fallen** | `docs/DEPLOYMENT.md` |
| Architektur, Kurzstand | `docs/rag/ARCHITECTURE.md` |
| Admin-Bereich, Kurzstand | `docs/rag/ADMIN.md` |
| Admin-Bereich und Startseite | `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` |
| Zusammenfuehrung der Admin-Menues | `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md` |
| Gesamtfunktionalitaet, reverse-engineered | `docs/REQUIREMENTS.md` |
| Diagramme und technische Einordnung | `docs/PROGRAMM_DIAGRAMME.md` |
| Projektstand, Kurzstand | `docs/rag/PROJECT.md` |
| Pausenspiel (Nebenfeature, Reiter ausgeblendet) | `docs/PAUSENSPIEL.md` |
| ccusage installieren und nutzen | `docs/CCUSAGE_INSTALL_ANLEITUNG.md` |

## Live-Werkzeuge

### SAP ERP: SapProbe

Ort `.tmp_sap_probe/`, Start ueber `.tmp_sap_probe/RunSapProbeInteractive.ps1 <befehl>`.
Default `travt762.sap.trafag.com`, SID `T76`, Client 100 — Produktion `travp762` nur
bewusst per `--ashost`. Passwort interaktiv oder ueber `SAP_NCO_PASSWORD`, **nie** in Doku
oder Git.

| Befehl | Zweck |
| --- | --- |
| `system-info` | Verbindung und System pruefen |
| `table-read` | Tabelleninhalte lesen |
| `table-fields`, `field-exists` | DDIC-Felder und Datenelemente pruefen |
| `function-info`, `function-search`, `rfc-call` | RFC-Bausteine untersuchen und aufrufen |
| `abap-read`, `abap-check` | ABAP lesen und im System syntaxpruefen |
| `abap-write`, `abap-activate` | schreiben und aktivieren, nur mit `--confirm-write` |

Grenzen: DDIC-Strukturen bleiben manuell in SE11, globale Klassen in SE24/ADT,
Gateway-Modell und EntitySets in SEGW. SapProbe verifiziert SAP-Fakten, ersetzt diese
Oberflaechen aber nicht.

### SAP B1/HANA: HanaQ

Lokaler Helfer `.tmp_tools/HanaQ/`, braucht den SAP-HANA-.NET-Client. Aufruf
`HanaQ.exe <TSC> <sqlFile> [dbPath]`. Verbindung, Schema und Credentials werden aus
`Sites`, `SourceSystemDefinitions` und `HanaServers` der lokalen SQLite-Kopie aufgeloest —
**keine Passwoerter in SQL-Dateien**. Guardrail: nur `SELECT`/`WITH`, Platzhalter `{schema}`.

Zwei Messfallen:

- Prozentangaben immer auf die fachliche Grundgesamtheit filtern, etwa aktive Lagerartikel
  statt aller `OITM`-Zeilen.
- `LIKE 'U_%'` matcht wegen des Platzhalter-Unterstrichs auch `UserSign`. Fuer UDF-Spalten
  `LIKE 'U\_%' ESCAPE '\'` schreiben. Schemavergleiche in `SYS.TABLE_COLUMNS`
  case-insensitiv, weil Schemanamen je Standort unterschiedlich geschrieben sind
  (`TRAFAG_LIVE` gegen `it01_p`).

### Standorte, die nur der Server erreicht: Serveranalyse

Zweck: lesende Abfragen gegen Systeme, die der Entwicklungsrechner nicht erreicht, etwa
Indiens HANA `20.197.20.60:30015`.

**Ausgefuehrt wird von der laufenden Anwendung**, nicht von einem Werkzeug auf dem Server:
`Services/ServerAnalysisBackgroundService.cs` prueft alle 20 Sekunden, ob im
Anwendungsordner `_analysis/run.trigger` liegt, arbeitet dann `_analysis/sql/*.sql` ab und
schreibt nach `_analysis/results`.

Grund fuer diesen Umweg: Auf `tragvapp401` sind `Invoke-Command`, `schtasks` und `C$`
gesperrt und es gibt keinen RDP-Zugang; der Share ist aber beschreibbar. Mit dem
Aliasnamen `trch-webapp-bidashboard` scheitert schon Kerberos.

Fernbedienung: `docs/analyse/Run-ServerAnalysis.ps1 -Action Run | Fetch | Clean`.
Abfragen in `docs/analyse/sql/`, Belege in `docs/analyse/ergebnisse/`.

Regeln fuer SQL-Dateien: Dateiname beginnt mit dem TSC (`TRIN__01_...`), Statementtrenner
ist eine Zeile ab `;;`, nur `SELECT`/`WITH` (`Services/ReadOnlySqlGuard.cs`), Platzhalter
`{schema}`, maximal 500 Zeilen je Statement. **Zwei Bindestriche als Zeichenkettenliteral
sind nicht moeglich** — sie gelten als Kommentar und der Guardrail lehnt ab.

## Fallen in diesem Ast

Die vier Deploy-Fallen stehen ausfuehrlich in `docs/DEPLOYMENT.md` Abschnitt 4. Kurz:

- Ein erfolgreicher Publish kann die Haupt-DLL wegen **PreserveNewest** still
  ueberspringen. Weicht der SHA256 ab, ist das kein Nicht-Determinismus, sondern ein nicht
  erfolgter Kopiervorgang.
- Ein SHA-Vergleich allein beweist die Wirkung nicht — Tokens brauchen eine
  Vorher-Messung.
- `/p:PublishProfile=FolderProfile` setzt `DeleteExistingFiles=true` und wuerde die
  Produktivdatenbank im Zielordner treffen.
- Die XLSX im Publish-Ordner sind Build-Ausgabe und werden bei jedem Deploy ueberschrieben.

## Querverweise in Nachbaraeste

- ABAP und SAP-Objekte: `docs/router/sap.md`
- Agentenkoordination vor einem Deploy: `docs/router/projekt.md`
