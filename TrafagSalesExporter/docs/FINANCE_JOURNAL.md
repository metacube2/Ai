# Finance Journal Import (Hauptbuch-Buchungszeilen)

Stand: 2026-09-09. Zusammengefuehrt aus `FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` und
`FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md`.

Zweck: Hauptbuchdaten je Tochtergesellschaft in die **separate** Tabelle
`FinancialJournalEntries` laden, als Grundlage fuer Konsolidierung und Analysen. Der
Sales-Datenfluss (`CentralSalesRecords`, Audit-CSV, Finance Summary) bleibt vollstaendig
unberuehrt.

Zwei Quellsysteme schreiben in dieselbe Tabelle, die Spalte `SourceSystem` unterscheidet:

- **SAP B1 ueber HANA** — FR (`fr01_p`), IT (`it01_p`), US (`us01_p`), Indien (`TRAFAG_LIVE`)
- **SAP ECC ueber OData** — CH/AT (`ZSCHWEIZ`). Das benoetigte Hauptbuch-EntitySet ist
  in P76 noch nicht vorhanden; `FinanzdataSchweizOeSet` wurde live als Verkaufsdatenquelle
  identifiziert. Der Journal-EntitySet-Name ist am Standort konfigurierbar.

Nicht enthalten: die Manual-Excel-Laender DE, UK, ES — sie haben keine Buchhaltungsquelle.

## Einordnung

- Quelltabellen B1: `OJDT` (Kopf) und `JDT1` (Zeilen), dazu `OACT` (Kontobezeichnung),
  `OADM` (Hauswaehrung) sowie seit dem 2026-09-09 `ITR1`/`OITR` fuer den Ausgleich.
  Quelle CH/AT: `BKPF`/`BSEG`.
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
| Faelligkeitsdatum | `DueDate` | `JDT1.DueDate` (produktiv) | `Faedt`, berechnetes Nettofaelligkeitsdatum |
| Ausgleichsdatum der Zeile | `ClearingDate` | `JDT1.MthDate` | `Augdt`, Initialwerte werden zu leer |
| Datum des juengsten Ausgleichs | `ReconciliationDate` | `MAX(OITR.ReconDate)` ueber `ITR1` | nicht vorhanden |
| Ausgleichsnummer | `ClearingReference` | `MAX(OITR.ReconNum)` ueber `ITR1` | `Augbl` |
| Anzahl Ausgleiche | `ClearingCount` | `COUNT(*)` ueber `ITR1` | 0 oder 1 |
| Ausgleich storniert | `IsClearingCancelled` | `OITR.Canceled` in `Y`/`C` | nicht vorhanden |
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
| CH/AT-Leser | `Services/SapGatewayFinancialJournalReader.cs`, EntitySet aus `Sites.SapEntitySet`, Fallback `FinanzJournalSet`, Paging in 20000er-Seiten je Buchungsperiode (`Gjahr` plus `Monat`) |
| Orchestrierung | `Services/FinancialJournalRefreshService.cs` |
| UI | `Components/Pages/FinanceJournalImport.razor` |
| Tests | `TrafagSalesExporter.Tests/FinancialJournalTests.cs` |

## SAP-Anforderung: vollstaendiges Journal-EntitySet

Zielgruppe SAP-/ABAP-Team, Service-Owner von `ZPOWERBI_EINKAUF_SRV`. Der Live-Abgleich
vom 2026-09-08 zeigt, dass noch kein vorhandenes EntitySet diese Spezifikation erfuellt.

**Anforderungen**

- Name vorzugsweise `FinanzJournalSet`; ein anderer Name kann am Standort in
  `Sites.SapEntitySet` gepflegt werden. Vor jedem Datenabruf prueft die App im `$metadata`,
  ob alle Pflicht-Properties vorhanden sind.
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
| `Faedt` | aus BSEG-Zahlungsbedingungen berechnet | DATS | Nettofaelligkeitsdatum; nicht das Basisdatum `ZFBDT` |

Bei S/4 kann `ACDOCA` als Quelle dienen; Property-Namen und Bedeutungen muessen gleich
bleiben. Zahlen als String und Datum als OData-`/Date(...)/` sind ok, die App parst
invariant.

