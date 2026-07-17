# Finance Gruppenmarge

Stand: 2026-06-16

## Zweck

Die neue Sicht `Management Analyse > Experten > Gruppenmarge` ist eine fachliche Pruefsicht fuer Gruppenmarge je Land, Sparte und Detailzeile. Sie ist noch kein final freigegebener Finance-Abschlusswert.

Ausloeser war `kosten.xlsx`, Blatt `Tabelle2`: Dort wurde die Frage erkennbar, ob die Marge nicht mit den lokalen Verkaufszeilenkosten, sondern aus Gruppensicht mit der richtigen Lieferanten-/Standardkostenbasis gerechnet werden muss.

## Fachliche Arbeitshypothese

- Externer Lieferant: Kostenbasis aus der Verkaufszeile bzw. aus dem verfuegbaren Standardkostenfeld der importierten Zeile.
- Interner Trafag-Lieferant: Kostenbasis soll fachlich auf dem internen Lieferanten-Standardpreis basieren, in SAP-Kontext `MBEW-STPRS`.
- Interne Lieferkette: MVP stoppt nach einer Ebene.
- Fehlende Kosten oder unklarer Lieferant werden nicht geschaetzt.
- Bei offener Kostenbasis ist die Gruppenmarge nicht belastbar.

## Aktuelle technische Umsetzung

- Neuer Reiter `Gruppenmarge` in der Management Analyse.
- Neuer Navigationspunkt `Management Analyse > Experten > Gruppenmarge`.
- Schnelluebersicht enthaelt einen Direktbutton zur Gruppenmarge.
- Datenbasis ist die gleiche Finance-gefilterte Ergebniszeile wie `Finance Summary`: nur `Finance Include = TRUE` und nach Finance-Regeln berechneter Netto-Ist-Wert.
- Lieferantentyp wird aus `SupplierNumber`, `SupplierName`, `SupplierCountry` heuristisch erkannt.
- Kostenbasis nutzt aktuell das im zentralen Modell vorhandene `StandardCost` mit Mengenmultiplikation.
- Detailtabelle ist auf 1000 Zeilen begrenzt; Summary-, Laender- und Spartenaggregation laufen ueber alle gefilterten Gruppenmargen-Zeilen.

## Anzeige- und Validierungsregeln

- `Umsatz` bleibt immer sichtbar.
- `Bekannte Kostenbasis` zeigt die aktuell berechenbare Kostenbasis.
- `Marge` und `%` werden nur angezeigt, wenn die Kostenbasis fuer die Zeile bzw. Gruppe vollstaendig geklaert ist.
- Wenn mindestens eine Zeile in Land/Sparte `Standardpreis fehlt` oder `Lieferant unklar` hat, zeigen `Marge` und `%` in der Aggregation `-`.
- Detailzeilen mit Status ungleich `OK` zeigen ebenfalls keine Marge.
- Spalte `Offen` zaehlt offene Kostenbasis, also `Standardpreis fehlt` und `Lieferant unklar`.

## Aktueller Datenbefund

Lokale Pruefung der zentralen Datenbank am 2026-06-16:

- AT/TRAT: fuer 2025 sind `StandardCost` und Supplier-Felder in den geprueften zentralen Zeilen leer bzw. 0. Gruppenmarge ist damit offen.
- CH/TRCH: fuer 2025 sind `StandardCost` und Supplier-Felder in den geprueften zentralen Zeilen leer bzw. 0. Gruppenmarge ist damit offen.
- IN/TRIN und IT/TRIT haben teilweise Standardkosten und interne Lieferanten, aber weiterhin offene Zeilen.
- ES/FR/UK/US zeigen bekannte Kostenbasis aus vorhandenen Standardkosten, haben aber ebenfalls offene Zeilen je nach Datenbestand.

Konsequenz: Die Sicht darf aktuell nicht als finale Marge interpretiert werden. Sie zeigt, welche Laender/Sparten eine belastbare Kostenbasis haben und wo Daten oder fachliche Regeln fehlen.

## Offene Entscheidungen

Das Multiple-Choice-Formular `docs/FINANCE_GRUPPENMARGE_MULTIPLE_CHOICE_2026-06-16.docx` soll von Andreas/Finance ausgefuellt werden. Es klaert insbesondere:

