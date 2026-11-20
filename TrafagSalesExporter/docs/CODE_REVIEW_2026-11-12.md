# Code-Review Befunde (12.11.2026)

Stand: 2026-11-20 (Nachfolge-Review `docs/CODE_REVIEW_2026-11-20.md` Abschnitt 2: Befund 13 erledigt am 16.11. mit `e10ab3a`,
die uebrigen zwoelf offen; Befunde 5 und 9 sind nicht mehr „nicht deployed“, `c9208b5` ist seit 18.11. 09:31 produktiv;
`finance-cockpit/controlling` aus Befund 3 ist laut Seed-Kommentar bewusst offen); davor 2026-11-12 (Review abgeschlossen,
nichts geaendert; Entscheide Ingo offen).

Auftrag Ingo 12.11.: „gesamten Quellcode reviewen, Bugs usw.“. Geprueft wurde der saubere Stand `a6a96b2` in einem eigenen
Worktree (ohne die unkommittierte Finance_All-Automatik), mit dem Review-Werkzeug auf Stufe „high“. Bei rund 128'000 Zeilen
war das keine Zeile-fuer-Zeile-Pruefung: der Commit `c9208b5` (Spanien) wurde ganz gelesen, der Rest gezielt nach den
Fehlerklassen dieses Repos; was nicht angesehen wurde, steht in Abschnitt E. **Die vier als hoch eingestuften Befunde (1 bis 4)
hat die Hauptsitzung selbst im Code nachgelesen und bestaetigt**; die uebrigen sind nicht ein zweites Mal geprueft. Befund 7
ist aus dem Router-Verhalten abgeleitet und nicht ausgefuehrt. Der Build des sauberen Standes kompiliert (77 Warnungen, davon
47 MudBlazor-Analyzer, 26 „nur Windows“; die drei nie zugewiesenen Benutzernamen-Felder sind toter Code).
Nicht im Umfang: Uebersetzungsdateien, Tests (nur als Beleg), `Tools/`, `whisper/`, ABAP.

Geprüfter Stand: Worktree `C:\TMP\TrafagSalesExporter_review_9e27a70\TrafagSalesExporter`, HEAD `a6a96b2`.
Alle Zeilenangaben beziehen sich auf diesen Stand. Es wurde nur gelesen: kein Build, kein Test,
keine Produktivdatenbank, kein SAP, keine Anmeldeseite, keine Änderung in einem der beiden Bäume.

Vorgehen: zuerst Commit `c9208b5` vollständig (Dedupe, Seed, SQL in allen drei Skriptkopien),
danach gezielte Suche nach den genannten Fehlerklassen und Lesen der jeweils umschliessenden
Funktion. Bei rund 128'000 Zeilen ist das keine Zeile-für-Zeile-Prüfung; was nicht angesehen
wurde, steht am Ende.

Schwere: Hoch = falsche Zahlen, Datenverlust oder Zugriff auf geschützte Daten möglich.
Mittel = tritt nur unter einer benennbaren Bedingung ein. Niedrig = latent oder kleiner Schaden.

---

## A. Ausgelieferter Code, nach Schwere

### 1. Hoch: Source Viewer liest jede Datei unter dem Anwendungsordner

`Components/Pages/SourceViewer.razor:91-104`

Geprüft wird nur, dass der Pfad kein `..` enthält und nicht absolut ist. Danach liest
`File.ReadAllText` jede Datei unter `ContentRootPath`, ohne Liste erlaubter Endungen oder Ordner.

Eingabe: `/source-viewer?path=appsettings.json`.
Ergebnis: Die Seite zeigt den ganzen Inhalt der Datei, also die dort hinterlegten Passwort-Hashes
für Admin, Finance und HR und alle weiteren Einträge (zum Beispiel das Shopfloor-Token). Mit
`path=web.config` gilt dasselbe. Die SQLite-Datenbank ist auf diesem Weg vermutlich nicht lesbar,
weil die Anwendung sie zum Schreiben offen hält und `File.ReadAllText` dann an der Dateifreigabe
scheitert; das habe ich nicht ausgeführt.

Die Seite hängt nur an der Finance-Freischaltung (`Routes.razor:57`). Zusammen mit Befund 2 reicht
das Finance-Passwort damit aus, um sich die HR- und die Admin-Freischaltung selbst auszustellen,
und zwar für jeden Bereich, dessen gültiger Hash noch der aus `appsettings.json` ist. Liegt für
einen Bereich ein neueres Passwort in der Tabelle `AccessPasswords`, gewinnt dieses
(`AccessPasswordPostConfigure`), und der Hash aus der Datei ist wertlos. Welche Bereiche das
produktiv betrifft, lässt sich in Admin > Passwörter am Änderungsdatum ablesen.

