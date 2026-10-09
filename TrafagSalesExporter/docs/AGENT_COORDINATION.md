# Agenten-Koordination

Stand: 2026-10-09

Diese Datei koordiniert gleichzeitig arbeitende Entwicklungsagenten im gemeinsamen
Workspace. Vor jeder Aenderung bitte vollstaendig lesen und den eigenen Eintrag
aktualisieren. Die Root-Dateien `AGENTS.md` und `CLAUDE.md` sowie `router.md` machen
diesen Schritt fuer neue Codex-/Claude-Sitzungen ausdruecklich verpflichtend.

**Diese Datei enthaelt nur laufende Arbeit und einen Kurzindex der juengsten Eintraege** (Ziel
sieben Tage; beim naechsten Aufraeumen gehoeren aeltere Zeilen in die Historie). Alles
Abgeschlossene, die frueheren Reservierungen gemeinsamer Dateien und die
Uebergabeprotokolle stehen im Volltext in `docs/AGENT_COORDINATION_HISTORIE.md`.
Das ist am 2026-09-09 getrennt worden, weil die Datei auf 236 KB gewachsen war und
damit vor jeder Analyse gelesen werden musste, obwohl fast alles darin auf
„abgeschlossen, Reservierung frei" stand. Wer eine abgeschlossene Arbeit sucht,
findet sie ueber den Kurzindex unten oder ueber die Historiendatei; nichts ist
geloescht.

## Aktive Bereiche

Hier steht ausschliesslich, was gerade laeuft oder reserviert ist. Eintraege mit
`abgeschlossen`, `deployed`, `frei` oder `Historie` gehoeren nicht hierher, sondern
in die Historiendatei.

| Agent | Bereich | Reservierte Dateien / Ordner | Status |
|---|---|---|---|
| Claude | Neuer Reiter Trafag Reddit: Forum mit Communities, Fragen/Antworten, Abstimmung, Karma (Reddit + StackOverflow), sofort fuer alle, Autor mit Windows-Namen, Deploy freigegeben (Wunsch Ingo 2026-10-09) | `Components/Pages/Forum*.razor`, `Components/Forum/*`, `Services/Forum/*`, `Models/Forum*.cs`, `Data/AppDbContext.cs`, Schema-SQL/-Wartung, Menue-Seed, `NavigationIconResolver.cs`, Uebersetzungen, `app.css` (Praefix `frm-`), `Program.cs` (eine Zeile), Tests, Doku | in Arbeit seit 2026-10-09 |

**Hinweis:** Im Arbeitsbaum liegt die unfertige Finance_All-Automatik aus der am 2026-09-29 geschlossenen Sitzung vom 2026-09-11 (unkommittiert; Stand 2026-09-29: `Services/FinanceAllExportService.cs`, `Services/FinanceAllWorkbookWriter.cs`, Testdatei und Aenderungen an `TimerBackgroundService.cs`, `Program.cs`, `.csproj`; ob fertig, ist ungeprueft). Nicht mit anderer Arbeit committen oder deployen. Einzelheiten in der Historie.

## Kurzindex der juengsten Eintraege

Abgeschlossene Arbeit seit dem 2026-09-02, eine Zeile je Eintrag. Der Volltext mit
Nachweisen, geaenderten Dateien und Fallen steht in
`docs/AGENT_COORDINATION_HISTORIE.md`.

