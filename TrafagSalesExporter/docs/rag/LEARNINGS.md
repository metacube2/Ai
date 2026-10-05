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
| 2026-10-05 | Deploys brachen dreimal in der DB-Sicherung mit SQLite `disk I/O error` ab; Ursache Always On VPN (SSTP, RAS-Code 829), Heimnetz stabil | Deploy erst starten, wenn die Freigabe 2 Minuten am Stueck erreichbar ist (Warteschleife im Hintergrund); leere `.bak` in Temp danach loeschen | `docs/rag/DEPLOYMENT.md` |
| 2026-10-05 | Ein fertiger Deploy-Lauf (Protokoll geschrieben) beendete sich nicht und sperrte `DeployConsole.dll`; Folgedeploys scheiterten beim Bauen | Nach Abbruch „konnte nicht kopiert werden“ zuerst `Get-Process DeployHeadless` pruefen; Ingo musste den Prozess beenden | `docs/rag/DEPLOYMENT.md` |
| 2026-08-07 | `dotnet publish` aus Git Bash auf den UNC-Pfad legte alles lokal unter `C:\trch-webapp...` ab, der Server stand mit `app_offline` still | Deploy nur als Bash `dotnet run --project .tmp_tools/DeployHeadless -c Release`; nie selbst publishen | `rag/DEPLOYMENT.md` oben |
| 2026-08-07 | Publish meldete Erfolg, uebersprang aber die neuere `BiDashboard.dll` im Ziel | Nach jedem Deploy SHA256 Server gegen Release-Build vergleichen (macht der Runner) | `rag/DEPLOYMENT.md` |
| 2026-09-01 | Produktiver Stand ohne Commit, kein Rollback-Punkt | Erst committen, dann aus dem sauberen Release-Worktree am Commit deployen, nie aus dem Arbeitsbaum | `CLAUDE.md` 3 |
| 2026-08-10 | Admin-Schalter in `appsettings.json` war nach dem Deploy weg | Laufzeit-Einstellungen gehoeren in die DB, nicht in `appsettings.json` | `rag/DEPLOYMENT.md` |
| 2026-08-24 | Schreiben von aussen in die produktive SQLite war fuer die App unsichtbar (WAL) | Nie von aussen in die Produktiv-DB schreiben; Pruefkopie immer `.db` + `-wal` + `-shm` | `rag/DEPLOYMENT.md` |
| 2026-10-01 | Im selben Befehl stand ein zweiter `dotnet run ... DeployHeadless --no-build \| head -0`; das Protokoll des ersten Laufs ging durch einen `grep` verloren | Deploy-Befehl allein ausfuehren, nie mit `head`, `grep` oder einem zweiten Aufruf koppeln; Protokoll ungefiltert behalten | `rag/DEPLOYMENT.md` Kurzstand 11:21 |
| 2026-10-01 | Deploy-Runner endet mit Exit 1 und „2 verschwunden“ | Das sind `trafag_exporter.db-wal`/`-shm`, die SQLite beim Neustart neu anlegt (am 01.10. 14:14 im ungefilterten Protokoll belegt); pruefen, dass beide wieder da sind, dann ist es kein Fehler | `rag/DEPLOYMENT.md` Kurzstand 14:14 |
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
| 2026-10-01 | Git Bash machte aus dem Methodennamen `/IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET` einen Pfad `C:/PROGRAM FILES/GIT/IWBEP/...` | SAP-Skripte mit Argumenten, die mit `/` beginnen, ueber PowerShell aufrufen, nie ueber Bash | SAP-Arbeitsweise „Bash verstuemmelt“ |
| 2026-10-01 | Ein neuer Datumsschutz (hoechstens 7 Tage zurueck) machte die Sets in T76 untestbar, weil die Testkopie im Maerz endet | Schutzgrenzen so waehlen, dass T76 testbar bleibt; die eigentliche Last-Grenze ist "genau ein Tag", nicht das Alter | `LOGISTIK_LIVE_2026-10-01.md` |
| 2026-10-01 | Falsche Annahme, dass F6 in SE24 nach dem Auftrag fragt; die Abfrage kam erst beim Sichern | Nach jedem Sichern auf `wnd[1]` pruefen und den Auftrag dort setzen | SAP-Arbeitsweise Skripttabelle |