### 2. Hoch: Freischalt-Cookie lässt sich aus dem gespeicherten Hash berechnen

`Services/AccessUnlockCookie.cs:39-43`, `Services/AccessPasswordSettingsWriter.cs:12-13`

Der Cookie-Wert ist `SHA256("TrafagSalesExporter|<Cookiename>|<PasswordHash>")`. Es fliesst kein
Servergeheimnis, keine Sitzung und kein Ablaufdatum ein. Der Passwort-Hash selbst ist ein
ungesalzenes SHA-256.

Eingabe: Jemand kennt den gültigen Hash und setzt im Browser das Cookie `TrafagHrUnlocked` auf den
berechneten Wert. Der gültige Hash steht je nach Bereich in `appsettings.json` (im Repository und
über Befund 1 lesbar) oder, sobald das Passwort in der Oberfläche geändert wurde, in der Tabelle
`AccessPasswords` der Datenbank und damit in jeder Datenbanksicherung.
Ergebnis: `IsUnlocked` liefert `true`, die HR-Kennzahlen mit Personendaten sind ohne Passwort
sichtbar. Dasselbe gilt für `TrafagAdminUnlocked` und `TrafagFinanceUnlocked`. Das Cookie gilt für
alle Benutzer gleich und bleibt gültig, bis das Passwort geändert wird; das Ablaufdatum von zwölf
Stunden steht nur im Browser.

Der Shopfloor-Login macht es richtig (`ShopfloorLogin.CreateCookieValue` mit Data Protection und
Ablauf im Wert). Derselbe Weg wäre hier die tiefere Korrektur.

### 3. Hoch: Schreibende Seiten ohne jede Freischaltung

`Components/Routes.razor:40-61` und die Seiten selbst

Produktiv steht `Security:Enabled = false` (bekannt, LEARNINGS 2026-11-10); die Richtlinie
`AdminOnly` lässt dann jeden durch. Die Lehre dort betrifft neue Endpunkte. Bestehende Seiten
sind aber weiterhin offen, weil sie weder in der Liste von `RequiresFinanceUnlock` stehen noch
`IAdminAccessService.IsUnlocked` prüfen:

| Seite | Was ein beliebiger Intranet-Benutzer dort kann |
| --- | --- |
| `/einkauf/verbindungen` | SAP-Verbindung und `PasswordOverride` ändern, Full Load und Delta gegen SAP starten |
| `/manual-imports` | Importdateien für DE, UK und ES hochladen und aktivieren, also Umsatzdaten ersetzen |
| `/finance-journal-import` | Hauptbuch-Abruf aller B1-Gesellschaften starten |
| `/admin/menu-structure` | Menüstruktur speichern (nur `AdminOnly`) |
| `/marktsegmente` | Segmentzuordnungen pflegen |
| `/finance-cockpit/controlling` | Finance-Auswertung ohne Finance-Passwort lesen |
| `/admin/nutzung` | Nutzungsauswertung lesen (nur `AdminOnly`) |

Für `/verkauf` ist das ein dokumentierter Entscheid; für die Seiten oben habe ich keinen gefunden.
Falls es einen gibt, entfällt die jeweilige Zeile. Zwei Zeilen sind dafür naheliegende Kandidaten:
`/manual-imports` beschreibt sich selbst als Seite für Keyuser, und Controlling rechnet auf
derselben Datenbasis wie der bewusst offene Reiter Verkauf. Klar unbeabsichtigt wirken die
Einkaufsverbindungen mit dem SAP-Passwort und die beiden Admin-Seiten.

### 4. Hoch: Zentrale Tabelle wird je Standort erst gelöscht, dann in Einzelschritten gefüllt

`Services/CentralSalesRecordService.cs:37-41`, Aufruf `Services/SiteExportService.cs:107`

`DeleteExistingAsync` löscht alle Zeilen des Standorts in einer eigenen, sofort bestätigten
Transaktion. Danach schreibt `InsertRecordsInCommittedBatchesAsync` in Stapeln zu 25 Zeilen mit je
eigener Transaktion.

Fall a: Der Worker wird während des Schreibens beendet (laut Kommentar in
`TimerBackgroundService` geschieht das alle ein bis drei Stunden) oder ein Insert schlägt fehl.
Ergebnis: Der Standort steht bis zum nächsten erfolgreichen Lauf mit einem Teilbestand oder leer
in der zentralen Tabelle; alle Auswertungen zeigen entsprechend weniger Umsatz.

