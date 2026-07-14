# Finance B1 Journal Import (Hauptbuch-Buchungszeilen)

Stand: 2026-07-14

Zweck: Erster Load der SAP-B1-Hauptbuchdaten (Journal Entries) je Tochtergesellschaft in eine
**separate Tabelle** `FinancialJournalEntries` — Grundlage fuer Konsolidierung und Analysen gemaess
der Prioliste von Andreas/Finance. Der bestehende Sales-Datenfluss (`CentralSalesRecords`,
Audit-CSV, Finance Summary) bleibt vollstaendig unberuehrt.

## Einordnung

- Gleiche Mechanik wie gehabt (zentraler HANA-Konnektor, Standort-Zuordnung, Full Load mit
  Datumsfilter, Ersetzen je Gesellschaft), aber **eigene Tabelle** und eigener Service.
- Quelltabellen in SAP B1: `OJDT` (Journalkopf) + `JDT1` (Journalzeilen), dazu `OACT`
  (Kontobezeichnung) und `OADM` (Hauswaehrung).
- Bewusst **ohne** den IT-Umsatzkontenfilter der Sales-Strecke — das Journal ist das volle Hauptbuch.
- Aktueller Umfang: **alle B1-Gesellschaften ueber HANA** — FR/`fr01_p`, IT/`it01_p`, US/`us01_p`
  (Quellsystem `BI1`) und **Indien/`TRAFAG_LIVE`** (2026-07-14 ergaenzt).
- **Wichtig zu Indien:** Indien ist fachlich ebenfalls SAP B1, ist in der Konfiguration aber
  historisch unter dem irrefuehrenden Quellsystem-Code `SAGE` angeschrieben (eigener HANA-Server
  `20.197.20.60:30015`). Die Standortauswahl grenzt deshalb bewusst **nicht ueber den
  Quellsystem-Code** ein, sondern ueber die **Anschlussart HANA + vorhandenes Schema**
  (`FinancialJournalRefreshService.IsJournalSite`). Ob `OJDT`/`JDT1` im Schema wirklich existieren,
  prueft der Reader vor dem Lesen und meldet sonst klar statt mit rohem SQL-Fehler.
- Nicht enthalten: **CH/AT** (SAP OData/Gateway — das Hauptbuch liegt dort in `BKPF`/`BSEG` bzw.
  `ACDOCA`; braucht einen eigenen Reader **und** ein neues OData-EntitySet auf SAP-Seite, da der
  aktuelle Service nur Umsatzdaten liefert) sowie die Manual-Excel-Laender DE/UK/ES.
  Weitere ERP-Systeme sollen spaeter als eigene Konnektoren **in dieselbe Tabelle** liefern
  (`SourceSystem`-Spalte unterscheidet die Herkunft).

## Bedienung

- Seite: `Finance Cockpit > B1 Journal Import` (`/finance-journal-import`, Seed-Key
  `finance-journal-import`).
- Je Gesellschaft `Laden` oder `Alle B1-Gesellschaften laden`; die Tabelle zeigt Zeilenanzahl,
  Buchungsdatum von/bis und letzten Load.
- Zeithorizont = `ExportSettings.DateFilter` (gleicher Filter wie der Sales-Export), angewendet auf
  `OJDT.RefDate`.
- Ein Lauf **ersetzt** den Journalbestand der Gesellschaft komplett (Full Load). Guardrail: liefert
  die Quelle 0 Zeilen, bleibt ein vorhandener Bestand unveraendert (Warnung im Eventlog).
- Protokollierung ueber `AppEventLogs` (Kategorie `Journal`) — bewusst **nicht** ueber `ExportLogs`,
  damit der Daten-Heartbeat der Sales-Strecke nicht verfaelscht wird.

## Feld-Mapping (Prioliste Andreas -> Tabelle `FinancialJournalEntries`)