- Erkennung interner Lieferanten.
- Kostenbasis fuer externe Lieferanten.
- Kostenbasis fuer interne Lieferanten.
- Umgang mit mehrstufigen internen Lieferketten.
- Waehrung und Umrechnung fuer Gruppenmarge.
- Verhalten bei fehlenden Standardpreisen.
- Freigabeumfang des MVP.

## Nachtrag 2026-07-15: Schalter fuer abweichende Kostenwaehrung (Entscheid D)

Umgesetzt, getestet (`226/226`) und deployed (Commit `08f5572`, DLL `15.07.2026 08:53:47`):

- Neues Setting `ExportSettings.GroupMarginCostCurrencyMode` mit den Werten `Mask` (Default)
  und `Convert`; UI unter `Einstellungen > Export Einstellungen > Gruppenmarge bei
  abweichender Kostenwaehrung`.
- `Mask`: Zeilen, deren Standardkostenwaehrung von der Verkaufswaehrung abweicht, erhalten
  den Status `Kostenwaehrung abweichend`; `Marge`/`%` bleiben offen (`-` bzw. leer). Im
  Pruefbuch sind `MarginOriginal`/`MarginPercent` dafuer nullable geworden.
- `Convert`: Die Kostenbasis wird mit dem Jahreskurs (31.12. des Finance-Jahres der Zeile)
  in die Verkaufswaehrung umgerechnet; ohne verfuegbaren Kurs faellt die Zeile auf `Mask`
  zurueck. Der verwendete Kurs steht sichtbar im `CostSource`-Label.
- Gemeinsame Logik: `Services/GroupMarginCostCurrencyConverter.cs`; verdrahtet in
  `ManagementCockpitService` (Gruppenmarge-Tab + Finance Pruefbuch) UND
  `ExcelExportService` (zentrales `Sales_All` + `Finance_Dashboard_Nachweis`), damit
  Dashboard und Excel identisch rechnen.
- Zentrale `Sales_All_*.xlsx` enthaelt seit 2026-07-15 zusaetzlich die Blaetter
  `Gruppenmarge Summary` und `Gruppenmarge Details` (vorher nur im Nachweis-Excel).
- Fachlich bleibt der Entscheid Mask vs. Convert bei Andreas; der Schalter erlaubt den
  direkten Vergleich beider Varianten an echten Zahlen ohne Codeaenderung. `Marge CHF`
  war und bleibt unabhaengig davon korrekt.
- NICHT durch den Schalter erledigt: Fachfragen A (Kostenart lokal vs. Konzern-Herstellkosten)
  und B (Preis der liefernden vs. verkaufenden Gesellschaft bei internen Lieferanten) —
  beide brauchen eine neue Datenquelle (MBEW-STPRS je liefernder Gesellschaft).

## Nachtrag 2026-07-15 (Teil 2): TR AG als liefernde Gesellschaft umgesetzt

Umgesetzt, getestet (`240/240`) und deployed (Commit `5efeed7`, DLL `15.07.2026 11:22:32`).
Live-Pruefung 2026-07-16 zeigt: produktiv noch NICHT wirksam, siehe Nachtrag unten. Nach
Live-Stichprobe (siehe
`docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` "Offen" Punkt 1) wurde Frage B fuer TR AG
konkret geloest:

- **Lieferant -> Gesellschaft:** `GroupMarginSupplierClassifier.ResolveDeliveringEntity`
  erkennt TR AG/TR IT/TR IN am Klartext von `SupplierName` (nicht `SupplierNumber` — die
  ist je TSC unterschiedlich verschluesselt). Stichprobe auf Produktivdaten-Snapshot
  (68'913 Zeilen, 8'995 intern): 0 Kollisionen.
- **Neue Tabelle `GroupStandardCosts`:** MBEW-STPRS fuer Bewertungskreis 1100 (TR AG),
  Waehrung CHF. Wird beim ohnehin laufenden CH/AT-SAP-Import zusaetzlich befuellt
  (`SapGatewayDataSourceAdapter.PersistGroupStandardCostsAsync`) — kein neuer Trigger,
  Full-Replace je Lauf.