Fall b: Die Quelle liefert null Zeilen, ohne einen Fehler zu werfen.
Ergebnis: Der Bestand wird gelöscht, das Protokoll meldet „Export erfolgreich“ mit `Rows=0`.
Diese Falle ist bekannt (LASTCHANGE-Archiv, „Plausibilitätsgrenze vorgeschlagen, aber nicht
gebaut“) und für SAP-OData inzwischen abgefangen (`SapGatewayDataSourceAdapter.cs:60`). Für
Manual-Excel (DE, UK, ES) und HANA habe ich keine solche Prüfung gefunden; dort gilt sie weiter,
siehe Befund 5 als konkreten Auslöser.

Fall c: Während der Minuten des Schreibens (Spanien hat rund 100'000 Zeilen, also etwa 4'000
Transaktionen) lesen Cockpit und Reiter Verkauf einen halb gefüllten Standort. Der Reiter Verkauf
korrigiert sich danach selbst, weil sein Quellstempel aus Zeilenzahl und höchster Id besteht
(`CentralSalesDataProvider.cs:41-46`); wer in dem Fenster einen Export zieht, hat aber falsche
Zahlen in der Datei.

Neu an diesem Befund ist Fall a: Löschen und Schreiben gehören in eine Transaktion, dann sind auch
b und c entschärft, weil der alte Bestand bis zum Commit sichtbar bleibt.

### 5. Mittel (Commit c9208b5, nicht deployed): Seed schaltet den Kopfzeilen-Leser ab, wenn nur inaktive Zuordnungen existieren

`Services/DatabaseSeedService.cs:879-890`

`existingMappings` zählt alle Zeilen der Site, auch inaktive. Der Schutz „bei leerer Liste nichts
tun“ greift deshalb nicht, wenn jemand die Liste in Admin > Standorte nicht gelöscht, sondern
deaktiviert hat.

Eingabe: Spanien hat nur inaktive Zeilen in `ManualExcelColumnMappings`.
Ergebnis: Der Seed legt fünf aktive Zeilen an (zwei Datumsfelder, neu drei Lieferantenfelder).
`ReadCsvSalesRecords` sieht eine nicht leere aktive Liste und verwendet den gemappten Leser. Der
füllt nur diese fünf Felder; `IsMeaningfulMappedRecord` verwirft jede Zeile. Der Import liefert
null Zeilen, und nach Befund 4 wird der spanische Bestand gelöscht.

Der Mechanismus bestand schon mit den zwei Datumsfeldern; der Commit erweitert ihn auf fünf.
Produktiv ist die Liste laut Kommentar leer (Messung 2026-08-25), der Fall ist also latent.
Korrektur an der richtigen Stelle: nur aktive Zeilen zählen.

### 6. Mittel: Konfigurationsimport stellt das Kursprofil auf Tageskurs

`Models/ConfigTransferPackage.cs:62` gegen `Models/ExportSettings.cs:52`, Import in
`Services/ConfigTransferService.cs:305`

Die Paketklasse hat die Vorgabe `CurrentDailyRate`, das Modell `BudgetRate`. `Normalize` behandelt
nur leer oder unbekannt als Budget; `CurrentDailyRate` ist ein gültiger Wert und bleibt stehen.

Eingabe: Import einer Konfigurationsdatei, die das Feld `GroupMarginChfRateMode` nicht enthält
(exportiert vor Einführung des Feldes).
Ergebnis: `ExportSettings.GroupMarginChfRateMode` wird auf `CurrentDailyRate` gesetzt. Gruppenmarge,
Prüfbuch, Nachweis und Sales_All rechnen danach nicht mehr mit Budgetkursen, entgegen dem Entscheid
vom 2026-10-07. Der einmalige Nachzug (`BudgetRateDecision20261007Applied`) läuft kein zweites Mal.

Das ist die Stelle, die mir als Beispiel für doppelte Vorgabewerte mitgegeben wurde;
`docs/MD_KONSISTENZPRUEFUNG_2026-11-12.md` Abschnitt 6 führt sie mit dem Vermerk „ob das beim
Konfigurationsimport wirkt, ist nicht geprüft“. Mein Beitrag ist die Antwort: Es wirkt, über
`ConfigTransferService.cs:305`, bei jeder Datei ohne dieses Feld. Im Issue-Log steht dazu nichts.