`Faedt` ist kein unveraendert zu kopierendes BSEG-Datenbankfeld. SAP muss das
Nettofaelligkeitsdatum mit der Standardlogik aus Basisdatum und Zahlungsbedingungen
ermitteln (zum Beispiel `DETERMINE_DUE_DATE`, Ergebnis `NETDT`). Ein leeres Datum ist bei
fachlich nicht faelligen Sachkontenzeilen erlaubt; die Property selbst ist Pflicht. Ein
spaeter benoetigtes Ausgleichsdatum waere separat `Augdt` und darf nicht mit `Faedt`
vermischt werden.

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

Nach dem paketierten Select `Faedt` je Zeile mit der SAP-Standard-Faelligkeitslogik
ermitteln und in die OData-Property uebertragen.

Grosse Selektionen bitte per Paket-Select statt Full-Table-Scan auf `BSEG`.

**Abnahme**

1. `GET .../FinanzJournalSet?$format=json&$top=5` liefert alle Properties.
2. `$filter=Budat ge datetime'2025-01-01T00:00:00'` grenzt korrekt ein.
3. Zeilenzahl je Buchungskreis plausibel gegen SE16.
4. In der App: `Journal Import > Schweiz/Oesterreich > Laden` meldet Erfolg mit Zeilenzahl.

## Offene Punkte

1. **CH/AT: das EntitySet steht auf dem Testsystem, produktiv fehlt es noch.** **NACHTRAG 2026-09-11:** `FinanzJournalSet` ist auf `T76/100` gebaut, aktiv und geprueft: beide Redefinitionen ueber RFC gegengelesen, `$metadata` und Datenabruf `HTTP 200`, Werte gegen `BKPF`/`BSEG`/`SKAT` verglichen. Der Leser laedt je Buchungsperiode mit `$top=20000`, ein Jahreslauf dauert damit rund zwei Minuten statt der gemessenen 80. **Produktiv aendert das noch nichts:** Transport `T76K912530` ist nicht freigegeben, die App liest `travp762`. Bau, Messungen und Fallen: `docs/abap/README_FIN_JOURNAL_ENTITYSET.md`. 1. **CH/AT wartet auf ein SAP-Journal-EntitySet.** **NACHTRAG 2026-09-10:** Die Vorpruefung ist erledigt und in `docs/abap/README_FIN_JOURNAL_ENTITYSET.md` dokumentiert. Alle 25 DDIC-Felder existieren, Buchungskreise sind `1100` CH/CHF und `1200` AT/EUR bei Kontenplan `1000`. Fachliche Vorgabe Andreas vom selben Tag: **zuerst nur Oesterreich**, weil die Schweiz ueber 5 Mio Zeitbuchungszeilen je Jahr hat. Fuer AT 2025 bis 2026 ohne CO-Belege sind es 6'275 Belegkoepfe und 17'364 Positionen. **Wichtigster Messbefund: `PRCTR` ist zu 0 Prozent gefuellt**, `AUGDT` und `AUGBL` zu 22,1 Prozent, `ZFBDT` als Basis fuer das Faelligkeitsdatum zu 20,8 Prozent; alle 98 bebuchten Sachkonten haben einen `SKAT`-Text. Pruefreport `ZFIN_JOURNAL_PRUEFUNG` liegt aktiv in Paket `ZPP`, Transport `T76K912530`, nicht freigegeben. Urspruenglicher Text: Live-$metadata aus P76 am 2026-09-08:
   `FinanzdataSchweizOeSet` ist Verkaufs-/Fakturadaten mit `Vbeln`, `Posnr`, `Matnr` und
   `NetwrDc`; Journalfelder wie `Belnr`, `Buzei`, `Hkont`, `Dmbtr`, `Shkzg` und `Faedt`
   fehlen. `bkpfSet` enthaelt nur Kopfdaten. `bsisSet` enthaelt nur offene
   Sachkontenposten und zudem weder `Dmbtr`, `Shkzg`, Kontotext, Profitcenter,
   Buchungstext noch Faelligkeitsdatum. Keines der drei Sets darf als Volljournal
   angebunden werden. App-seitig sind ein konfigurierbarer EntitySet-Name, eine strikte
   Pflichtfeldpruefung und das `Faedt`-Mapping umgesetzt. SAP muss jetzt das oben
   spezifizierte EntitySet liefern; danach `Sites.SapEntitySet` pflegen und CH/AT laden.
