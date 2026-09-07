# Finance Journal Import (Hauptbuch-Buchungszeilen)

Stand: 2026-08-17. Zusammengefuehrt aus `FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` und
`FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`.

Zweck: Hauptbuchdaten je Tochtergesellschaft in die **separate** Tabelle
`FinancialJournalEntries` laden, als Grundlage fuer Konsolidierung und Analysen. Der
Sales-Datenfluss (`CentralSalesRecords`, Audit-CSV, Finance Summary) bleibt vollstaendig
unberuehrt.

Zwei Quellsysteme schreiben in dieselbe Tabelle, die Spalte `SourceSystem` unterscheidet:

- **SAP B1 ueber HANA** — FR (`fr01_p`), IT (`it01_p`), US (`us01_p`), Indien (`TRAFAG_LIVE`)
- **SAP ECC ueber OData** — CH/AT (`ZSCHWEIZ`), blockiert bis SAP das EntitySet liefert

Nicht enthalten: die Manual-Excel-Laender DE, UK, ES — sie haben keine Buchhaltungsquelle.

## Einordnung

- Quelltabellen B1: `OJDT` (Kopf) und `JDT1` (Zeilen), dazu `OACT` (Kontobezeichnung) und
  `OADM` (Hauswaehrung). Quelle CH/AT: `BKPF`/`BSEG`.
- Bewusst **ohne** den IT-Umsatzkontenfilter der Sales-Strecke — das Journal ist das
  volle Hauptbuch.
- **Indien-Falle:** Indien ist fachlich SAP B1, ist in der Konfiguration aber historisch
  unter dem irrefuehrenden Quellsystem-Code `SAGE` angeschrieben. Die Standortauswahl
  grenzt deshalb bewusst **nicht ueber den Quellsystem-Code** ein, sondern ueber
  Anschlussart HANA plus vorhandenes Schema (`FinancialJournalRefreshService.IsJournalSite`).
- Ein Lauf **ersetzt** den Journalbestand der Gesellschaft komplett. Guardrail: liefert die
  Quelle 0 Zeilen, bleibt ein vorhandener Bestand unveraendert (Warnung im Eventlog).
- Protokollierung ueber `AppEventLogs`, Kategorie `Journal` — bewusst **nicht** ueber
  `ExportLogs`, damit der Daten-Heartbeat der Sales-Strecke nicht verfaelscht wird.

## Bedienung

Seite `Finance Cockpit > Journal Import` (`/finance-journal-import`, Seed-Key
`finance-journal-import`). Je Gesellschaft `Laden` oder `Alle Gesellschaften laden`.
Zeithorizont ist `ExportSettings.DateFilter`, angewendet auf `OJDT.RefDate` (B1)
beziehungsweise `Budat` (CH/AT).

## Feld-Mapping

| Bedeutung | Spalte | B1 (FR/IT/US/IN) | CH/AT (SAP ECC) |
| --- | --- | --- | --- |
| Gesellschaft | `Tsc`, `Land`, `CompanySchema`, `CompanyCode` | Standortstamm, `CompanyCode` leer | `CompanyCode` = `Bukrs`, trennt CH von AT |
| Quellsystem | `SourceSystem` | `BI1`, Indien historisch `SAGE` | `ZSCHWEIZ` |
| Journal Entry ID | `JournalEntryId` | `OJDT.TransId` | `Bukrs/Gjahr/Belnr` |
| Zeilen-ID | `JournalEntryLineId` | `JDT1.Line_ID` | `Buzei` |
| Buchungsdatum | `PostingDate` | `OJDT.RefDate` | `Budat` |
| Geschaeftsjahr | `FiscalYear` | Kalenderjahr aus `RefDate` | `Gjahr` |
| Periode | `FiscalPeriod` | Monat aus `RefDate` | `Monat` |
| Sachkonto | `AccountCode` | `JDT1.Account` | `Hkont`, fuehrende Nullen entfernt |
| Kontobezeichnung | `AccountName` | `OACT.AcctName` | `HkontTxt` aus `SKAT` |
| Soll | `DebitAmount` | `JDT1.Debit` | `Dmbtr` bei `Shkzg = 'S'` |
| Haben | `CreditAmount` | `JDT1.Credit` | `Dmbtr` bei `Shkzg = 'H'` |
| Betrag mit Vorzeichen | `SignedAmountLocal` | `Debit - Credit` | `Dmbtr`, Soll positiv |
| Lokale Waehrung | `LocalCurrency` | `OADM.MainCurncy` | `Hwaer` |
| Transaktionswaehrung | `TransactionCurrency` | `JDT1.FCCurrency` | `Waers`, leer wenn = `Hwaer` |
| Betrag in Transaktionswaehrung | `SignedAmountTransaction` | `FCDebit - FCCredit` | `Wrbtr` mit Vorzeichen |
| Kostenstelle | `CostCenter` | `JDT1.ProfitCode` | `Kostl` |
| Weitere Dimension | `Dimension2` | `JDT1.OcrCode2` | `Prctr` (Profitcenter) |
| Buchungstext | `LineMemo` | `JDT1.LineMemo` | `Sgtxt` |
| Belegart | `TransactionType` | `OJDT.TransType` (13 = AR-Rechnung, 30 = manuell) | `Blart` (`SA`, `RV`, `KR`) |
| Quelldokument | `SourceDocumentNumber` | `OJDT.BaseRef` | `Xblnr` |
| Manuell | `IsManual` | `TransType = '30'` | `Blart = 'SA'` (**Annahme**) |
| Storno | `IsReversal` | `StornoToTr` gesetzt oder `AutoStorno = 'Y'` | `Stblg` gesetzt |