### 7. Mittel: Finance-Sperre vergleicht den Pfad als Zeichenkette

`Components/Routes.razor:42-60`

`RequiresFinanceUnlock` vergleicht den rohen Pfad aus `Navigation.Uri` mit einer festen Liste.
`Routes` läuft interaktiv ohne Prerendering (`App.razor:44`), der Pfad kommt also so, wie er in
der Adresszeile steht. Der Router dekodiert Prozent-Kodierung vor dem Abgleich, die Sperre nicht.

Eingabe: `/setting%73` statt `/settings` (ebenso für `logs`, `standorte`, `source-viewer`).
Erwartetes Ergebnis: Die Sperre erkennt den Pfad nicht, der Router zeigt die Seite trotzdem.

Einschränkung: Ich habe das nicht ausgeführt (keine Anmeldeseiten). Die Aussage stützt sich auf
das Verhalten des Routers in .NET 8; im Browser ist sie in einer Minute geprüft. Unabhängig davon
ist die Liste die falsche Stelle: Eine neue Seite ist offen, bis jemand an die Liste denkt
(siehe `finance-cockpit/controlling` in Befund 3). Robuster wäre ein Attribut an der Seite, das
`Routes` aus `routeData.PageType` liest.

### 8. Mittel: `async void` ohne Fehlerbehandlung im Export-Dashboard

`Components/Pages/ExportDashboard.razor:480-497`

`HandleStatusChanged` ist `async void` und hängt am Ereignis `OnExportStatusChanged` des
Orchestrators. Der Orchestrator ruft es bei jeder Statusänderung vom Export-Thread aus auf, also
auch bei jedem der rund 8'000 Stapel-Meldungen aus Befund 4. `LoadDataAsync` und
`RefreshLiveDataAsync` haben kein `try/catch`.

Eingabe: Das Dashboard ist offen, während ein Export läuft, und ein Datenbankzugriff in
`LoadDataAsync` schlägt fehl, oder die Seite wird gerade geschlossen.
Ergebnis: Die Ausnahme hat keinen Aufrufer, der sie fangen könnte. Sie beendet den Prozess, mitten
im Export und damit im Fenster von Befund 4.

### 9. Mittel (Commit c9208b5, nicht deployed): Die Erbregel behandelt nur ein Symptom der Lesereihenfolge

`Services/DataSources/ManualExcelDataSourceAdapter.cs:388-398`,
Reihenfolge in `Services/SharePointUploadService.cs:208-213`

Spanische Dateien werden nach dem Startdatum ihres Bereichs gelesen, nicht nach ihrem Alter. Bei
gleichem Schlüssel gewinnt die später gelesene Zeile in allen Feldern. Der Commit lässt deshalb
einen leeren Lieferanten aus der früheren Zeile erben. Zwei Folgen:

a) Alle anderen Felder bleiben beim alten Verhalten.
Eingabe: neuer Export `range_20260101_to_20261112` und alte Datei `range_20260601_to_20260630`
mit derselben Position.
Ergebnis: Betrag, Menge, Buchungsdatum und Belegart kommen aus der alten Datei. Eine in Sage
nachträglich korrigierte Position oder ein nachgeliefertes Buchungsdatum geht verloren; nur der
Lieferant wird gerettet.

b) Die Regel kann einen überholten Lieferanten zurückbringen.
Eingabe: Die später gelesene Datei ist die neuere und trägt die Lieferantenspalten, der Lieferant
der Position ist dort aber zu Recht leer (Wareneingang storniert, Artikeltyp geändert).
Ergebnis: Der Lieferant der älteren Datei wird übernommen; die Zeile wird als Fremdbezug
klassifiziert, obwohl der aktuelle Export keinen Lieferanten kennt. Die Regel kann „Spalte fehlt“
nicht von „Wert leer“ unterscheiden.

Tiefere Korrektur: Reihenfolge nach Exportzeitpunkt (`LastModified` liegt in `ManualImportCandidate`
bereits vor), dann braucht es die Sonderregel nicht.

**Gesamturteil zu c9208b5:** Ausser den Befunden 5 und 9 habe ich nichts gefunden, was gegen den
Deploy spricht. Im Einzelnen geprüft:

- Der Schlüssel ist `SourceLineId` (`LineasPosicion`) und damit je Position eindeutig; es wird
  kein Lieferant zwischen verschiedenen Positionen übertragen.
