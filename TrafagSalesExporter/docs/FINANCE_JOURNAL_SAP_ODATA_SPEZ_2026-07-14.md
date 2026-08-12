# SAP OData Spezifikation: FinanzJournalSet (Hauptbuch CH/AT fuer Journal Import)

Stand: 2026-07-14
Zielgruppe: SAP-/ABAP-Team (Lucas bzw. OData-Service-Owner von `ZPOWERBI_EINKAUF_SRV`)
App-Seite: bereits umgesetzt und deployed (`SapGatewayFinancialJournalReader`); der Load
funktioniert, sobald das EntitySet im Service verfuegbar ist.

## 1. Zweck

Das BiDashboard laedt Hauptbuch-Buchungszeilen je Gesellschaft in die separate Tabelle
`FinancialJournalEntries` (Konsolidierung/Analysen, Prioliste Andreas/Finance). Fuer die
B1-Gesellschaften (FR/IT/US/IN) liest die App direkt `OJDT`/`JDT1` aus HANA. Fuer CH/AT
(`ZSCHWEIZ`, SAP ECC) gibt es keine B1-Tabellen — das Hauptbuch liegt in `BKPF`/`BSEG`.
Der bestehende Z-Service liefert bisher nur Umsatzdaten (`FinanzdataSchweizOeSet`);
es braucht ein **neues EntitySet** mit den FI-Belegzeilen.

## 2. Anforderungen an das EntitySet

- **Name:** `FinanzJournalSet` (fest im App-Code hinterlegt,
  `SapGatewayFinancialJournalReader.JournalEntitySet`).
- **Service:** idealerweise derselbe Service, auf den der `ZSCHWEIZ`-Standort zeigt
  (aktuell `ZPOWERBI_EINKAUF_SRV` auf `travp762`), damit URL und Berechtigungen
  unveraendert bleiben. Ein separater Service ginge auch, dann muss die Site-URL angepasst werden.
- **Granularitaet:** eine Zeile pro FI-Belegzeile (`BKPF` x `BSEG`), beide Buchungskreise
  (CH und AT), alle Konten — kein Kontenfilter, das Journal ist bewusst das volle Hauptbuch.
- **Query-Optionen:** `$filter` auf `Budat` (`ge datetime'...'`), `$top`/`$skip`/`$orderby`
  (`Bukrs,Gjahr,Belnr,Buzei`) — die App liest gebatcht mit 1000er-Seiten (SAP-Seitenlimit).
- **Storno:** stornierte Belege NICHT herausfiltern; die App kennzeichnet sie ueber `Stblg`.

## 3. Felddefinition

| OData-Property | SAP-Feld | Typ | Bedeutung |
| --- | --- | --- | --- |
| `Bukrs` | BKPF-BUKRS | CHAR 4 | Buchungskreis (trennt CH und AT) |
| `Belnr` | BKPF-BELNR | CHAR 10 | Belegnummer |
| `Gjahr` | BKPF-GJAHR | NUMC 4 | Geschaeftsjahr |
| `Buzei` | BSEG-BUZEI | NUMC 3 | Belegzeile |
| `Budat` | BKPF-BUDAT | DATS | Buchungsdatum (Filterfeld) |
| `Monat` | BKPF-MONAT | NUMC 2 | Buchungsperiode |
| `Blart` | BKPF-BLART | CHAR 2 | Belegart (z. B. RV, KR, SA) |
| `Xblnr` | BKPF-XBLNR | CHAR 16 | Referenzbelegnummer (Drill-down zum Quellbeleg) |
| `Stblg` | BKPF-STBLG | CHAR 10 | Storno-Belegnummer (leer = kein Storno) |
| `Hwaer` | BKPF-HWAER / T001-WAERS | CUKY | Hauswaehrung des Buchungskreises |
| `Waers` | BKPF-WAERS | CUKY | Belegwaehrung |
| `Hkont` | BSEG-HKONT | CHAR 10 | Sachkonto |
| `HkontTxt` | SKAT-TXT50 (SPRAS = DE) | CHAR 50 | Kontobezeichnung |
| `Shkzg` | BSEG-SHKZG | CHAR 1 | Soll/Haben-Kennzeichen (S/H) |
| `Dmbtr` | BSEG-DMBTR | CURR | Betrag in Hauswaehrung |
| `Wrbtr` | BSEG-WRBTR | CURR | Betrag in Belegwaehrung |
| `Kostl` | BSEG-KOSTL | CHAR 10 | Kostenstelle |
| `Prctr` | BSEG-PRCTR | CHAR 10 | Profitcenter (weitere Hauptdimension) |
| `Sgtxt` | BSEG-SGTXT | CHAR 50 | Buchungstext / Line Memo |