- **Kostenbasis-Ueberschreibung:** Ist der Lieferant TR AG UND liegt ein Treffer in
  `GroupStandardCosts` vor, wird die echte Konzernkostenbasis verwendet (Label
  "Konzernkosten TR AG (MBEW-STPRS)") — unabhaengig davon, welches Land/welche TSC
  verkauft hat. Ohne Treffer (Material noch nicht erfasst, oder TR IN/TR IT) faellt die
  Logik unveraendert auf die bisherige lokale Kostenbasis zurueck — keine Regression.
  Umgesetzt in `ManagementCockpitService` (Gruppenmarge-Tab + Finance Pruefbuch) UND
  `ExcelExportService` (zentrale Excel + Nachweis-Excel), gleiche Stelle wie der
  Kostenwaehrungsschalter (Entscheid D).
- **Zusammenspiel mit Entscheid D bestaetigt:** Verkauft z. B. TRDE (Finance-Waehrung EUR)
  ein TR-AG-geliefertes Produkt, weichen Kosten- (CHF) und Verkaufswaehrung (EUR)
  automatisch ab — der bestehende Schalter greift korrekt (Mask maskiert, Convert
  rechnet mit Jahreskurs um).

**TR IN/TR IT bleiben offen** (Frage B fuer diese beiden Gesellschaften): Live-Stichprobe
2026-07-15 gegen TR ITs SAP-B1-Schema (`IT01_P`, erreichbar ueber BI1-HANA) zeigt, dass
weder `OITM.PrdStdCst` noch `OITM/OITW.AvgPrice` bei aktiv gefuehrten Materialien befuellt
sind (durchgaengig 0, trotz realem Lagerbestand) — nur `LastPurPrc` (Einkaufspreis
zugekaufter Komponenten, nicht Herstellkosten). TR IT pflegt aktuell also gar keinen
nutzbaren Standardkosten-Wert je Material in SAP B1. Das ist keine Codefrage mehr, sondern
eine offene Frage an Andreas/TR-IT-Controlling: Wo (wenn ueberhaupt) wird die
Herstellkosten-Kalkulation gefuehrt? TR IN war vom Entwicklungsrechner aus nicht
erreichbar (Netzwerk/Firewall) und daher gar nicht pruefbar.

### Nachtrag 2026-07-16: Produktiv noch nicht wirksam — Root Cause gefunden

Ein Tag nach Deploy live geprueft: `GroupStandardCosts` hat auf dem Produktivserver
weiterhin `0` Zeilen. Stichprobe in `Sales_All_2026-07-15.xlsx` (Gruppenmarge Details,
per ClosedXML-Lesetool ausgewertet): 45 gefundene Trafag-AG-Lieferant-Zeilen (TRFR/TRIN/
TRIT) zeigen durchgaengig `CostSource = Interner Standardpreis` / `Status = Standardpreis
fehlt` — keine einzige `Konzernkosten TR AG`. Die neue Logik greift also noch nicht.

Root Cause laut `AppEventLogs`/`ExportLogs`: Der ZSCHWEIZ-Import lief seit dem Deploy
zweimal, beide Male ohne erfolgreichen Standardpreis-Read:

1. **2026-07-15 07:29 Uhr** (vor dem Deploy): Umsatzimport OK (`40'292` Zeilen), aber
   `mbewSet`-Read schlug mit `500 Internal Server Error` fehl. Guardrail griff korrekt
   (Umsatzimport lief trotzdem durch, `StandardCost` blieb 0 fuer die betroffenen Zeilen).
2. **2026-07-15 12:01-12:02 Uhr** (nach dem Deploy): „Standardpreis-Read gestartet"
   geloggt, danach bricht die Log-Kette komplett ab — kein Abschluss-, kein Fehler-Log,
   nicht mal ein `ExportLogs`-Eintrag fuer diesen Lauf ueberhaupt. Der Prozess blieb
   offenbar mitten in der SAP-Abfrage haengen oder wurde unterbrochen.

Beide Aufrufe gingen an `travt762.sap.trafag.com` (TEST-Server) statt `travp762` (Prod) —
deckt sich mit dem bereits bekannten, separaten Problem „CH/AT sieht das laufende Jahr
nicht" (`Sites.SapServiceUrl` zeigt fuer ZSCHWEIZ auf den Testserver). Das ist sehr
wahrscheinlich auch die Ursache fuer den 500-Fehler bzw. den Haenger beim `mbewSet`-Read —
beide Probleme (2026 fehlt, Standardpreise leer) haben denselben wahrscheinlichen Fix.