- Die drei SQL-Kopien sind zeichengleich. `OUTER APPLY` korreliert über `CodigoEmpresa`, hat eine
  eindeutige Sortierung und einen Rückfall für ein leeres Lieferdatum. Datum und Dezimalzahlen
  schreibt das Skript kulturunabhängig, Texte in Anführungszeichen.
- Der Kopfzeilen-Leser gleicht exakt ab, `SupplierCountryName` kollidiert nicht mit
  `SupplierCountry`. Eine fehlende Spalte in einer alten Datei ergibt im gemappten Leser eine
  leere Zeichenkette, keinen Fehler.
- Der Weg in die Marge passt zu den Tests des Commits: ein Fremdlieferant ergibt „Extern“ mit den
  Kosten der Verkaufszeile, „TRAFAG AG“ ergibt „Intern“ mit Konzernkosten
  (`GroupMarginSupplierClassifier.Resolve` und `ResolveDeliveringEntity`).
- Offen bleibt die im Plan bereits benannte Voraussetzung, kein neuer Befund: Schreibt Sage Trafag
  AG anders als „TRAFAG AG“ oder „TR AG“, bleibt die Zeile „Intern“, verliert aber die
  Konzernkosten und rechnet mit dem lokalen IC-Preis, Status trotzdem „OK“
  (`docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md` Zeile 144 und 174, Prüfabfrage Teil F). Das
  Ergebnis von Teil F sollte vor dem Deploy vorliegen.
- Randfall, sehr selten: Der Marker-Abgleich läuft über „Nummer Name Land“. Endet ein
  Lieferantenname auf das Wort „TR“ und ist das Land `IT` oder `IN`, entsteht der Marker „TR IT“
  beziehungsweise „TR IN“ und die Zeile gilt als intern. Erst mit dem Länderkürzel aus Sage
  möglich.

### 10. Mittel bis niedrig: Ungeschützter Datenbankzugriff im Timer beendet die Anwendung

`Services/TimerBackgroundService.cs:77` und `:95`

`RecalculateNextRunAsync` wird in `ExecuteAsync` ohne `try/catch` erwartet; alle anderen Schritte
der Schleife sind abgesichert. `BackgroundServiceExceptionBehavior` ist nirgends gesetzt, es gilt
also `StopHost`.

Eingabe: Der Lesezugriff auf `ExportSettings` schlägt einmal fehl (Datenbank gesperrt während
eines Checkpoints, Datei kurz nicht erreichbar).
Ergebnis: Der Host fährt herunter, also die ganze Webanwendung mit allen offenen Sitzungen und
laufenden Hintergrundläufen. Unter IIS startet sie erst mit der nächsten Anfrage wieder. In Zeile
95 trifft es den Moment direkt nach Tagesexport und Einkauf-Delta.
`Recalculate()` in Zeile 47 verwirft dieselbe Ausnahme stillschweigend; `_nextRun` bleibt dann auf
dem alten Wert, eine in den Einstellungen geänderte Uhrzeit wirkt nicht.

### 11. Mittel bis niedrig: Finance-Regel mit leerem Vergleichswert trifft jede Zeile

`Services/FinanceRuleEngine.cs:200-216`, Speichern in `Services/FinanceRulesPageService.cs:64`

`Contains("")` und `StartsWith("")` sind immer wahr; ein leerer `FieldName` liefert ebenfalls eine
leere Zeichenkette. Beim Speichern wird nichts geprüft.

Eingabe: In `/finance-rules` wird eine Regel `Exclude`, Geltungsbereich `DE`, Vergleich `Contains`
angelegt und der Wert bleibt leer.
Ergebnis: Sämtliche deutschen Zeilen sind ausgeschlossen, der Umsatz DE ist in Cockpit, Verkauf
und Nachweis null. Mit `NegateAmount` würden alle Beträge negativ.
Nebenbei: Der `switch` über `MatchType` unterscheidet Gross- und Kleinschreibung, der Vergleich
auf `Always` nicht.

### 12. Niedrig: Einkauf-Delta entfernt in SAP gelöschte Einteilungen nie

`Services/PurchasingDataRefreshService.cs:384-408`

Das Delta schreibt nur per `INSERT OR REPLACE`. Für nachgeladene Belege werden Zeilen, die SAP
nicht mehr liefert, nicht gelöscht; das macht nur der Full Load (Zeilen 324-326).

Eingabe: Eine Einteilung (EKET) mit offener Menge wird in SAP gelöscht.
Ergebnis: Die Zeile bleibt im Cache, zählt weiter als offen und als überfällig und sorgt dafür,
dass der Beleg bei jedem Delta wieder nachgeladen wird, bis zum nächsten Full Load.

