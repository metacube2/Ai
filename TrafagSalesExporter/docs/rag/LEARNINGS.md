# Learnings: gemachte Fehler und die Regel daraus

Stand: 2026-10-01. Angelegt auf Wunsch von Ingo: Fehler an einer Stelle festhalten statt verstreut,
damit sie nicht wieder passieren.

**Pflicht:** vor jeder Aenderung, jedem SAP-Schritt und jedem Deploy diese Datei lesen. Wer einen
neuen Fehler macht oder einen alten wiederholt, traegt ihn hier ein, im selben Commit wie die
Korrektur (`CLAUDE.md` Abschnitt 3). Eine Zeile je Fehler: was passiert ist, die Regel, wo die
Einzelheiten stehen. Die Einzelheiten bleiben in der Fachdatei; hier steht nur der Verweis.

## Deploy und Server

| Datum | Was passiert ist | Regel | Detail |
| --- | --- | --- | --- |
| 2026-08-07 | `dotnet publish` aus Git Bash auf den UNC-Pfad legte alles lokal unter `C:\trch-webapp...` ab, der Server stand mit `app_offline` still | Deploy nur als Bash `dotnet run --project .tmp_tools/DeployHeadless -c Release`; nie selbst publishen | `rag/DEPLOYMENT.md` oben |
| 2026-08-07 | Publish meldete Erfolg, uebersprang aber die neuere `BiDashboard.dll` im Ziel | Nach jedem Deploy SHA256 Server gegen Release-Build vergleichen (macht der Runner) | `rag/DEPLOYMENT.md` |
| 2026-09-01 | Produktiver Stand ohne Commit, kein Rollback-Punkt | Erst committen, dann aus dem sauberen Release-Worktree am Commit deployen, nie aus dem Arbeitsbaum | `CLAUDE.md` 3 |
| 2026-08-10 | Admin-Schalter in `appsettings.json` war nach dem Deploy weg | Laufzeit-Einstellungen gehoeren in die DB, nicht in `appsettings.json` | `rag/DEPLOYMENT.md` |
| 2026-08-24 | Schreiben von aussen in die produktive SQLite war fuer die App unsichtbar (WAL) | Nie von aussen in die Produktiv-DB schreiben; Pruefkopie immer `.db` + `-wal` + `-shm` | `rag/DEPLOYMENT.md` |
| 2026-10-01 | Im selben Befehl stand ein zweiter `dotnet run ... DeployHeadless --no-build \| head -0`; das Protokoll des ersten Laufs ging durch einen `grep` verloren | Deploy-Befehl allein ausfuehren, nie mit `head`, `grep` oder einem zweiten Aufruf koppeln; Protokoll ungefiltert behalten | `rag/DEPLOYMENT.md` Kurzstand 11:21 |
| 2026-10-01 | Ein Neustart (Deploy) startet die Hintergrunddienste neu, z. B. den SAP-HR-Abruf nach 2 Minuten | Nach einem Deploy mit neuem Abruf das Ereignisprotokoll (`AppEventLogs`) pruefen; ein Neustart ist auch ein Weg, einen Tagesabruf vorzuziehen | `HR_KPI.md` 8.6/8.7 |
| 2026-10-01 | Der Absenzen-Abruf hing am Zeitstempel einer anderen Datei und waere erst am naechsten Morgen gelaufen | Jeder Tagesabruf bekommt seinen eigenen Faelligkeitsstempel | `c12b34a` |

## SAP

| Datum | Was passiert ist | Regel | Detail |
| --- | --- | --- | --- |
| 2026-09-10 | `Sessions=0` kostete zwei Stunden, Ursache lag am Server | Erst Server und Scripting-Engine pruefen, nicht den Arbeitsplatz | `saptasks/SAP_ARBEITSWEISE...` 0 |
| 2026-09-10 | Der vorgeschlagene Transportauftrag war der eines anderen, abzunehmenden Vorhabens | `KO008-TRKORR` nie blind uebernehmen; eigenen Auftrag anlegen (`btn[8]`) oder ausdruecklich setzen | SAP-Arbeitsweise, Abschnitt Transport |
| 2026-09-10 | Im Dialog „Inaktive Objekte“ standen fremde, unfertige Objekte | Nur ueber Namensabgleich markieren, nie ueber Zeilennummern | SAP-Arbeitsweise 3 |
| 2026-09-30 | `SapGuiStrukturFelder.vbs` ueberschrieb an der Seitengrenze die neunte Zeile (`TEILK`), die Struktur aktivierte trotzdem | Nach jedem SE11-Feldeintrag die Felder per DD03L zaehlen | SAP-Arbeitsweise SE11 |
| 2026-09-30 | `Start-Process` zerlegte ein Argument mit Leerzeichen | Argumente mit Leerzeichen ausdruecklich in Anfuehrungszeichen | `HR_KPI.md` 8.6 |
| 2026-09-30 | VBScript las eine Datei mit LF-Zeilenenden als eine Zeile | Hilfsdateien fuer VBS mit CRLF oder im Skript normalisieren | SAP-Arbeitsweise |
| 2026-09-30 | Ein freigegebener Customizing-Auftrag war in SE09/SE10 „unsichtbar“ (Mandant 090, freigegeben) | Auftraege ueber SE01 mit Nummer oder E070/E070C suchen | `SAP_ZRL2_MENGENRABATT_AT_2026-09-30.md` |
| 2026-10-01 | `SapProbe.exe` direkt aufgerufen fand kein Passwort (`SAP_PASSWORD` gesetzt) | Variable heisst `SAP_NCO_PASSWORD`; `RunSapProbe.ps1` zeigt Fehler nur als leere `NativeCommandError` | SAP-Arbeitsweise Transport |
| 2026-10-01 | `Remove-Item Env:...` in einem `finally` blockierte das Werkzeug | Umgebungsvariable mit `$env:X = $null` leeren | diese Datei |
| 2026-10-01 | Ungefiltertes `FinanzJournalSet` lief 154 s in einen 500er | OData-Gegenproben immer mit Filter; ein 500 ohne Filter ist kein Transportfehler | `HR_KPI.md` 8.6 |
| 2026-10-01 | Falsche Annahme, dass F6 in SE24 nach dem Auftrag fragt; die Abfrage kam erst beim Sichern | Nach jedem Sichern auf `wnd[1]` pruefen und den Auftrag dort setzen | SAP-Arbeitsweise Skripttabelle |

