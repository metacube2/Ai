# Finance: Kostenbasis der Gruppenmarge (Standardkosten)

Stand: 2026-07-14 — umgesetzt, getestet (`203/203`), deployed (Commit `8e0f51e`)

Betrifft das **alte** Standardkosten-Thema aus `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`,
nicht den Journal-Import.

## Ausgangslage (an Produktivdaten gemessen, 14.07.2026)

| TSC | Zeilen | Kosten <> 0 | Quelle |
| --- | --- | --- | --- |
| ZSCHWEIZ (CH/AT) | 40'292 | **0 %** | SAP OData |
| TRDE | 6'879 | **0 %** | Alphaplan |
| TRUK | 1'033 | **0 %** | Sage UK |
| TRFR | 2'509 | 51 % | SAP B1 |
| TRSE | 5'229 | 81 % | Sage ES |
| TRUS | 1'427 | 92 % | SAP B1 |
| TRIT | 19'006 | 96 % | SAP B1 |
| TRIN | 6'779 | 99 % | SAP B1 |

Ursachen der beiden grossen Luecken:

- **CH/AT:** `StandardCost` war im Seed hart auf `=0` gemappt, weil der Umsatz-Service
  (`FinanzdataSchweizOeSet`) kein Kostenfeld liefert.
- **DE:** Das Mapping wartete auf eine Spalte `EinstandsPreis` — die es im Alphaplan-Export
  gar nicht gibt. Der Rohertrag war immer da und wurde nur weggeworfen.

## Was SAP dazu sagt (ABAP-Analysereport)

Report `docs/abap/ZFIN_ANALYSE_STPRS_JOURNAL.abap`, Ausgabe in `stdpreis.txt`:

- **`mbewSet` ist im Service `ZPOWERBI_EINKAUF_SRV` bereits vorhanden.** Es musste also
  nichts auf SAP-Seite gebaut werden — die urspruengliche Annahme (neues EntitySet noetig)
  war falsch.
- Bewertungskreis **1100** = Trafag AG (CH, CHF): 65'447 Materialien, **96.3 %** mit
  `STPRS > 0`, 100 % `VPRSV = S`.