Dazu eine Frage an die Fachseite, kein Befund: Der Filter `Aedat ge` auf EKKO (Zeile 368) findet
nur dann geänderte Belege, wenn das OData-Feld wirklich ein Änderungsdatum ist. In der
Standardtabelle EKKO ist AEDAT das Anlagedatum des Satzes.

### 13. Niedrig: „Datei öffnen“ startet einen Prozess auf dem Server

`Components/Pages/ExportDashboard.razor:520`

`Process.Start` mit `UseShellExecute = true` läuft im IIS-Worker, nicht beim Benutzer. Ein Klick
öffnet die Exportdatei bestenfalls unsichtbar in Sitzung 0 des Servers oder scheitert; der
Benutzer sieht nichts. Rest aus der Zeit als Desktop-Anwendung.

---

## B. Geprüfte Spuren ohne Befund

- **CS0649 in `AdminAccessPanel.razor:52`, `FinanceCockpitUnlockPanel.razor:55`, `HrKpi.razor:265`:**
  Kein Fehler. Die Anmeldung läuft über ein HTML-Formular an `/access/admin`, `/access/finance` und
  `/access/hr`; der Benutzername kommt aus dem Formularfeld `username`. Die Methoden `Unlock()` und
  `UnlockHrKpiAsync()`, die die nie zugewiesenen Felder lesen, werden von keinem Knopf mehr
  aufgerufen. Das ist toter Code, kein stiller Null-Benutzername.
- **CS8604 in `PageUsageTracker.cs:182`:** Kein Fehler. `Max(v => v.Day)` läuft je Route über eine
  Liste, aus der die Route selbst stammt, sie ist also nie leer.
- **SQL mit Benutzereingabe:** Die Treffer bauen SQL aus Konstanten, formatierten Datumswerten oder
  mit Parametern. Der Berichtsmonat der Liefertreue wird auf sechs Ziffern geprüft, bevor er in
  den OData-Filter geht.
- **Shopfloor und `/api/integration/push`:** Ein leeres Token sperrt die Schnittstelle
  (`TokenOk`), der Vergleich ist zeitkonstant, schreibende Aufrufe verlangen den Header `X-User`.
- **`FinanceRuleEngine`-Dedupe:** Jede Auswertung baut eine eigene Instanz und ruft
  `ShouldInclude` je Zeile einmal auf; der Zustand zählt keine Zeile doppelt.
- **`preferredImportYear`:** Wird von keinem Aufrufer gesetzt, der Jahreszweig ist toter Code.

## C. Bekannt und deshalb nicht gemeldet

Kurs-Suche über Budget- und EZB-Zeilen in derselben Tabelle (ISS-018 A2, LEARNINGS 2026-10-07),
leere Kostenwährung (B3.2), Extraktionsdatum als Periodenrückfall (B4), `Security:Enabled = false`
als Ursache (LEARNINGS 2026-11-10), leere Quellantwort leert den Standort (LASTCHANGE-Archiv,
`docs/rag/MANUAL_IMPORT.md`), Schreibweise von Trafag AG in Sage (Plan ES Supplier),
kulturabhängiges Parsen als Fehlerklasse, WKURS-Richtung.

Zu den Befunden 1, 2, 7, 8, 11 und 13 habe ich in `docs/` keinen Eintrag gefunden (Suche nach
`SourceViewer`, `AccessUnlockCookie`, `async void`, `HandleStatusChanged`, `Process.Start`,
`MatchValue`).

Ein Punkt am Rand davon, falls er noch nicht gemessen ist: Der Reiter Verkauf rechnet mit
`ResolveRate` zum Belegdatum (`Services/Sales/SalesDataService.cs:109`). Für EUR trifft das bis zum
ersten EZB-Import die Budgetzeile und danach den EZB-Tageskurs, für alle anderen Währungen immer
die Budgetzeile. Der Kurs wechselt damit für EUR mitten im Jahr 2026.

---

## D. Nicht ausgeliefert: Finance_All-Automatik im Arbeitsbaum

Betrifft `Services/FinanceAllExportService.cs`, `Services/FinanceAllWorkbookWriter.cs` und die
Änderungen in `Services/TimerBackgroundService.cs` im Arbeitsbaum
`C:\Users\koi\source\repos\Ai\TrafagSalesExporter`. Unkommittiert und unfertig, deshalb getrennt
und ohne Einfluss auf die Rangfolge oben. Den Writer habe ich nur in Teilen gelesen.