## Code, Tests, Werkzeuge

| Datum | Was passiert ist | Regel | Detail |
| --- | --- | --- | --- |
| 2026-09-30 | ClosedXML verweigerte eine temporaere Datei mit Endung `.tmp` | Temporaere Excel-Dateien `<name>.tmp.xlsx` nennen | `SapGatewayHrKpiReader.WriteWorkbook` |
| 2026-09-30 | `node -e` mit Backslashes oder einfachen Anfuehrungszeichen brach im Bash-Quoting | Groessere Ersetzungen als Skriptdatei im Scratchpad schreiben und mit `node <datei>` ausfuehren | diese Datei |
| 2026-10-01 | Testerwartung im Kopf gerechnet: der 02.01. ist in Zuerich kein Feiertag, 12,8 statt 16 h | Erwartungswerte mit Datumslogik aus dem Kalender ableiten, nicht schaetzen | `HrKpiServiceTests` |
| 2026-10-01 | Neuer Anzeigetext liess den Uebersetzungstest scheitern | Jeder neue `T(de, en)`-Text braucht Eintraege in allen Sprachbloecken von `UiTextGeneratedTranslations.cs` | `UiTextServiceTests` |
| 2026-10-01 | Eine neue Datenquelle aenderte die Zahl der `FileStatuses`, ein Konsistenztest schlug an | Bei neuer Quelldatei die Konsistenztests mitpflegen und den Grund im Test kommentieren | `HrKpiServiceTests` |
| 2026-10-01 | `Erledigt am` in Wochen_Todo hat gemischte Formate (`02.09.2026` und `2026-10-01 11:34`); ein neuer Auswerter brach mit `ValueError` ab | Beim Lesen beide Formate annehmen (`lies_datum` im Generator); neu nur noch `JJJJ-MM-TT HH:MM` schreiben (router.md Regel 10) | `wochen_todo_xlsx.py` |
| 2026-10-01 | Blatt „Zusammenfassung“ in `Wochen_Todo.xlsx` stand noch auf August (PM-02 „Vorfrage SE93“, PM-07 0 %), weil der Generator es bewusst nicht anfasst | Bei Statusaenderungen eines Vorhabens auch das Blatt „Zusammenfassung“ von Hand nachfuehren; nach dem Erzeugen TSV gegen Blatt „Wochen-Todo“ vergleichen | `wochen_todo_xlsx.py` Zeile 499 |
| 2026-09-29 | `python` ist nur ein Store-Platzhalter; Wochen_Todo.xlsx blieb mehrfach alt | uv-Cache-Python verwenden (`memory/reference_python_ohne_installation`); ist die Datei gesperrt, hat Ingo sie offen | `rag/init.md` |

## Arbeitsweise und Fachliches

| Datum | Was passiert ist | Regel | Detail |
| --- | --- | --- | --- |
| 2026-09-02 | UK galt als „ohne Lieferantenfelder“, Spanien-Datum war laengst entschieden, Doku sagte anderes | Doku vollstaendig nachfuehren, ueberholte Aussagen markieren | `CLAUDE.md` 3 |
| laufend | Lokale DB oder Dev-Server als Beleg genommen | Ingo testet nur produktiv; Belege nur aus Produktion | `router.md` 8 |
| laufend | Zu viele Annahmen bei unklarem Auftrag kosteten Mehraufwand | Bei fachlich offenen Punkten kurz fragen, mit Empfehlung | `persona.md` |
| 2026-09-30 | 8,4 h je Arbeitstag stand ungeprueft im Code | Fachkonstanten nur mit Quelle (Person, Datum) | `HR_KPI.md` 8.4 |
| 2026-10-01 | Erst nur „Felder in HrKpiSet“ als Option angeboten, obwohl ein Set je Fall besser passte | Vor einer Entscheidungsfrage alle sinnvollen Varianten durchdenken, nicht nur die naheliegende | `HR_KPI.md` 8.7 |
| 2026-10-01 | Ohne Datei wuerde ein Zeitraum vor den SAP-Daten eine leere, „verlaessliche“ 0-%-Quote zeigen | Bei jeder neuen Quelle pruefen, was ausserhalb ihrer Abdeckung angezeigt wird | `c12b34a` |