Hinweise:
- Bei S/4-Systemen kann statt `BKPF`/`BSEG` auch `ACDOCA` als Quelle dienen; die
  Property-Namen und Bedeutungen muessen gleich bleiben.
- `HkontTxt` bevorzugt Sprache DE, Fallback EN.
- Zahlen als String im OData-JSON sind ok (die App parst invariant).
- Datumswerte kommen als OData-`/Date(...)/;` das parst die App ebenfalls.

## 4. Was die App daraus macht (zur Einordnung)

| Zielfeld | Ableitung |
| --- | --- |
| `JournalEntryId` | `Bukrs/Gjahr/Belnr` (BELNR ist erst mit Buchungskreis+Jahr eindeutig) |
| `CompanyCode` | `Bukrs` — damit ist CH vs. AT in der Journal-Tabelle unterscheidbar |
| `DebitAmount`/`CreditAmount` | `Shkzg = S` -> Soll = `Dmbtr`; `Shkzg = H` -> Haben = `Dmbtr` |
| `SignedAmountLocal` | Soll positiv, Haben negativ |
| `TransactionCurrency` | `Waers`, nur wenn abweichend von `Hwaer` (echte FW-Belege) |
| `IsManual` | Annahme: `Blart = SA` (manuelle Sachkontenbuchung) — mit Finance zu bestaetigen |
| `IsReversal` | `Stblg` nicht leer |
| `AccountCode`/`Kostl`/`Prctr` | fuehrende Nullen entfernt |

## 5. ABAP-Skizze (GET_ENTITYSET, vereinfacht)

```abap
METHOD finanzjournalset_get_entityset.
  DATA: lt_bkpf TYPE STANDARD TABLE OF bkpf,
        lv_budat_von TYPE budat VALUE '20250101'. " aus $filter Budat uebernehmen

  " $filter (Budat ge ...), $top/$skip aus io_tech_request_context uebernehmen.
  SELECT k~bukrs k~belnr k~gjahr s~buzei k~budat k~monat k~blart k~xblnr k~stblg
         k~hwaer k~waers s~hkont t~txt50 AS hkont_txt s~shkzg s~dmbtr s~wrbtr
         s~kostl s~prctr s~sgtxt
    INTO CORRESPONDING FIELDS OF TABLE et_entityset
    FROM bkpf AS k
    INNER JOIN bseg AS s
      ON s~bukrs = k~bukrs AND s~belnr = k~belnr AND s~gjahr = k~gjahr
    LEFT OUTER JOIN skat AS t
      ON t~saknr = s~hkont AND t~ktopl = 'TRAG' AND t~spras = 'D' " Kontenplan anpassen
    WHERE k~bukrs IN ('....CH....', '....AT....') " beide Buchungskreise CH/AT
      AND k~budat >= lv_budat_von
    ORDER BY k~bukrs k~gjahr k~belnr s~buzei.
ENDMETHOD.
```

Wichtig fuer den ABAP-Owner: `$top`/`$skip`/`$orderby`/`$filter` muessen wie beim
bestehenden `FinanzdataSchweizOeSet` unterstuetzt werden (Standard-Gateway-Paging);
grosse Selektionen bitte per Paket-Select statt Full-Table-Scan auf `BSEG`.

## 6. Abnahme

1. `GET .../FinanzJournalSet?$format=json&$top=5` liefert Zeilen mit allen Properties aus Abschnitt 3.
2. `$filter=Budat ge datetime'2025-01-01T00:00:00'` grenzt korrekt ein.
3. Zeilenzahl je Buchungskreis plausibel gegen SE16 (`BKPF`-Belege im Zeitraum).
4. Danach in der App: `Finance Cockpit > Journal Import > Schweiz/Oesterreich > Laden`;
   die App meldet Erfolg mit Zeilenzahl, und `Finance Pruefbuch`-artige Auswertungen auf
   `FinancialJournalEntries` koennen folgen.

## 7. Offene fachliche Punkte (Andreas)

- `IsManual = Blart SA`: reicht diese Abgrenzung oder sollen weitere Belegarten als
  manuell gelten?
- Profitcenter als „weitere Hauptdimension" ok, oder wird Segment/Geschaeftsbereich gewuenscht?
- Zeithorizont des ersten Loads (DateFilter der Export-Einstellungen gilt auch hier).