2. Fachlich mit Andreas: reicht `IsManual = Blart 'SA'`, oder gelten weitere Belegarten als
   manuell? Genuegt Profitcenter als weitere Hauptdimension, oder wird Segment gewuenscht?
   Reicht `OcrCode2` bei B1 oder braucht es `OcrCode3-5`?
3. **Dimensionspruefung erledigt am 2026-09-08:** `ProfitCode` und `OcrCode2-5` existieren
   in FR/IT/US/IN und sind ab 2025 vollstaendig leer. Dimension 2-5 sind in `ODIM` inaktiv.
   Ein anderes Dimensionsfeld loest die Luecke nicht; keine Ersatzwerte erfinden.
4. Volumen: `JDT1` ist deutlich groesser als die Verkaufsbelege. Bei mehr Historie den
   Datumsfilter bewusst setzen und Ladezeit beobachten.
5. Geschaeftsjahr = Kalenderjahr ist fuer die B1-Gesellschaften **angenommen**; bei
   abweichenden Wirtschaftsjahren muesste `OFPR`/`FinncPriod` ausgewertet werden.

6. **Zielbild Andreas vom 2026-09-08 fuer die Konsolidierung:** Das B1-Faelligkeitsdatum
   ist produktiv und die vier B1-Gesellschaften wurden neu geladen. Konzernkonto-Mapping fehlt
   fachlich weiterhin. Die verdichtete Ein-Zeilen-Darstellung traegt nur bei
   zweizeiligen Buchungen. Einzelheiten in
   `docs/FINANCE_JOURNAL_KONSOLIDIERUNG_ANDREAS_2026-09-08.md`.

## Live-Feldpruefung und Umsetzung vom 2026-09-08

Direkter HANA-Zugriff mit der produktiven Standortkonfiguration, ausschliesslich SELECT.
Grundgesamtheit: `OJDT.RefDate >= 2025-01-01`; kein Kontenfilter. Auch Indien war vom
Entwicklungsrechner erreichbar. Werkzeug und aggregierter Nachweis:
`.tmp_tools/JournalFieldProbe0908/` (`Program.cs`, `details.sql`, `results.txt`).

| TSC | Journalzeilen | DueDate gefuellt | ProfitCode / OcrCode2-5 jeweils gefuellt | Zeilen-DueDate anders als Kopf-DueDate |
| --- | ---: | ---: | ---: | ---: |
| TRFR | 20'170 | 20'170 | 0 | 87 |
| TRIT | 162'724 | 162'724 | 0 | 16'551 |
| TRUS | 21'770 | 21'770 | 0 | 54 |
| TRIN | 264'997 | 264'997 | 0 | 16 |