`JournalEntryId` fuer CH/AT ist zusammengesetzt, weil die Belegnummer erst mit
Buchungskreis und Geschaeftsjahr eindeutig ist.

## Technik

| Baustein | Ort |
| --- | --- |
| Entity/Tabelle | `Models/FinancialJournalEntry.cs`, Create-SQL in `DatabaseInitializationService.SchemaSql.cs` |
| Indizes | `Tsc`, `PostingDate`, `AccountCode`; Unique `(Tsc, JournalEntryId, JournalEntryLineId)` |
| B1-Leser | `Services/HanaFinancialJournalReader.cs`, prueft vorab ueber `sys.tables`, ob `OJDT`/`JDT1` existieren |
| CH/AT-Leser | `Services/SapGatewayFinancialJournalReader.cs`, EntitySet `FinanzJournalSet`, Paging in 1000er-Seiten |
| Orchestrierung | `Services/FinancialJournalRefreshService.cs` |
| UI | `Components/Pages/FinanceJournalImport.razor` |
| Tests | `TrafagSalesExporter.Tests/FinancialJournalTests.cs` |

## SAP-Anforderung: EntitySet `FinanzJournalSet` (offen)

Zielgruppe SAP-/ABAP-Team, Service-Owner von `ZPOWERBI_EINKAUF_SRV`. Die App-Seite ist
umgesetzt und deployed; der Load funktioniert, sobald das EntitySet verfuegbar ist.

**Anforderungen**

- Name `FinanzJournalSet`, fest hinterlegt in
  `SapGatewayFinancialJournalReader.JournalEntitySet`.
- Idealerweise derselbe Service, auf den der `ZSCHWEIZ`-Standort zeigt, damit URL und
  Berechtigungen unveraendert bleiben.
- Eine Zeile je FI-Belegzeile (`BKPF` x `BSEG`), beide Buchungskreise, **alle Konten**.
- `$filter` auf `Budat`, `$top`/`$skip`/`$orderby` (`Bukrs,Gjahr,Belnr,Buzei`) wie beim
  bestehenden `FinanzdataSchweizOeSet`.
- Stornierte Belege **nicht** herausfiltern, die App kennzeichnet sie ueber `Stblg`.

**Felddefinition**