| Agent | Bereich | Letztes Datum | Ergebnis in Kurzform |
|---|---|---|---|
| Claude | Neuer Reiter Operations (Shopfloor): PPA-Shopfloor-App ins Cockpit (JS-Oberflaeche uebernommen, Backend in C#), Etappe 1 Geruest + ZD05 + Forecast aus SAP; sichtbar erst nach Freigabe Leiter Produktion/Operations (2026-10-07) | 2026-10-08 | abgeschlossen, deployed 2026-10-08 06:39 (`59d0531`); SAP T76K912718 offen, Reservierung frei |
| Claude | Operations (Shopfloor): Design wie Cockpit (CI, Hell/Dunkel), Grafiken ergaenzen (Wunsch Ingo 2026-10-08) | 2026-10-08 | abgeschlossen, deployed 2026-10-08 09:23 (`cbf168c`); Cache-Fix committet, Reservierung frei |
| Claude | Einkauf (Gespraech Armin 2026-10-08): offener Mengenkontraktwert (neues Set EinkKontraktSet, T76) auf Kontrakte und Dashboard; Lebenszyklus- und Sortiments-Code im Spend-Aufriss | 2026-10-08 | deployed 2026-10-08 15:03 (`f9bf598`); SAP T76K912724+T76K912718 offen, Reservierung frei |
| Claude | Einkauf Punkt 4 (Armin 2026-10-08): Produktgruppe im Spend-Aufriss ueber ZLO03 (Komponente -> verkuerzte Nummer -> Disponent -> ZC23); Ursache: Verwendungs-Cache nur 86 Zeilen | 2026-10-08 | deployed 2026-10-08 15:03 (`f9bf598`); ohne SAP-Aenderung (Bottom-Up je Komponente ueber ZSTR_LZCODE_USAGESet, Cache PurchasingComponentDispoCache); Tests 1135/1135; Reservierung frei |
| Claude | SAP T76: ZM_OFFENE_FAUF Bedarfsverursacher-Spalten standardmaessig einblenden (Wunsch Ingo 2026-10-08) | 2026-10-08 | abgeschlossen in T76 2026-10-08 (NO_OUT entfernt, aktiv, Auftrag `T76K912714` offen, Freigabe/Import Ingo); fremdes inaktives `Z_REICHW` nicht angefasst; Reservierung frei |
| Claude | SAP T76: ZM_OFFENE_FAUF Werk des Bedarfsverursachers (MDRQ-WERKS) als Feld `ZZPEG_WERKS` (Wunsch Fabio/Ingo 2026-10-08) | 2026-10-08 | abgeschlossen in T76 2026-10-08 (Struktur und Report aktiv, Layout gesichert, geprueft mit Material 63500); Import P76 durch Ingo; Reservierung frei |
| Claude | Finance-CHF-Umrechnung auf Budgetkurs je Finance-Jahr (Entscheid Ingo 2026-10-07: immer Budgetkurse) | 2026-10-07 | abgeschlossen, deployed 2026-10-07 10:06 (`cbc528b`), Sales_All-Neuerzeugung durch Ingo, Reservierung frei |
| Claude | Doku-Nachtrag 1-Stufen-Logik, Lieferantenerkennung und Befund TR IT/TR IN-Artikel (2026-10-07) | 2026-10-07 | abgeschlossen 2026-10-07, nur Doku, Reservierung frei |
| Claude | SAP T76: Report `ZM_OFFENE_FAUF` um Bedarfsverursacher (MD_PEGGING_NODIALOG) erweitern, Struktur `ZMM_UEB_FAUF` (Wunsch Ingo 2026-10-07) | 2026-10-07 | abgeschlossen in T76 2026-10-07, Transport `T76K912714` offen (Freigabe Ingo), Reservierung frei |
| Claude | Pruefbefund-Fixes 2026-10-05: Elikz True/False an allen Stellen, OrdersOnly in Einkauf Interaktiv/Weltlage, Logistik live (Timeout, Fehlercache, Tagesfilter, Ausblick, Lgnum), Interaktiv-/Controlling-Rechnungen, Netzwerk/AD-Einstufungen | 2026-10-06 | abgeschlossen, deployed 2026-10-06 10:16 (`f582814`); ABAP-Teil Logistik wartet auf T76-Transport, Reservierung frei |
| Claude | Betriebswirtschaftliches Anwenderhandbuch aller Module als Word (2026-10-06) | 2026-10-06 | abgeschlossen, deployed 2026-10-06 10:16 und 10:46 (`d7270a5`), Reservierung frei |
| Claude | Weltlage unter Finance Cockpit verschieben, hinter Finance-Passwort (Wunsch Ingo 2026-10-06) | 2026-10-06 | abgeschlossen, deployed 2026-10-06 14:36 (`eb47027`), Intranet-News im Papierkorb, Reservierung frei |
| Claude | Standardkosten in CHF im Excel-Blatt Finance Details (Wunsch Andreas/Ingo 2026-10-06), Klaerung aktuell vs. historisch | 2026-10-06 | abgeschlossen, deployed 2026-10-06 15:45 (`3c0db1b`), Reservierung frei |
| Claude | Logistik live: Kapazitaetsplanung (Arbeitsvorrat Kommissionierung, Arbeitsplaetze Produktion, Warenausgang nach Termin), Produktivitaet Ruesten bis Warenausgang, Namen nur mit Login (HR-Freigabe laut Ingo 2026-10-05) | 2026-10-05 | in Arbeit 2026-10-05; Stand 12:45: Cockpit A/B/D produktiv (`9fdae38`, `b776115`, `f9dc3eb`), SAP Teil B `T76K912658` in T76 aktiv und getestet, wartet auf Ingos Freigabe/Import; Teil B in P76 importiert (Ingo); Teil C Struktur `T76K912660` (freigegeben), Klassen `T76K912662` in T76 aktiv und getestet, in P76 mit Warnung importiert, `LogKapSet` produktiv 13:53 bestaetigt; Cockpit bis `dd4c627` produktiv 13:34. Offen nur: Passwort-Hash (Ingo), Fehler kuerzer merken |
| Claude | Einkauf: Interaktiv (9 Ansichten auf Bestellungen) und Korrekturen Verkauf Interaktiv | 2026-10-05 15:15 | **Abgeschlossen, produktiv 15:11 (`ba13744`), Reservierung frei.** `PurchasingInteractiveService`, `PurchasingInteractivePage`, Schalter `Purchasing` in den Ansichten, 29 Texte, 2 Tests. Tooltip-Korrektur Netzwerk im naechsten Deploy. |
| Claude | Verkauf: Unterreiter Interaktiv (9 animierte Ansichten) | 2026-10-05 14:45 | **Abgeschlossen, produktiv 14:25 (`25da8a1`), Reservierung frei.** `SalesInteractive` + 8 Tests, `Components/Sales/Interactive/*`, Seite `/verkauf/interaktiv`, 65 Texte. Korrekturen laufen im Eintrag Einkauf Interaktiv mit. |
| Claude | Logistik live: 3D-Lagerplatzansicht | 2026-10-05 10:25 | **Abgeschlossen, Reservierung frei.** `98146c6` produktiv 09:21, `71981b6` (echte Platznamen, Gaenge in Spalten) produktiv 10:23, mit echten Daten angesehen. `LogisticsBinLayout` + 15 Tests, `WarehouseBins3D.razor`, CSS `wh3d-`, 18 Texte. |
| Claude | Reiter Weltlage (externe Quellen, Radar je Abteilung, offen fuer alle) | 2026-10-05 10:25 | **Abgeschlossen, Reservierung frei.** `e54190a` produktiv 09:21, `1e3fd0f` (EZB-90-Tage-Kurse) produktiv 09:45; Quellen vom Server: GDELT, Eurostat, EZB verbunden, FRED und IMF Firewall blockiert. Offene Befunde in `WELTLAGE_2026-10-05.md`: Logistik 0.0 (Elikz-Vermutung), EUR netto -6.9 Mio (Waehrung an EU-Verkaufszeilen pruefen). Intranet-News: leerer Entwurf `SitePages/Page(19).aspx` angelegt, nicht veroeffentlicht. |
| Claude | Finance Cockpit: Unterreiter Controlling (Eigeninitiative) | 2026-10-05 08:20 | **Abgeschlossen und produktiv um 08:18, Reservierung frei.** `cb08f18`: `ControllingAnalytics` (Umsatzbruecke, Hochrechnung), `SalesFact` mit Lokalwert und Waehrung, Seite `FinanceControlling.razor` ohne Passwort, Menue, CSS `ctl-`, 27 Texte, 6 Tests; 911/911 (896 Release), ohne Alarm, per Edge headless angesehen. Offene Fragen nur in `CONTROLLING_2026-10-05.md` und auf der Seite (kein Auftraggeber). Doku Router Finance, `baum.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, `rag/ARCHITECTURE.md`, Wochen_Todo 75. |
| Claude | Reiter Verkauf: ohne Passwort und Korrekturen | 2026-10-02 14:46 | **Abgeschlossen und produktiv, Reservierung frei.** `84fd1fd` ohne Passwort (Ingo), `fa83ad1` Vergleich gleicher Zeitraeume (Daten ab 01.2025), `a2bd605` Veraenderung je Gesellschaft, Verluste nach 6 Monaten, Preisstreuung bereinigt, alter Stand beim Neuladen, Achsanker (auch Netzwerk-Verlauf), `81a4132` Prognoseachse. Tests 905/905 (890 Release). Alle Seiten per Edge headless angesehen. Doku `VERKAUF_2026-10-02.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, LEARNINGS (2), Wochen_Todo 74. |
| Claude | Reiter Verkauf (acht Unterreiter) | 2026-10-02 13:26 | **Abgeschlossen und produktiv um 13:24, Reservierung frei.** `e2fb8ce`: `Services/Sales/*` (Datendienst nach Finance-Regeln, ohne Konzernkunden, CHF zum Belegdatum; reine Logik `SalesAnalytics`), acht Seiten `Sales*.razor`, `Components/Sales/*` (inkl. stilisierte Weltkarte), Menue, CSS `sal-`, 77 Texte, 14 Tests; drei Helfer in `ManagementCockpitService` auf `internal`; `/verkauf*` hinter der Finance-Freischaltung (`Routes.razor`); `Program.cs` nur eigene Zeilen. Tests 898/898 (883 Release), DLL bitgleich, ohne Alarm; Sperre geprueft, Seiten wegen Passwort nicht von mir angesehen. Doku `VERKAUF_2026-10-02.md` (neu), Router Finance, `baum.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, `rag/ARCHITECTURE.md`, LEARNINGS, Wochen_Todo 74, Status kurz. |
| Claude | Netzwerk: Migration, Bericht, Geraetelandkarte, Altlasten-Score | 2026-10-02 12:01 | **Abgeschlossen und produktiv um 12:00, Reservierung frei.** `c6b2d29` + `7d31df5`: Seiten `NetworkMigration.razor`, `NetworkReport.razor`, Karte in `NetworkAdInfra.razor`, Score-Reiter in `NetworkAd.razor`, Logik in `AdInfraAnalysis`, Kennzahlen `os:*`/`altlasten` im Schnappschuss. Tests 884/884 (869 Release), DLL bitgleich, Alarm nur WAL-Checkpoint. Alle vier per Edge headless angesehen. Doku `NETZWERK_2026-10-02.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, LEARNINGS, Wochen_Todo 73. |
| Claude | Netzwerk: AD-Infrastruktur, Gruppenrichtlinien, DNS, Verlauf | 2026-10-02 10:17 | **Abgeschlossen und produktiv um 10:14, Reservierung frei.** `50b6c4a` (enthaelt `65e3a88`): Dienste `AdInfrastructureService`, `AdGpoService`, `AdDnsZoneService`, `AdSnapshotService`, Logik `AdInfraAnalysis`, vier Seiten, Drucker in Arbeitsplaetze, Excel-Export, Countdown, drei Tabellen; `Program.cs` nur eigene Zeilen per Index. Tests 881/881 (866 Release), DLL bitgleich, ohne Alarm, Seiten per Edge headless angesehen. Doku `NETZWERK_2026-10-02.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, `rag/ARCHITECTURE.md`, LEARNINGS, Wochen_Todo 73. |
| Claude | Netzwerk AD: Korrekturen nach Sichtpruefung | 2026-10-02 08:53 | **Abgeschlossen, Reservierung frei; nur Deploy `65e3a88` offen.** `ce9e06a` 08:29 Klassen `dir-` (Edge-Werbefilter blendete `ad-tabs` aus), `3e21220` 08:43 LAPS/BitLocker „nicht lesbar“, RODC als DC, Wurzelhoehe; `65e3a88` Veeam gruppiert. Tests 868/868 (853 Release). Produktiv per Edge headless (DevTools) alle Reiter angesehen. Eintrag nachtraeglich (LEARNINGS). Doku `NETZWERK_2026-10-02.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, LEARNINGS, Wochen_Todo 73. |
| Claude | Netzwerk: Unterreiter Active Directory | 2026-10-02 08:15 | **Abgeschlossen und produktiv um 08:07, Reservierung frei.** `b57d20f`: `AdAnalysis` (neu), `AdComputerService` (OUs, Subnetze, LAPS-Datum, BitLocker-Anzahl, DNS), DCs als Pruefziele, Seite `NetworkAd.razor` mit 3D-OU-Stadt und sieben inneren Reitern, `ComputerTable.razor`, Menue, CSS, 68 Texte in sieben Sprachen, 25 Tests; 864/864 (849 Release), DLL bitgleich, ohne Alarm. Sichtpruefung im Browser offen (Erweiterung nicht verbunden), 3D vorab mit Edge headless am Muster geprueft. Doku `NETZWERK_2026-10-02.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, `rag/ARCHITECTURE.md`, LEARNINGS (3 Zeilen), Wochen_Todo 73. |
| Claude | Netzwerk: AD-Computer einschalten | 2026-10-02 07:36 | **Abgeschlossen und produktiv um 07:34, Reservierung frei.** Erlaubnis der IT laut Ingo. `48121df`: nur `appsettings.json` (`NetworkProbe:AdEnabled = true`), 824/824, DLL bitgleich, Server-appsettings geprueft. Gleiche AD-Abfrage vom Arbeitsplatz: 784 Computer, 50 aktive ohne Anmeldung seit 90 Tagen. Sichtpruefung im Browser nicht moeglich (Erweiterung nicht verbunden), Lesezugriff des App-Pools also unbelegt. Doku `NETZWERK_2026-10-02.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, Wochen_Todo 73, Status kurz. |
| Claude | Reiter Netzwerk (fuenf Unterreiter) | 2026-10-02 07:22 | **Abgeschlossen und produktiv um 07:20, Reservierung frei.** Entscheide Ingo: laufend alle 5 Min., AD nur Computer ohne Personen (Schalter aus bis IT zustimmt). Schutz: SAP nur `/sap/public/ping` ohne Anmeldung, HANA nur Port, feste Ziele, keine Scans. `1635b46`: Dienste `Services/Network/*`, fuenf Seiten, Menue, Tabellen, CSS, Texte sieben Sprachen, Tests 839/839 (824 Release), DLL bitgleich (SHA256 `D73F77B5...5A68`). Erste Runde 07:21: alle 7 Ziele ok; Client-Subnetze werden erkannt. Doku `NETZWERK_2026-10-02.md`, Router Plattform, `baum.md`, `lastchange.md`, `rag/DEPLOYMENT.md`, `rag/ARCHITECTURE.md`, LEARNINGS, Wochen_Todo 73, Status kurz. |

## Regeln

1. Ein Agent bearbeitet nur seinen eingetragenen Bereich.
2. Vor Aenderungen an gemeinsam genutzten Dateien zuerst hier reservieren. Dazu
   gehoeren insbesondere `Program.cs`, `appsettings.json`, Projektdateien,
   Navigation, Datenbankinitialisierung und zentrale RAG-Dokumente.
3. Keine fremden Aenderungen zuruecksetzen, ueberschreiben, formatieren oder in
   einen eigenen Commit aufnehmen.
4. Projektweite Formatierungen, Paketupdates, Migrationen, Deployments und
   App-Starts werden seriell ausgefuehrt und vorher hier angekuendigt.
5. Vollstaendige Builds und Gesamttests moeglichst nacheinander ausfuehren. Lokale,
   bereichsspezifische Tests duerfen parallel laufen.
6. Beim Abschluss Status, geaenderte Dateien und Testergebnis eintragen. Danach die
   Reservierung als frei markieren, aber den Eintrag als kurze Historie stehen lassen.