Damit muss **JDT1.DueDate** gelesen werden, nicht das Kopfdatum `OJDT.DueDate` und nicht
das Buchungsdatum. Die Quelle hat 469'661 Zeilen, etwas mehr als der zuvor geladene
Export (469'629), weil Indien waehrend des Tages weiterbucht. Das ist kein neuer Import.

Dimension 1 ist in allen vier Gesellschaften aktiv, aber ohne Buchungswerte; FR/IT nennen
sie in der Beschreibung `Business Unit`. Dimension 2-5 sind ueberall inaktiv.
`JDT1.U_CTX_OCRCD` ist in FR/IT ebenfalls leer. `OACT.ExportCode` ist in allen vier
Kontenstaemmen leer; daraus entsteht kein Konzernkonten-Mapping. Das indische
`OACT.U_PROFIT` hat sieben Stammsaetze, ist aber kein belegtes Buchungsdimension-Mapping.

**Produktiv seit 2026-09-08:** nullable `DueDate` im Modell und im Create-SQL,
additiver Nachzug per Schema-Maintenance, HANA-Reader und `Finance_All` durchverbunden.
Alle vier B1-Gesellschaften wurden danach neu geladen. EF und der Refresh speichern das
neue Modellfeld ohne separate Sonderbehandlung. CH/AT wartet auf das SAP-EntitySet oben.
`Finance_All` kann alte Datenbanken weiterhin lesen und zeigt die tatsaechliche
Feldbelegung im neuen Blatt `Feldstatus`, immer ueber den vollstaendigen Journalbestand.

**Zahlungsdatum / Clearing Date — Erhebung vom 2026-09-08, teilweise ueberholt:** live in
allen vier B1-Schemata vorhanden und belegt: `OITR.ReconDate` (interner Ausgleich),
`ORCT.DocDate` (Zahlungseingang), `OVPM.DocDate` (Zahlungsausgang) sowie `TrsfrDate` fuer
den jeweiligen Ueberweisungsweg. Zahlungsbelege und Ausgleichshistorie wurden ueber ihre
gesamte Historie gezaehlt, nicht nur ab 2025. `JDT1.MthDate` ist ebenfalls teilweise
gefuellt (FR 5'680, IT 64'672, US 9'892, IN 98'778 Journalzeilen ab 2025).
Ein Ausgleich kann aus Teilzahlung, Gutschrift oder manueller Zuordnung entstehen;
auch stornierte Ausgleiche sind vorhanden. Deshalb vor Umsetzung mit Andreas unterscheiden:
Zahlungsbuchungsdatum, Ueberweisungsdatum, letzter Ausgleich oder vollstaendig ausgeglichen.
**UEBERHOLT ist der letzte Satz dieses Absatzes vom 2026-09-08**, die Felder seien nicht
eingebaut worden: die Ausgleichsfelder sind am 2026-09-09 aufgenommen worden, siehe den
folgenden Abschnitt. Unveraendert gilt, dass **kein** abgeleitetes `date paid` eingebaut
wurde; die Bedeutungsfrage ist weiterhin offen.
SAP-Referenzen: [OITR](https://help.sap.com/doc/089315d8d0f8475a9fc84fb919b501a3/10.0/en-US/SDKHelp/OITR.html),
[Payments.DocDate](https://help.sap.com/doc/089315d8d0f8475a9fc84fb919b501a3/10.0/en-US/SDKHelp/SAPbobsCOM~Payments~DocDate.html),
[Teil- und Vollausgleich](https://help.sap.com/docs/SAP_BUSINESS_ONE/68a2e87fb29941b5bf959a184d9c6727/44f3e8dfc4b80486e10000000a155369.html).

### Ausgleichsfelder, eingebaut am 2026-09-09 (ISS-006.1)

Auftrag von Ingo: „kannst du die felder einbauen, in Finance_All waere ja korrekt oder,
dann haben wir die felder auch drin beim import klick". Aufgenommen sind fuenf Felder je
Buchungszeile, die die Quelle **abbilden**; eine fachliche Auswahl findet bewusst nicht
statt.

| Feld im Modell | B1-Quelle | SAP ECC (CH/AT) | Bedeutung |
| --- | --- | --- | --- |
| `ClearingDate` | `JDT1.MthDate` | `BSEG-AUGDT` | Ausgleichsdatum der Buchungszeile |
| `ReconciliationDate` | `MAX(OITR.ReconDate)` ueber ITR1 | — | Datum des juengsten Ausgleichsvorgangs |
| `ClearingReference` | hoechste `OITR.ReconNum` **am** `ReconciliationDate` | `BSEG-AUGBL` | Nummer genau dieses Ausgleichs |
| `ClearingCount` | `COUNT(*)` ueber ITR1 | 0 oder 1 | Anzahl der Ausgleichsvorgaenge |
| `IsClearingCancelled` | `OITR.Canceled` in `Y`/`C` | — | mindestens ein Ausgleich storniert |

**Zwei Messbefunde vom 2026-09-09 haben die Umsetzung bestimmt.** Beide sind direkt auf
den produktiven B1-Schemata erhoben, ausschliesslich lesend.

1. **`JDT1.IntrnMatch` ist unbrauchbar.** Der naheliegende Weg, die Ausgleichsnummer
   direkt aus der Buchungszeile zu nehmen, faellt aus: das Feld ist in FR, IT und US in
   **jeder** Zeile `0` oder `-1`, also nie gepflegt (FR 0 von 20'198, IT 0 von 162'821,
   US 0 von 21'816 echte Werte). Ein `LEFT JOIN OITR ON ReconNum = IntrnMatch` haette
   syntaktisch funktioniert und dauerhaft leere Spalten geliefert. Die tragfaehige
   Bruecke ist `ITR1.TransId`/`ITR1.TransRowId` gegen `JDT1.TransId`/`JDT1.Line_ID`.
2. **Eine Buchungszeile kann mehrfach ausgeglichen werden.** Betroffen sind FR 56, IT 559
   und US 324 Zeilen, mit bis zu **neun** Ausgleichsvorgaengen je Zeile. Ein direkter
   `JOIN` ueber `ITR1` haette diese Zeilen vervielfacht und damit den Journalsaldo
   verfaelscht — derselbe Fehler, der beim spanischen Buchungsdatum durch `OUTER APPLY`
   statt `JOIN` vermieden wurde (`docs/FINANCE_ES_BUCHUNGSDATUM_2026-08-03.md`). Deshalb
   wird in einer Unterabfrage nach `TransId`/`TransRowId` aggregiert, bevor verknuepft
   wird. `ClearingCount` macht den Mehrfachausgleich in der Auswertung sichtbar, statt ihn
   hinter einem einzelnen Datum verschwinden zu lassen.
3. **Die hoechste Ausgleichsnummer ist nicht die des juengsten Ausgleichs.** Der erste
   Entwurf nahm `MAX(ReconNum)` und `MAX(ReconDate)` in derselben Aggregation und haette
   damit Nummer und Datum zweier **verschiedener** Ausgleiche nebeneinandergestellt.
   Gemessen betrifft das FR 374 von 1'412, IT 77 von 5'834 und US 49 von 332 mehrfach
   ausgeglichene Zeilen; `ReconNum` wird also nicht in Datumsreihenfolge vergeben. Die
   Unterabfrage laeuft deshalb zweistufig: erst Datum und Anzahl je Zeile, dann die
   hoechste Nummer genau an diesem Datum.

**Gegenprobe auf Produktivdaten.** Die unveraenderte produktive Reader-Query wurde live
ausgefuehrt und ihre Zeilenzahl gegen die Basisabfrage ohne Ausgleichs-Join gezaehlt:

| TSC | Basis ohne Join | produktive Query | `ClearingDate` | `ReconciliationDate` | mehrfach ausgeglichen | Ausgleich storniert |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| TRFR | 20'198 | 20'198 | 5'680 | 5'680 | 56 | 16 |
| TRIT | 162'833 | 162'833 | 64'677 | 64'609 | 559 | 267 |
| TRUS | 21'816 | 21'816 | 9'902 | 8'289 | 324 | 257 |

Die Zeilenzahl ist in allen drei Gesellschaften identisch; die Erweiterung vervielfacht
also keine Zeile.

Die Gegenprobe ist nach der zweistufigen Korrektur aus Befund 3 **wiederholt** worden und
hat dasselbe Ergebnis: `basis = produktiv` bei TRFR 20'198, TRIT 162'843 und TRUS 21'816,
und die abgeleiteten Zahlen (`ClearingDate`, `ReconciliationDate`, mehrfach ausgeglichen,
storniert) sind Wert fuer Wert unveraendert. Die Korrektur hat also ausschliesslich die
Zuordnung von Nummer zu Datum geaendert und nichts an der Zeilenmenge. Dass TRIT zwischen
den beiden Laeufen von 162'833 auf 162'843 gewachsen ist, liegt am laufenden Tagesgeschaeft
und ist kein Effekt der Query.

Diese Wiederholung war notwendig und kein Formalismus: der erste Versuch der zweistufigen
Fassung scheiterte auf HANA mit `invalid column name REC.last_recon_date`, weil
unquotierte Aliase grossgeschrieben werden — waehrend alle 690 Unit-Tests gruen waren. Die
Tests pruefen nur Teilzeichen der Query und koennen eine solche Laufzeitfrage
grundsaetzlich nicht entscheiden. Werkzeug: `.tmp_tools/JournalClearingProbe0909`, das die
unveraenderte produktive Query ausfuehrt und gegen eine Basisabfrage ohne Ausgleichs-Join
zaehlt; es schreibt weder in die App-Datenbank noch in das Ereignisprotokoll.

**Indien ist weiterhin ungemessen.** Der Standort war am 2026-09-09 auch bei aktiver VPN
nicht erreichbar: weder Ping noch TCP auf `30015` oder `30013` (unabhaengig mit
`Test-NetConnection` gegen `20.197.20.60` geprueft), waehrend FR, IT und US ueber dieselbe
Verbindung antworteten. Am 2026-09-08 war Indien noch erreichbar. Es handelt sich also um
eine Stoerung auf indischer Seite oder auf der Route dorthin, nicht um einen Befund zu den
Ausgleichsfeldern. Die Belegung dort ist beim naechsten erreichbaren Fenster nachzumessen.

Dass `ClearingDate` und `ReconciliationDate` **beide** gefuehrt werden, ist gemessen und
nicht kosmetisch: in den USA tragen 1'613 Zeilen ein `MthDate` ohne zugehoerigen
Ausgleichssatz, in Italien 68; umgekehrt weichen die Daten dort, wo beide gefuellt sind,
in FR 5, IT 13 und US 45 Zeilen voneinander ab.

**Was bewusst nicht eingebaut wurde.** Kein abgeleitetes `date paid`, und keine Anbindung
der Zahlungsbelege `ORCT`/`OVPM`. Ein Ausgleich ist nicht dasselbe wie eine Zahlung, und
welche der vier Bedeutungen fuehrend werden soll, ist eine Fachfrage an Andreas. Solange
sie offen ist, zeigt `Finance_All` die Quelllage und benennt im Lesehinweis ausdruecklich,
dass eine leere Zelle „nicht (oder noch nicht) ausgeglichen" heisst und nicht „nicht
geladen".

**Wirksamkeit.** Wie beim Faelligkeitsdatum sind die Spalten erst nach einem erneuten
Journal-Ladelauf je Gesellschaft gefuellt; das Blatt `Feldstatus` in `Finance_All` weist
die tatsaechliche Belegung aus. Der Schemanachzug ist additiv, alte Produktivstaende
bleiben lesbar. CH/AT liest `BSEG-AUGDT`/`AUGBL` optional: die Felder stehen **nicht** in
`RequiredFields`, damit ein EntitySet ohne sie den CH/AT-Import nicht abbricht, sondern
den Ausgleich leer laesst. `ReconciliationDate`, `ClearingCount` und `IsClearingCancelled`
bleiben fuer CH/AT bauartbedingt leer, weil ECC keine Ausgleichshistorie wie `OITR` fuehrt;
das ist im Lesehinweis von `Finance_All` ausdruecklich vermerkt, damit es nicht als
fehlende Ladung missverstanden wird.

**Bewusste Ausweitung:** `ParseSapDate` filtert die SAP-Initialwerte (`00000000`,
`0001-01-01`, `1753-01-01`) heraus und gibt dafuer `null` zurueck. Das wirkt auf **alle**
Datumsfelder des CH/AT-Lesers, also auch auf `Budat` und `Faedt`, nicht nur auf das neue
`Augdt`. Das ist beabsichtigt: ein Initialwert ist in keinem der drei Felder ein Datum,
und bei einer Position ohne Faelligkeit waere zuvor der 01.01.0001 als `DueDate` gelandet.

Validierung: 690/690 Release-Tests, darin die neuen Journaltests zu Mehrfachausgleich,
Storno, B1-Platzhaltern und additivem Schemanachzug; Python-Exportpruefung mit alter und
neuer Datenbank bestanden. Keine Produktivdaten geaendert, kein Deploy.

Validierung: 675/675 Release-Tests, darin 14/14 gezielte Journaltests mit idempotenter Schemaerweiterung,
Erhalt alter Zeilen und Persistenz beim Refresh; Python-Exportpruefung mit alter und neuer
Datenbank bestanden (Datum, fehlendes Mapping, gekuerztes Detail, vollstaendiger Feldstatus).
Keine Produktivdaten geaendert, kein App-Start, kein Deploy.

## Querverweise

- B1-Anbindung der Verkaufsstrecke: `docs/QUELLSYSTEME_SAP_B1.md`
- ABAP-Analysereport Standardpreis und Journal: `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md`