- Bewertungskreis **1200** = Trafag Ges.m.b.H. (AT, EUR): 2'564 Materialien, **99.6 %**.
- Von den **tatsaechlich fakturierten** Zeilen (41'114 ab 2025) haben **96.5 %** einen
  Standardpreis. `VBRP-WAVWR` (Kostenwert direkt auf der Faktura) waere mit 92.3 % die
  Alternative, ist im Z-Service aber **nicht exponiert**.
- `PEINH` (Preiseinheit) ist derzeit durchgaengig 1.

## Umsetzung

| Baustein | Ort |
| --- | --- |
| MBEW-Leser | `Services/SapGatewayStandardCostReader.cs` — `mbewSet` gepaged (`$top`/`$skip`, Filter auf `Bwkey`), Metadata-Vorpruefung, pure `MapRow` |
| Zuordnung | `Services/StandardCostEnricher.cs` — `Land` -> Bewertungskreis (CH=1100, AT=1200, per `T001K` bestaetigt), setzt `StandardCost` je Zeile |
| Einhaengepunkt | `Services/DataSources/SapGatewayDataSourceAdapter.cs` — Anreicherung nach dem Umsatzimport |
| Deutschland | `Services/ManualExcelImportService.DeriveAlphaplanUnitCost` — Einstandswert = `NettoPreisGesamt - RohertragGesamt` |
| Tests | `TrafagSalesExporter.Tests/StandardCostTests.cs` (14 Tests) |

### Die zentrale Falle: Stueckpreis vs. Zeilensumme

`ManagementCockpitService.ResolveGroupMarginCostBasis` rechnet **`Menge x StandardCost`**.
`StandardCost` muss daher ein **Stueckpreis** sein. Aber:

- `MBEW-STPRS` gilt pro **`PEINH`** Stueck -> wird durch die Preiseinheit geteilt.
- `VBRP-WAVWR` ist eine **Zeilensumme**.
- Der Alphaplan-Rohertrag ist eine **Zeilensumme** -> wird durch die Menge geteilt.

Ohne diese Normalisierung waere die Kostenbasis um genau diesen Faktor zu hoch — und zwar
still, ausgerechnet in den Laendern, die repariert werden sollten. `PEINH = 1` heute
schuetzt nicht: ein einziges Material mit `PEINH = 100` wuerde reichen.

### Warum Material UND Bewertungskreis im Schluessel stehen

MBEW ist je Material **und** Bewertungskreis verschluesselt. Ein Join nur ueber das Material
wuerde CH-Zeilen den oesterreichischen Preis geben (und umgekehrt). Der Umsatz-Service
liefert keinen Bewertungskreis, deshalb wird er aus dem Land der Zeile abgeleitet.

### Guardrail

Schlaegt das Lesen der Standardpreise fehl, laeuft der **Umsatzimport trotzdem durch**
(Warning im Eventlog, `StandardCost` bleibt 0). Ein Kostenproblem darf nie den taeglichen
Umsatzexport eines ganzen Landes verhindern.

## Offen

1. **Interner Lieferant war eine Attrappe — fuer TR AG seit 2026-07-15 umgesetzt, aber
   produktiv noch nicht wirksam.** TR AG als liefernde Gesellschaft nutzt jetzt echte
   MBEW-STPRS-Konzernkosten statt lokaler Verkaufszeilen-Kosten, siehe
   `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Nachtrag 2026-07-15 Teil 2. Live-Pruefung
   2026-07-16: `GroupStandardCosts` ist auf dem Server weiterhin leer, weil der
   ZSCHWEIZ-Standardpreis-Read seit dem Deploy zweimal fehlschlug (500-Fehler bzw.
   Haenger) — vermutlich wegen der falschen Test-Server-URL `travt762` statt `travp762`
   (Punkt 5 unten). TR IN/TR IT weiterhin ohne Konzernkostenquelle (TR IT live geprueft:
   SAP B1 pflegt keinen Standardkosten-Wert je Material).
2. **UK** liefert keine Kostenspalte im Sage-Export; **FR** hat bei 49 % der B1-Zeilen
   keinen `StockPrice` (Stammdatenfrage an FR).
3. **Waehrungsmisch-Bug** (`Marge Original`) — GEFIXT 2026-07-15: Schalter
   `ExportSettings.GroupMarginCostCurrencyMode` (Mask/Convert) verhindert das stille
   Mischen, siehe `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` Nachtrag 2026-07-15. Fuer
   CH/AT bleibt der Fall aktuell neutral (Kosten- = Verkaufswaehrung); scharf wuerde er
   erst mit Punkt 1 (STPRS der liefernden Gesellschaft, z. B. CH-Verkauf mit EUR-Kosten).
4. **Drei Fachfragen an Andreas:** Welche Kostenart (lokaler Einstandswert vs.
   Konzern-Herstellkosten)? Bei internem Trafag-Lieferanten: Preis der liefernden oder der
   verkaufenden Gesellschaft? Lokal oder konzernweit rechnen?
5. **NEU 2026-07-16: `Sites.SapServiceUrl` fuer ZSCHWEIZ zeigt auf den Test-Server
   `travt762` statt `travp762` (Prod).** Bereits als Ursache fuer „CH/AT sieht 2026 nicht"
   bekannt (siehe `docs/rag/FINANCE.md`); vermutlich auch Ursache fuer den fehlgeschlagenen
   MBEW-Read (Punkt 1). Fix ist eine Konfigurationsaenderung, kein Codechange.

## Nachsorge nach dem naechsten Export

- Kostenquote fuer `ZSCHWEIZ` und `TRDE` pruefen; erwartet werden fuer CH/AT rund **96 %**
  (SAP-Messwert). Deutlich weniger deutet auf ein Material-Matching-Problem hin.
- Gruppenmarge fuer CH/AT und DE fachlich mit Andreas plausibilisieren, bevor sie als
  belastbar kommuniziert wird.

## Nachtrag 2026-07-16: mbewSet haengt reproduzierbar, Kostenquoten aller anderen Laender verifiziert, WAVWR-Weg eingeschlagen

Zusammenfassung eines laengeren Live-Diagnose-Tages, damit der Stand auch nach einem
Chat-Abbruch nachvollziehbar bleibt.

### mbewSet haengt — 3 von 3 Versuchen, auch nach App-Neustart

Der `ZSCHWEIZ`-Import wurde dreimal ueber die UI gestartet, alle drei Male blieb er
exakt an derselben Stelle stehen (Log-Eintrag „Standardpreis-Read gestartet", danach
**keine** weitere Zeile — kein Erfolg, kein Fehler, kein `ExportLogs`-Eintrag):

1. 2026-07-15 12:01:48–12:02:08 Uhr (vor jedem Neustart)
2. 2026-07-16 08:03:46–08:04:04 Uhr
3. 2026-07-16 08:58:10–08:58:27 Uhr (**nach** einem sauberen App-Neustart via
   `app_offline.htm` um ca. 08:15 Uhr — der Neustart hat das Problem NICHT geloest,
   spricht gegen einen reinen App-Zustands-Fehler)

Der konfigurierte HTTP-Timeout in `SapGatewayStandardCostReader.CreateClient` ist 5
Minuten; in allen drei Faellen wurde dieser Timeout nicht sauber als Fehler geloggt,
obwohl weit mehr als 5 Minuten vergingen. `GroupStandardCosts` blieb bei allen drei
Versuchen bei `0` Zeilen (zuletzt per Live-SQL gegen die Produktiv-DB bestaetigt).

### Manueller Browser-Test: mbewSet antwortet, aber langsam/moeglicherweise ungefiltert

Der SAP-Entwickler hat die exakte App-URL manuell im Browser aufgerufen:
```
http://travt762.sap.trafag.com:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/mbewSet?$format=json&$top=1000&$skip=0&$orderby=Bwkey,Matnr&$filter=Bwkey eq '1100' or Bwkey eq '1200'
```
Ergebnis: echte, valide STPRS-Daten kommen zurueck, aber die Antwort ist sehr gross und
laedt ueber mehrere Sekunden beim Scrollen kontinuierlich nach — deutet darauf hin, dass
`$top=1000` von diesem Z-Service moeglicherweise nicht serverseitig durchgesetzt wird
und (fast) der gesamte Bestand (~68'000 Materialien) in einer Antwort zurueckkommt.
**Nicht abschliessend geklaert**, aber als Arbeitshypothese fuer die Haenger-Ursache
plausibel.

### Kostenquoten aller anderen Laender heute verifiziert (Produktiv-DB, nach SQL-Bugfix)

Erste Abfrage hatte einen Bug (Textvergleich `StandardCost <> 0` auf einer TEXT-Spalte
zaehlte faelschlich fast alles als „gefuellt"). Korrigiert mit `CAST(StandardCost AS REAL)`:

| TSC | Zeilen | Kosten gefuellt | Anteil | Vergleich zu 14.07. |
| --- | ---: | ---: | ---: | --- |
| TRDE | 6'901 | 4'726 | **68.5 %** | war 0 % -> **deutlich verbessert** (DE-Feature vom 14.07. wirkt produktiv) |
| TRES | 5'249 | 4'249 | 80.9 % | war 81 % — stabil |
| TRFR | 2'522 | 1'296 | 51.4 % | war 51 % — stabil (bekannte Luecke, fehlende FR-Stammdaten) |
| TRIN | 6'810 | 6'771 | 99.4 % | war 99 % — stabil |
| TRIT | 19'011 | 18'191 | 95.7 % | war 96 % — stabil |
| TRUS | 1'428 | 1'318 | 92.3 % | war 92 % — stabil |
| TRUK | 1'033 | 0 | 0 % | unveraendert (Sage liefert keine Kostenspalte) |
| TRAT | 1'454 | 0 | 0 % | erwartet — ZSCHWEIZ-Import haengt seit 15.07. |
| TRCH | 38'838 | 0 | 0 % | erwartet — ZSCHWEIZ-Import haengt seit 15.07. |

**Keine Regression** durch die TR-AG-Aenderung vom 15.07. — alle unabhaengigen Laender
sind stabil auf bekanntem Niveau, DE sogar verbessert.

### Live-Bestaetigung: `Sales_All_2026-07-15.xlsx` enthaelt (noch) keine TR-AG-Konzernkosten

Datei vom Server-Share heruntergeladen und per ClosedXML-Lesetool geprueft: Blatt
„Gruppenmarge Details", 2'000 Zeilen durchsucht, davon 45 mit Lieferant „Trafag AG"
(ueber TRFR/TRIN/TRIT) — **alle** zeigen noch `CostSource = Interner Standardpreis`,
keine einzige `Konzernkosten TR AG`. Konsistent mit der leeren `GroupStandardCosts`-
Tabelle. `sharedStrings.xml` der Datei enthaelt den Text „Konzernkosten" gar nicht.

### Neuer Weg eingeschlagen: VBRP-WAVWR statt MBEW-STPRS fuer CH/AT

Der SAP-Entwickler (im Chat bestaetigt: ist Teil des SAP-Teams) hat direkt in SE16N
gegen `VBRP` geprueft: `WAVWR` ist real befuellt (siehe Stichprobe in der neuen
Spezifikationsdatei). Live gegen `$metadata` von `ZPOWERBI_EINKAUF_SRV` verifiziert
(Passwort aus der App-DB genutzt, nie ausgegeben): **`Wavwr` ist aktuell in KEINEM
EntityType des Service exponiert**, auch nicht in `FinanzdataSchweizOe`.

Voller Verlauf, Begruendung und die drei offenen technischen Fragen an den SAP-
Entwickler (Zeilensumme vs. Stueckpreis, Waehrung, Vorzeichen bei Gutschriften):
**`docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`**. Das ist jetzt der bevorzugte Weg fuer
die CH/AT-Kostenbasis — App-seitig noch NICHT umgesetzt, wartet auf SAP-Feld-Freigabe
und Antworten auf die drei Fragen.

### Aktueller Live-Zustand am Ende der Session (2026-07-16)

- `ZSCHWEIZ`-Import: letzter Versuch (08:58 Uhr) haengt vermutlich weiterhin (kein
  Abschluss beobachtet, Session endete waehrend des Wartens).
- `GroupStandardCosts`: `0` Zeilen auf Produktiv.
- Server-DLL: unveraendert seit Commit `5efeed7` (`15.07.2026 11:22:32`) — nur der
  zwischenzeitliche `app_offline.htm`-Neustart am 16.07. hat keinen neuen Code gebracht.
- Kein Code wurde heute (16.07.) geaendert — reine Diagnose, Doku und ein App-Neustart.
- Naechster Schritt liegt beim SAP-Entwickler: `Wavwr` freischalten + drei Fragen aus
  `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` beantworten. Optional in derselben
  Abstimmung: `Sites.SapServiceUrl` fuer `ZSCHWEIZ` von `travt762` auf `travp762`
  korrigieren (separates, bereits bekanntes Problem, reine Konfigurationsaenderung).