- **D1, Mittel: Der Tagesstempel wartet jetzt auf Finance_All.**
  `TimerBackgroundService.cs:135-140` (Arbeitsbaum). `RunFinanceAllAsync` läuft vor
  `StampLastRunAsync`. Wird der Worker während der Erzeugung oder des bis zu zehn Minuten langen
  Uploads beendet, fehlt der Stempel, und der Nachhol-Lauf beim nächsten Start wiederholt den
  ganzen Export aller Standorte. Vorher lag der Stempel direkt hinter dem Export.
- **D2, Mittel: Ein Upload-Fehler erscheint nicht im Admin-Log.**
  `FinanceAllExportService.cs:133-141`. Der Kommentar in `RunFinanceAllAsync` sagt, der Fehler lande
  über `IAppEventLogService` im Admin-Log. Das stimmt nur für die Erzeugung. Schlägt der Upload
  fehl oder läuft er in die Zeitgrenze, bleibt im Admin-Log „SharePoint Upload gestartet“ als
  letzter Eintrag stehen; der Fehler steht nur im Konsolenprotokoll. Genau der stille Ausfall, den
  der Kommentar vermeiden will.
- **D3, Niedrig: Abbruch und Zeitgrenze halten die Arbeit nicht an.**
  `FinanceAllExportService.cs:91-92` und `:141`. `Task.Run(..., abbruch)` bricht nur ab, solange die
  Aufgabe noch nicht läuft; `WaitAsync` gibt nach zehn Minuten auf, der Upload läuft im Hintergrund
  weiter. Der Timer übergibt ausserdem kein Abbruchsignal.
- **D4, Niedrig: Die Mappe wird direkt am Zielpfad geschrieben.**
  `FinanceAllWorkbookWriter.cs:89`. Bricht der Writer ab, bleibt eine unvollständige
  `Finance_All_<Datum>.xlsx` im Ausgabeordner liegen. Schreiben in eine temporäre Datei und
  Umbenennen am Ende vermeidet das.
- **D5, Niedrig, ungeprüft: Steuerzeichen im Buchungstext.**
  `FinanceAllWorkbookWriter.cs:633-636`. `new Text(text)` geht ungefiltert in den XML-Writer. Ein
  in XML unzulässiges Steuerzeichen in einem Freitextfeld würde die ganze Erzeugung abbrechen.
  Das Python-Werkzeug hätte dasselbe Problem; solange es durchläuft, enthält der Bestand keines.
- **D6, Aufräumen: Drei Hilfsmethoden sind aus `ConsolidatedExportService` kopiert**
  (`ResolveCentralSharePointUploadTarget`, `IsLegacyExportFolder`, `HasCompleteSharePointConfig`,
  dort ab Zeile 375), dazu die Ordnerauflösung aus Zeile 366. Ändert sich die Zielauflösung für
  Sales_All, landet Finance_All still in einem anderen Ordner.

---

## E. Nicht angesehen

HR-Rechenlogik (`HrKpiDashboardBuilder`), `ManagementCockpitService` ausserhalb der Parse-Stellen,
`ExcelExportService`, `PurchasingDashboardService` (nur SQL-Bau gesichtet), Projekte, Forum,
Netzwerk, Weltlage, Handbestellwesen, die meisten Razor-Komponenten (Lebenszyklus nur
stichprobenweise). Ausgenommen laut Auftrag: Textdateien der Übersetzungen, `whisper/`, `.tmp_*`,
`Tools/`, `zlo03/`, ABAP.

---

## F. Vorschlag fuer die Behebung (nicht umgesetzt, Reihenfolge nach Risiko)

Stand 2026-11-12: Ingo hat noch nichts entschieden. Die Ansaetze unten sind Vorschlaege der Hauptsitzung, kein gepruefter
Entwurf; vor jeder Umsetzung gilt „erst Plan, dann ausfuehren“, Tests in `TrafagSalesExporter.Tests`, Deploy nur mit dem
Werkzeug. Zeilenangaben gelten fuer den Stand `a6a96b2`.