| Property | SAP-Feld | Typ | Bedeutung |
| --- | --- | --- | --- |
| `Bukrs` | BKPF-BUKRS | CHAR 4 | Buchungskreis, trennt CH und AT |
| `Belnr` | BKPF-BELNR | CHAR 10 | Belegnummer |
| `Gjahr` | BKPF-GJAHR | NUMC 4 | Geschaeftsjahr |
| `Buzei` | BSEG-BUZEI | NUMC 3 | Belegzeile |
| `Budat` | BKPF-BUDAT | DATS | Buchungsdatum, Filterfeld |
| `Monat` | BKPF-MONAT | NUMC 2 | Buchungsperiode |
| `Blart` | BKPF-BLART | CHAR 2 | Belegart |
| `Xblnr` | BKPF-XBLNR | CHAR 16 | Referenzbelegnummer |
| `Stblg` | BKPF-STBLG | CHAR 10 | Storno-Belegnummer, leer = kein Storno |
| `Hwaer` | BKPF-HWAER | CUKY | Hauswaehrung |
| `Waers` | BKPF-WAERS | CUKY | Belegwaehrung |
| `Hkont` | BSEG-HKONT | CHAR 10 | Sachkonto |
| `HkontTxt` | SKAT-TXT50 | CHAR 50 | Kontobezeichnung, Sprache DE, Fallback EN |
| `Shkzg` | BSEG-SHKZG | CHAR 1 | Soll/Haben |
| `Dmbtr` | BSEG-DMBTR | CURR | Betrag in Hauswaehrung |
| `Wrbtr` | BSEG-WRBTR | CURR | Betrag in Belegwaehrung |
| `Kostl` | BSEG-KOSTL | CHAR 10 | Kostenstelle |
| `Prctr` | BSEG-PRCTR | CHAR 10 | Profitcenter |
| `Sgtxt` | BSEG-SGTXT | CHAR 50 | Buchungstext |

Bei S/4 kann `ACDOCA` als Quelle dienen; Property-Namen und Bedeutungen muessen gleich
bleiben. Zahlen als String und Datum als OData-`/Date(...)/` sind ok, die App parst
invariant.

**ABAP-Skizze**

```abap
METHOD finanzjournalset_get_entityset.
  " $filter (Budat ge ...), $top/$skip aus io_tech_request_context uebernehmen.
  SELECT k~bukrs k~belnr k~gjahr s~buzei k~budat k~monat k~blart k~xblnr k~stblg
         k~hwaer k~waers s~hkont t~txt50 AS hkont_txt s~shkzg s~dmbtr s~wrbtr
         s~kostl s~prctr s~sgtxt
    INTO CORRESPONDING FIELDS OF TABLE et_entityset
    FROM bkpf AS k
    INNER JOIN bseg AS s
      ON s~bukrs = k~bukrs AND s~belnr = k~belnr AND s~gjahr = k~gjahr
    LEFT OUTER JOIN skat AS t
      ON t~saknr = s~hkont AND t~ktopl = 'TRAG' AND t~spras = 'D'
    WHERE k~bukrs IN ( '....CH....', '....AT....' )
      AND k~budat >= lv_budat_von
    ORDER BY k~bukrs k~gjahr k~belnr s~buzei.
ENDMETHOD.
```

Grosse Selektionen bitte per Paket-Select statt Full-Table-Scan auf `BSEG`.

**Abnahme**

1. `GET .../FinanzJournalSet?$format=json&$top=5` liefert alle Properties.
2. `$filter=Budat ge datetime'2025-01-01T00:00:00'` grenzt korrekt ein.
3. Zeilenzahl je Buchungskreis plausibel gegen SE16.
4. In der App: `Journal Import > Schweiz/Oesterreich > Laden` meldet Erfolg mit Zeilenzahl.

## Offene Punkte

1. **CH/AT blockiert**, bis `FinanzJournalSet` auf `travp762` bereitsteht. Bis dahin
   meldet ein Ladeversuch klar „EntitySet fehlt"; alle anderen Gesellschaften laden normal.
2. Fachlich mit Andreas: reicht `IsManual = Blart 'SA'`, oder gelten weitere Belegarten als
   manuell? Genuegt Profitcenter als weitere Hauptdimension, oder wird Segment gewuenscht?
   Reicht `OcrCode2` bei B1 oder braucht es `OcrCode3-5`?
3. Spaltenverfuegbarkeit live gegen `fr01_p` und `TRAFAG_LIVE` verifizieren
   (`JDT1.ProfitCode`, `OcrCode2`, `FCCurrency`, `OJDT.StornoToTr`, `AutoStorno`).
4. Volumen: `JDT1` ist deutlich groesser als die Verkaufsbelege. Bei mehr Historie den
   Datumsfilter bewusst setzen und Ladezeit beobachten.
5. Geschaeftsjahr = Kalenderjahr ist fuer die B1-Gesellschaften **angenommen**; bei
   abweichenden Wirtschaftsjahren muesste `OFPR`/`FinncPriod` ausgewertet werden.

## Querverweise

- B1-Anbindung der Verkaufsstrecke: `docs/QUELLSYSTEME_SAP_B1.md`
- ABAP-Analysereport Standardpreis und Journal: `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md`