**Naechster Schritt:** `Sites.SapServiceUrl` fuer `ZSCHWEIZ` auf `travp762` korrigieren,
danach ZSCHWEIZ-Import erneut anstossen und `GroupStandardCosts`/eine neu erzeugte
`Sales_All`- bzw. Nachweis-Datei erneut pruefen (gleiche Stichprobenmethode: Filter auf
`SupplierName LIKE '%Trafag AG%'` im Blatt „Gruppenmarge Details", `CostSource` muss
`Konzernkosten TR AG (MBEW-STPRS)` zeigen).

## Nachtrag 2026-07-17: Supplier-Felder sind quellensystemisch strukturell leer (globales Problem)

Anlass: Stichprobe aus `Sales_All`-Export (TRAT-Zeilen) zeigte durchgaengig leere
`Supplier number`/`Supplier name`/`Supplier country`. Codepruefung ergab: das ist **kein
Datenfehler und kein Einzelfall**, sondern betrifft je nach Quelle unterschiedliche,
strukturelle Gruende:

| Quelle | Supplier-Felder | Grund |
| --- | --- | --- |
| CH/AT (`ZSCHWEIZ`, SAP OData) | immer leer | Im Seed-Mapping (`DatabaseSeedService.EnsureSapODataDachMapping`) gibt es fuer `SupplierNumber/Name/Country` gar kein Mapping — `FinanzdataSchweizOeSet` (VBRK/VBRP) exponiert kein Lieferantenfeld. |
| UK (Manual Excel) | immer leer | `EnsureUkManualExcelMapping` enthaelt keine Supplier-Spalten. |
| DE (Alphaplan) | leer je nach Exportspalten | Mapping erwartet `Lieferanten Nummer`/`Name Lieferant`/`Land Lieferant`; nur gefuellt, wenn Alphaplan diese Spalten liefert. |
| ES (Sage CSV) | leer | Kein Supplier-Mapping im Spanien-Import vorhanden. |
| FR/IT/US/IN (SAP B1/HANA) | teilweise gefuellt | Supplier = `OITM.CardCode`, der **Standardlieferant im Artikelstamm** (`HanaQueryService`), nicht der Beleglieferant — leer, wenn im Artikel kein Default-Lieferant gepflegt ist. |

**Fachliche Konsequenz, die ueber reine Datenluecken hinausgeht:** Sind alle drei
Supplier-Felder leer, liefert `GroupMarginSupplierClassifier.Resolve` `Unklar`, und
`ResolveGroupMarginStatus` setzt dadurch **unabhaengig von der Kostenbasis** immer
`Lieferant unklar` (siehe Konstante `Unclear` und die Statuslogik). Status `Lieferant
unklar` zaehlt als offene Kostenbasis (`HasOpenGroupMarginCostBasis`), also bleiben
`Marge`/`%` `-`.

Damit greift die am 2026-07-16 gefuellte CH/AT-Kostenbasis (WAVWR/STPRS, Fuellgrad TRCH
96,5 %/TRAT 99,9 %) **in der Gruppenmarge-Sicht aktuell gar nicht**: Jede ZSCHWEIZ-Zeile
bleibt mangels Supplier-Feldern auf `Lieferant unklar` maskiert, obwohl die Kostenbasis
selbst jetzt vorhanden waere. Gleiches gilt strukturell fuer UK und ES.

**Offene Fachfrage an Andreas (neu, noch nicht auf dem Multiple-Choice-Bogen):** Soll
CH/AT (verkauft als Trafag AG selbst) ueber eine Regel automatisch als eigene
Lieferkategorie gelten (statt ueber die leeren Supplier-Textfelder erkannt zu werden),
damit die WAVWR-Kostenbasis in der Marge wirksam wird? Betrifft nur die
Klassifikationsregel (`GroupMarginSupplierClassifier`), keine Kostenberechnung. Noch
NICHT umgesetzt — reine Dokumentation des Befunds, Entscheidung liegt bei Andreas/Finance.

## Naechste technische Schritte nach Fachfreigabe

- Falls externe Lieferanten eine andere Kostenquelle als `StandardCost` brauchen, neues Feld oder Mapping in `CentralSalesRecords` ergaenzen.
- Falls interne Lieferanten immer ueber SAP `MBEW-STPRS` laufen muessen, separate SAP-Kostenquelle bzw. Mapping anbinden.
- Lieferantenerkennung nicht nur heuristisch, sondern regel-/stammdatenbasiert pflegen.
- Tests fuer offene Kostenbasis und Aggregationsanzeige ergaenzen, sobald die finale Fachregel fixiert ist.