## Code, Tests, Werkzeuge

| Datum | Was passiert ist | Regel | Detail |
| --- | --- | --- | --- |
| 2026-09-30 | ClosedXML verweigerte eine temporaere Datei mit Endung `.tmp` | Temporaere Excel-Dateien `<name>.tmp.xlsx` nennen | `SapGatewayHrKpiReader.WriteWorkbook` |
| 2026-09-30 | `node -e` mit Backslashes oder einfachen Anfuehrungszeichen brach im Bash-Quoting | Groessere Ersetzungen als Skriptdatei im Scratchpad schreiben und mit `node <datei>` ausfuehren. **Am 2026-10-01 dreimal wiederholt** (Apostroph in `1'796`, Regex mit Backslash): auch kleine `node -e` mit Anfuehrungszeichen, Apostroph oder Backslash gehoeren in eine Datei | diese Datei |
| 2026-10-01 | Testerwartung im Kopf gerechnet: der 02.01. ist in Zuerich kein Feiertag, 12,8 statt 16 h | Erwartungswerte mit Datumslogik aus dem Kalender ableiten, nicht schaetzen | `HrKpiServiceTests` |
| 2026-10-01 | Neuer Anzeigetext liess den Uebersetzungstest scheitern | Jeder neue `T(de, en)`-Text braucht Eintraege in allen Sprachbloecken von `UiTextGeneratedTranslations.cs` **und** in `UiTextBerneseTranslations.cs` (seit 14:26) | `UiTextServiceTests` |
| 2026-10-01 | Der Uebersetzungstest hielt `AddAttribute(seq++, "class", "bom-node")` fuer ein Text-Paar (zwei benachbarte Zeichenketten) und verlangte "class" in allen Sprachen | In Razor-Code keine zwei Zeichenketten-Literale nebeneinander, die keine Texte sind; als Konstanten auslagern | `BomUsageRisk.razor` |
| 2026-10-01 | Offene Einteilungen ohne Datumsgrenze ergaben 41'434 "ueberfaellige" Positionen bis 2002 zurueck | Bei "offen/ueberfaellig" aus dem Einkaufscache immer ein Zeitfenster setzen (hier 12 Monate) und die Zahl plausibilisieren, bevor sie angezeigt wird | `LOGISTIK_STUECKLISTE_VERWENDUNG_RISIKO_2026-10-01.md` |
| 2026-10-01 | Nicht vorher in `AGENT_COORDINATION.md` eingetragen, bevor Verwendung & Risiko gebaut wurde | Auch bei schnellen Folgeauftraegen zuerst eintragen | `CLAUDE.md` 2 |
| 2026-10-02 | SVG-`<text>` direkt in einer `@for`/`@foreach`-Schleife bricht den Razor-Build (RZ1023, Razor haelt es fuer sein `<text>`-Schluesselwort). **Gleichentags wiederholt** in einem `@if`-Block | SVG-`<text>` direkt in **jedem** Codeblock (`@for`, `@foreach`, `@if`) in ein `<g>` einpacken | `NetworkExports.razor`, `NetworkAd.razor` |
| 2026-10-02 | Die inneren Reiter des AD-Reiters waren unsichtbar: Edge blendet Klassen mit Praefix `ad-` per eingebauter Werbefilter-Regel aus (`.ad-tabs { display: none !important }`, Herkunft user-agent) | Nie CSS-Klassen oder IDs mit `ad-`, `ads-`, `advert` o. ae. beginnen lassen; bei „unsichtbar ohne Regel“ die Kaskade per DevTools `CSS.getMatchedStylesForNode` pruefen | `NETZWERK_2026-10-02.md` |
| 2026-10-02 | Der Uebersetzungstest las `string.Join(", ", ...)` in Razor als Textpaar und schlug fehl | In Razor keine `string.Join(", ", ...)`; Hilfsmethode `NetSvg.Csv(...)` | `NetSvg.cs` |
| 2026-10-02 | RZ1023 zum dritten Mal (SVG-`<text>` in `@if` der Geraetelandkarte) und zweimal benachbarte Zeichenketten in Razor (`Replace("Windows ", "Win ")`, `InvokeVoidAsync("...", "net-report")`) trotz bestehender Regeln | Vor dem Build neuer Razor-Seiten gezielt suchen: `<text` innerhalb von `@if`/`@for`/`@foreach` und `", "` zwischen zwei Literalen | `NetworkAdInfra.razor`, `NetworkMigration.razor`, `NetworkReport.razor` |
| 2026-10-02 | Verkauf zeigte +57 % zum Vorjahr, weil die Daten erst 01.2025 beginnen und die „12 Monate davor“ nur 9 Monate enthielten | Vor jedem Zeitvergleich den Datenbeginn je Quelle pruefen; nur gleiche Zeitraeume vergleichen, die ganz in den Daten liegen, und den Zeitraum anschreiben | `VERKAUF_2026-10-02.md` |
| 2026-10-02 | SVG-Attribut `text-anchor="end"` wirkte nicht, weil die Klasse `nw-axis` `text-anchor: middle` setzt (CSS schlaegt Praesentationsattribut) | Ausrichtung bei Klassen mit eigener Ausrichtung als `style="text-anchor:..."` setzen | `SalesCrossSell.razor`, `NetworkTrend.razor` |
| 2026-10-02 | Razor-Seite `SalesConcentration.razor` hiess gleich wie der Record `SalesConcentration` (ebenso `SalesDecline`); der Build fand die Record-Member nicht mehr (47 Fehler) | Records und Seiten nie gleich benennen; Records mit Endung (`...Result`, `...Item`) | `Services/Sales/SalesModels.cs` |
| 2026-10-02 | Heredoc mit CSS in Bash brach mit „unexpected EOF“ ab | Groessere CSS-Bloecke als Datei im Scratchpad schreiben und mit `cat >>` anhaengen | diese Datei |
| 2026-10-02 | Leerer Messwert als 0 % und „543 ohne LAPS“ angezeigt, obwohl kein einziger Wert lesbar war | Ist ein Attribut bei keinem Objekt lesbar, „nicht lesbar“ anzeigen statt einer Alarmzahl | `NetworkAd.razor` |
| 2026-10-02 | Korrekturrunde am AD-Reiter wieder ohne vorherigen Eintrag in `AGENT_COORDINATION.md` begonnen (Wiederholung vom 01.10.) | Auch nach einem Bild mit Fehler zuerst eintragen, dann suchen | `CLAUDE.md` 2 |
| 2026-10-02 | Ein Anzeigetext sagte „seit heute“ und waere ab morgen falsch gewesen | Keine relativen Zeitangaben in festen Texten | `NetworkAd.razor` |
| 2026-10-02 | Deploy brach mit „Produktiv-DB fehlt“ ab: der Laptop war im Heim-WLAN ohne Firmennetz | Vor dem Deploy pruefen, dass die Freigabe erreichbar ist; die Meldung heisst dann nicht, dass die DB weg ist | `rag/DEPLOYMENT.md` |
| 2026-10-02 | Ein Werkzeug, das App-Dienste mit Hosting-Typen nutzt, startete nicht (Hosting.Abstractions fehlt) | Pruefwerkzeuge unter `.tmp_tools` mit `Microsoft.NET.Sdk.Web` anlegen, wenn sie die App-DLL laden | `.tmp_tools/NetProbe` |
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
| 2026-10-05 | Weltlage: Kursveraenderung 90 Tage aus der Kurstabelle gerechnet, die nur bei Bedarf importiert wird; alle Waehrungen 0.0 | Vor einer Zeitreihen-Rechnung pruefen, ob die Quelle ueberhaupt Verlauf hat (Fuellstand, Import-Takt), nicht nur, ob sie Werte liefert | `WELTLAGE_2026-10-05.md` Produktiver Stand |
| 2026-10-05 | Logistik: „Rüstzeit“ als Zeit vom TA bis zur Quittierung angezeigt; Neva: das ist keine Arbeitszeit | Kennzahlen nach dem benennen, was sie messen (Durchlaufzeit), und die Grenze auf der Seite sagen | `LOGISTIK_LIVE_2026-10-01.md` |
| 2026-10-05 | Cockpit merkt einen Fehlabruf (Set fehlt) 15 Minuten wie einen Erfolg; nach dem P76-Import zeigte die Seite weiter „fehlt“ | Fehler kurz merken (1 Minute), Erfolge lang | `LOGISTIK_LIVE_2026-10-01.md` (offen) |
| 2026-10-05 | Fusszeile „Ohne Personendaten“ blieb stehen, nachdem Namen mit Login eingebaut waren | Bei jeder Erweiterung feste Texte der Seite auf neue Unwahrheiten durchsuchen | `LogisticsLive.razor` |
| 2026-10-05 | Logistik 3D: Platzschema geraten statt gemessen; echte Namen (`1-008-B`, `BP-MLE04-2`, `DL20004`) fielen durch | Bei Darstellungen aus Schluesseln zuerst echte Werte ansehen (Seite produktiv oeffnen), dann Regeln schreiben | `LOGISTIK_LIVE_2026-10-01.md` 3D |
| 2026-10-05 | SAP: Teil C sollte in eigenen Transport, die Klassen waren aber noch im nicht freigegebenen `T76K912658` gesperrt; Sichern fragte nicht nach dem Auftrag und haette Teil C still in Teil B gelegt | Vor jeder weiteren Aenderung an einem Objekt pruefen, ob es in einem offenen Auftrag gesperrt ist (SE03/E071); getrennte Transporte erst nach Freigabe des ersten | `LOGISTIK_LIVE_2026-10-01.md` Teil C |
| 2026-10-05 | SAP: Ingo gab `T76K912660` zusammen mit `T76K912658` frei, obwohl er erst die Struktur enthielt; das Sichern der Klassen meldete dann „Auftrag bereits freigegeben“ | Vor dem Sichern pruefen, ob der Zielauftrag noch offen ist; beim Melden eines Auftrags an Ingo dazusagen, ob er schon freigabereif ist oder noch Objekte bekommt | `LOGISTIK_LIVE_2026-10-01.md` Teil C |
| 2026-10-05 | Dialoge in GUI-Skripten ohne Auslesen weggeklickt: eine „Information“ nach dem Auftragsdialog blieb ungelesen, das Sichern war nicht erfolgt | Jedes unerwartete Fenster erst auslesen (`txtMESSTXT1/2`), dann entscheiden; `SapGuiAuftragNeuImDialog.vbs` liest und meldet es | `.tmp_sap_probe/` |
| 2026-10-05 | Datumsschutz `LogKapSet` (heute − 30 Tage) machte den ersten T76-Test mit Maerzdaten leer; erst ein Test im erlaubten Fenster zeigte, dass T76 offene Auftraege fuer Sept/Okt hat | Vor dem Test pruefen, wo im Testsystem im erlaubten Fenster Daten liegen (RFC `table-read` mit Datumsfilter) | `LOGISTIK_LIVE_2026-10-01.md` Teil C |
| 2026-10-01 | Ohne Datei wuerde ein Zeitraum vor den SAP-Daten eine leere, „verlaessliche“ 0-%-Quote zeigen | Bei jeder neuen Quelle pruefen, was ausserhalb ihrer Abdeckung angezeigt wird | `c12b34a` |