| Prioliste | Spalte | Quelle / Ableitung |
| --- | --- | --- |
| Gesellschaft / Company Code | `Tsc`, `Land`, `CompanySchema` | Standortstamm (`Sites`), B1-Schema z. B. `fr01_p` |
| Quellsystem | `SourceSystem` | Quellsystem-Code, aktuell `BI1` |
| Journal Entry ID | `JournalEntryId` | `OJDT.TransId` |
| Journal Entry Line ID | `JournalEntryLineId` | `JDT1.Line_ID` |
| Buchungsdatum | `PostingDate` | `OJDT.RefDate` |
| Geschaeftsjahr | `FiscalYear` | Kalenderjahr aus `RefDate` (B1-Gesellschaften = Kalenderjahr) |
| Buchungsperiode | `FiscalPeriod` | Monat aus `RefDate` |
| Lokales Sachkonto | `AccountCode` | `JDT1.Account` |
| Kontobezeichnung | `AccountName` | `OACT.AcctName` |
| Sollbetrag | `DebitAmount` | `JDT1.Debit` (lokale Waehrung) |
| Habenbetrag | `CreditAmount` | `JDT1.Credit` (lokale Waehrung) |
| Betrag mit Vorzeichen | `SignedAmountLocal` | `Debit - Credit` (Soll positiv, Haben negativ) |
| Lokale Waehrung | `LocalCurrency` | `OADM.MainCurncy` |
| Betrag in lokaler Waehrung | `SignedAmountLocal` | identisch mit Betrag mit Vorzeichen (dokumentiert) |
| Transaktionswaehrung | `TransactionCurrency` | `JDT1.FCCurrency` (leer bei reinen LC-Buchungen) |
| Betrag in Transaktionswaehrung | `SignedAmountTransaction` | `FCDebit - FCCredit` |
| Kostenstelle / Dimension 1 | `CostCenter` | `JDT1.ProfitCode` |
| Weitere Hauptdimension | `Dimension2` | `JDT1.OcrCode2` |
| Buchungstext / Line Memo | `LineMemo` | `JDT1.LineMemo` |
| Belegart / Source Transaction Type | `TransactionType` | `OJDT.TransType` (B1-ObjType, z. B. 13=AR-Rechnung, 30=manuelle Buchung) |
| Quelldokumentnummer | `SourceDocumentNumber` | `OJDT.BaseRef` |
| Manuell / automatisch | `IsManual` | `TransType = '30'` |
| Storno-/Reversal-Kennzeichen | `IsReversal` | `OJDT.StornoToTr` gesetzt oder `AutoStorno = 'Y'` |
| Extraktionszeitpunkt | `ExtractionDate` (+ `StoredAtUtc`) | `CURRENT_TIMESTAMP` der Quelle / Speicherzeitpunkt |

## Technik

| Baustein | Ort |
| --- | --- |
| Entity/Tabelle | `Models/FinancialJournalEntry.cs`, Create-SQL in `DatabaseInitializationService.SchemaSql.cs`, `EnsureFinancialJournalEntriesTable` in `DatabaseSchemaMaintenanceService` (additiv, kein Migrationsrisiko) |
| Indizes | `Tsc`, `PostingDate`, `AccountCode`; Unique `(Tsc, JournalEntryId, JournalEntryLineId)` |
| B1-Leser | `Services/HanaFinancialJournalReader.cs` (`IFinancialJournalReader`); Query/Zeilenfabrik als pure statische Methoden mit Tests |
| Orchestrierung | `Services/FinancialJournalRefreshService.cs` (`IFinancialJournalRefreshService`): B1-Standortauswahl (`IsB1JournalSite`), Server-/Credential-Aufloesung wie `HanaDataSourceAdapter`, transaktionales Ersetzen je TSC |
| UI | `Components/Pages/FinanceJournalImport.razor` |
| Tests | `TrafagSalesExporter.Tests/FinancialJournalTests.cs` (9 Tests: Vorzeichen/Periode/Manuell/Storno, Query-Aufbau, Standortauswahl, Ersetzen, 0-Zeilen-Guardrail, Nicht-B1-Ablehnung) |

## Offene Punkte / vor erstem Produktivlauf pruefen

1. Spaltenverfuegbarkeit live gegen `fr01_p` und `TRAFAG_LIVE` verifizieren (`JDT1.ProfitCode`,
   `OcrCode2`, `FCCurrency`, `OJDT.StornoToTr`, `AutoStorno`) — Namen entsprechen dem
   B1-Standardschema, wurden aber noch nicht gegen die produktiven Trafag-B1-Systeme geprobt.
   Fuer Indien zusaetzlich bestaetigen, dass `OJDT`/`JDT1` im Schema `TRAFAG_LIVE` vorhanden sind;
   der Reader bricht sonst mit klarer Meldung ab (kein Datenschaden).
2. Volumen: `JDT1` ist deutlich groesser als die Verkaufsbelege; der `DateFilter` begrenzt den
   Horizont. Falls Finance mehr Historie will, Datumsfilter bewusst setzen und Ladezeit beobachten.
3. Fachlich mit Andreas abstimmen: reicht `OcrCode2` als "weitere Hauptdimension" oder braucht es
   `OcrCode3-5`; ist `TransType` als Text-ObjType ausreichend oder soll eine Klartext-Belegart
   uebersetzt werden.
4. Geschaeftsjahr = Kalenderjahr ist fuer die B1-Gesellschaften angenommen; bei abweichenden
   Wirtschaftsjahren muesste `OFPR`/`FinncPriod` ausgewertet werden.
5. Firewall: der Webserver erreicht BI1-HANA bereits fuer die Sales-Strecke; keine neuen Ziele.