| Rang | Befund | Ansatz | Vorher klaeren |
| --- | --- | --- | --- |
| 1 | 1 Source Viewer (`Components/Pages/SourceViewer.razor:91`) | Nur noch Dateien einer festen Liste von Endungen und Ordnern zeigen (Quelltext `.cs`/`.razor`), `appsettings*.json`, `web.config`, `*.db`, Schluessel- und Protokolldateien ausschliessen; Zugriff an die Admin-Freischaltung haengen. Test: `appsettings.json` wird abgelehnt | Wird die Seite ueberhaupt gebraucht (Nutzung unter Admin > Nutzung), sonst entfernen |
| 2 | 2 Freischalt-Cookie (`Services/AccessUnlockCookie.cs:39`, Hash in `AccessPasswordSettingsWriter.cs:12`) | Cookie-Wert mit einem Servergeheimnis bilden, das nicht im Repository liegt (ASP.NET Data Protection oder ein beim ersten Start erzeugter Schluessel in der DB), Ablaufzeit in den Wert aufnehmen; Passwort-Hashes mit Salz und langsamem Verfahren (PBKDF2) speichern und beim naechsten Login umstellen; danach alle Bereichspasswoerter neu setzen und die Hashes aus `appsettings.json` im Repository entfernen (Rueckfall dann leer) | Alle Sitzungen werden einmal abgemeldet; Reihenfolge mit Ingo (Passwoerter neu setzen) |
| 3 | 3 Schreibende Seiten ohne Freischaltung (`Components/Routes.razor:40`) | Je Seite entscheiden: `/einkauf/verbindungen`, `/finance-journal-import`, `/admin/menu-structure`, `/admin/nutzung` hinter Admin; `/marktsegmente` hinter Finance; `/manual-imports` und `/finance-cockpit/controlling` nach Entscheid Ingo. Schutz in der Seite selbst pruefen (wie `AdminPasswords.razor`), nicht nur in der Routenliste | Wer nutzt `/manual-imports` (Keyuser der Standorte) und `/marktsegmente` heute ohne Passwort |
| 4 | 7 Pfadvergleich der Finance-Sperre (`Components/Routes.razor:42`) | Pfad vor dem Vergleich dekodieren und normalisieren (`Uri.UnescapeDataString`), besser: Schutz an der Seite statt an einer Pfadliste (faellt mit Rang 3 zusammen) | Zuerst im Browser nachstellen (`/setting%73`), der Befund ist nur abgeleitet |
| 5 | 4 Standortbestand nicht atomar (`Services/CentralSalesRecordService.cs:37`) | Loeschen und Schreiben eines Standorts in EINER Transaktion; null Zeilen aus Manual-Excel und HANA nicht uebernehmen, sondern den alten Bestand behalten und als Fehler melden (wie beim SAP-Gateway, `SapGatewayDataSourceAdapter.cs:60`) | Laufzeit und Sperrdauer bei grossen Standorten (CH), WAL-Groesse |
| 6 | 5 und 9 Spanien, Commit `c9208b5` (nicht deployed) | 5: im Seed nur aktive Zuordnungen zaehlen (`DatabaseSeedService.cs:890`). 9: Spanien-Dateien nach Aenderungsdatum statt nach Bereichsstart lesen (`ManualImportCandidate` kennt `LastModified`), dann kann die Erbregel entfallen oder als Sicherung bleiben | Vor dem Deploy von `c9208b5` entscheiden, ob 5 und 9 zuerst behoben werden; 5 ist produktiv latent (Liste leer), 9 wirkt erst mit der Nachlieferung (Gang 2) |
| 7 | 6 Konfigurationsimport (`Models/ConfigTransferPackage.cs:62`) | Vorgabe auf `BudgetRate` wie `ExportSettings.cs:52`; fehlendes Feld beim Import nicht als Tageskurs deuten. Test mit einem Paket ohne das Feld | keine |
| 8 | 8 `async void` (`Components/Pages/ExportDashboard.razor:480`) und 10 Timer (`Services/TimerBackgroundService.cs:77`) | try/catch mit Protokolleintrag um die Datenbankzugriffe; fuer den Hintergrunddienst `BackgroundServiceExceptionBehavior.Ignore` pruefen | keine |
| 9 | 11 bis 13 (leerer Vergleichswert in Finance-Regeln, geloeschte Einteilungen im Einkauf-Delta, „Datei oeffnen“ auf dem Server) | einzeln, nach den Punkten oben | 11 fachlich mit Ingo (soll eine Regel ohne Vergleichswert ungueltig sein) |

Zusammenhang mit anderen offenen Punkten: Rang 2 haengt mit dem Admin-Passwort im Klartext zusammen
(`docs/MD_KONSISTENZPRUEFUNG_2026-11-12.md` Abschnitt 4 „Sofort“, P29). Abschnitt D (Finance_All) betrifft nur die
unkommittierte Automatik und wird erst wichtig, wenn Ingo ueber sie entscheidet (Wochen_Todo 117).
