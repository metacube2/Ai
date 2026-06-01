# Finance Dashboard - Stand und offene Punkte

Stand: 2026-06-01

Zweck: Argumentationshilfe fuer den heutigen Austausch mit Finance. Dieses Dokument trennt den aktuellen belastbaren Stand von fachlich noch offenen Punkten.

## Kurzfazit

Das Finance Dashboard ist technisch produktiv nutzbar. Die fuehrende Sicht ist `Finance Summary`; `Management Analyse` dient als Diagnose- und Erklaerungsebene. Die zentralen Finance-Regeln sind im Code umgesetzt und mit Tests abgesichert.

Offen sind nicht primaer technische Grundlagen, sondern fachliche Abgrenzungen je Land: Welche lokale Auswertung ist offiziell fuehrend, welche Filter gelten, und ob bestimmte Differenzen akzeptiert oder durch zusaetzliche Quell-/Filterlogik erklaert werden muessen.

## Nachtrag Sitzung 2026-06-01

Aus der Sitzung mit Finance / Andreas ergeben sich diese aktualisierten Punkte:

1. Intercompany
   - Frage aus Finance: Sind Intercompany-Umsaetze bereits in den Standortdaten herausgerechnet?
   - Aktueller Eindruck aus der Sitzung: Anscheinend sind IC-Anteile in einzelnen Standortauswertungen bereits bereinigt, trotzdem bleiben Abweichungen.
   - Umsetzung: `Management Analyse > Laender` zeigt jetzt IC/2nd-party und `Ist ohne IC` als Diagnosewerte.
   - Wichtig fuer die App: Das Dashboard entfernt IC weiterhin nicht automatisch aus dem Standard-Ist.
   - Folgeaktion: Pro Standort klaeren, ob die Quellzahl bereits netto ohne IC geliefert wird oder ob das Dashboard IC noch fachlich abziehen soll.

2. Spanien
   - Aussage Sitzung: Spanien hat fachlich keine echte Soll/Ist-Abweichung.
   - Ist-Wert im Dashboard: `3'082'320.18 EUR`.
   - Der bisherige Sollwert `3'102'333.61 EUR` ist falsch bzw. wahrscheinlich ein Excel-/Referenzfehler.
   - Umsetzung: ES-FinanceReference 2025 wird auf `3'082'320.18 EUR` gesetzt; `FinanceProbe` nutzt denselben Referenzwert.
   - Folgeaktion: Quelle der falschen Excel-/Referenzzahl weiterhin fachlich nachvollziehen, falls Audit gefragt ist.

3. Wechselkurse 2025
   - In den Settings / Kurstabellen fehlt ein Feld, auf welches Datum der Kurs angewendet wird.
   - Zu klaeren: Anwendung auf `DocDate`, `PostingDate`, `InvoiceDate` oder ein anderes Periodendatum.
   - Umsetzung: In `Settings > Export Einstellungen` ist `Wechselkurse anwenden auf` konfigurierbar.
   - Umsetzung: `Management Analyse > Rohdaten Diagnose` zeigt das verwendete Kursdatum an.

4. Sparten-Finanzanalyse
   - Aktueller Befund: Mehr als 90% der Werte sind nicht zugeordnet.
   - Aussage Andreas: Das kann fachlich nicht stimmen.
   - Umsetzung: Sparten-Materialabgleich normalisiert fuehrende Nullen in Materialnummern.
   - Umsetzung: Bei >=90% nicht zugeordnet / nicht im Stamm zeigt die Management-Analyse einen Warnhinweis mit Pruefpunkten.
   - Folgeaktion: Zentrale Spartenzuordnung pruefen, insbesondere Mapping gegen TR-AG-/SAP-Referenz, Materialnummernformat, fuehrende Nullen, lokale Artikelnummern und Fuellung von `ProductDivisionRefSet`.

## Was aktuell vorhanden ist

### Finance Summary

- Fuehrende Management-Sicht fuer Soll/Ist-Vergleiche.
- Nutzt dieselbe `FinanceRuleEngine` wie der zentrale Excel-/Finance-Abgleich.
- Filter fuer Jahr, Land und Waehrung wirken auf das Finance-Endergebnis.
- Standard-Ist bleibt inklusive aller Positionen.
- Intercompany / 2nd-party wird separat ausgewiesen und nicht still aus dem Hauptwert entfernt.

### Management Analyse

Die `Management Analyse` ist als Diagnosebereich vorhanden und links direkt navigierbar.

Vorhandene Reiter:

- `Finance Summary`
- `Laender`
- `Datenstatus`
- `Abweichungen`
- `Gutschriften`
- `Datenqualitaet`
- `Spartenanalyse > Finanzanalyse`
- `Spartenanalyse > Zentrale Zuordnung`
- `Rohdaten Diagnose`

Damit kann Finance nicht nur die Endzahl sehen, sondern auch Datenstand, Abweichungen, Gutschriften-Kandidaten, Datenqualitaet und Produktsparten-Abdeckung nachvollziehen.

### Spartenanalyse

- Produktspartenanalyse ist umgesetzt.
- Grundlage ist die zentrale TR-AG-/SAP-Referenz, nicht lokale ERP-Sparten anderer Laender.
- Umsatz kann nach `PAPH1 Detail`, `Produktfamilie` oder `Produktsparte` gruppiert werden.
- `Top 10`, Laenderflaggen und visuelle Sparten-Icons sind vorhanden.
- Statuswerte fuer Materialzuordnung:
  - `Zugeordnet`
  - `Nicht zugeordnet`
  - `Nicht im TR-AG-Stamm`
  - `Material fehlt`

### Technische Validierung

- Letzter dokumentierter Deploy: 2026-05-29 13:47.
- Letzter dokumentierter Testlauf: `80/80` Tests gruen.
- Die Kernlogik ist dokumentiert in:
  - `docs/rag/FINANCE.md`
  - `docs/FINANCE_ENTSCHEIDE.md`
  - `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`

## Verbindliche Finance-Regeln

Diese Regeln sind aktuell fuehrend:

| Thema | Regel |
| --- | --- |
| Fuehrende Sicht | `Finance Summary` |
| Wertbasis | Nettofakturawert pro Position |
| Jahresabgrenzung | `PostingDate`, Fallback `InvoiceDate`, danach `ExtractionDate` |
| Waehrung | Hauswaehrung des Landessystems |
| CHF | Nur Kontroll-/Reporting-Kandidat ueber Budgetkurse |
| Gutschriften / Storno | Als negative Beleg-/Positionszeilen behandeln |
| Intercompany / 2nd-party | Separat ausweisen, nicht automatisch aus Standard-Ist entfernen |
| Vergleich | Ist gegen `check.xlsx` / FinanceReference |

## Vorbesprechung: Umrechnung in CHF, Kurse, Kurstabellen

Aktueller Stand:

- Der Standardvergleich laeuft in der Hauswaehrung des Landessystems.
- CHF ist aktuell als Kontroll-/Reporting-Kandidat ueber Budgetkurse vorgesehen.
- SNB-Tageskurse sind nicht als Standardlogik fuer den Finance-Soll/Ist-Abgleich vorgesehen.

Warum das wichtig ist:

Wenn Finance eine konsolidierte CHF-Sicht erwartet, muss klar sein, welcher Kurstyp verwendet wird. Unterschiedliche Kurse koennen zu echten Abweichungen fuehren, obwohl die lokale Umsatzlogik korrekt ist.

Entscheidoptionen:

| Option | Beschreibung | Vorteil | Risiko / Frage |
| --- | --- | --- | --- |
| A: Hauswaehrung bleibt fuehrend | Soll/Ist je Land in lokaler Hauswaehrung | Fachlich sauber fuer lokale Abstimmung | Keine konsolidierte CHF-Gesamtsicht |
| B: Budgetkurs je Jahr | Umsatz wird mit Jahresbudgetkurs in CHF umgerechnet | Stabil, passend fuer Budget-/Management-Sicht | Muss exakt mit Finance-Budgetkursen gepflegt werden |
| C: Monatskurs | Umsatz wird je Monat mit Monatskurs umgerechnet | Naeher an periodischer Konzernsicht | Mehr Pflege, mehr Erklaerungsbedarf |
| D: Transaktionskurs aus Quellsystem | Kurs aus ERP-Beleg verwenden | Nahe an Buchungsrealitaet | Nicht in allen Quellen gleich verfuegbar |
| E: Tageskurs extern | z. B. SNB-/EZB-Tageskurs | Objektiv nachvollziehbar | Nicht passend, wenn Finance mit Budget-/Konzernkursen rechnet |

Empfehlung fuer das Gespraech:

- Fuer lokale Soll/Ist-Freigabe bleibt Hauswaehrung fuehrend.
- Fuer Management-/Konzernsicht wird eine separate CHF-Sicht definiert.
- Finance muss den offiziellen Kurstyp benennen: Budgetkurs, Monatskurs, Transaktionskurs oder anderer Konzernkurs.

Empfohlene Trennung im Dashboard:

1. `Local Currency View`
   Fuehrend fuer Soll/Ist je Land. UK muss in `GBP`, Indien in `INR`, USA in `USD` und EUR-Laender in `EUR` plausibel sein.

2. `CHF Reporting View`
   Zusaetzliche Management-/Konzernsicht. Abweichungen in dieser Sicht koennen aus Kursen entstehen und muessen getrennt von lokalen Umsatzabweichungen erklaert werden.

Vorschlag Kurstabelle:

| Feld | Bedeutung |
| --- | --- |
| `Year` | Gueltiges Jahr |
| `Month` | Optional fuer Monatskurse |
| `FromCurrency` | Quellwaehrung, z. B. EUR, GBP, USD, INR |
| `ToCurrency` | Zielwaehrung, meist CHF |
| `RateType` | Budget, MonthAverage, Transaction, Closing, Other |
| `Rate` | Umrechnungsfaktor |
| `Source` | Finance, SAP, Treasury, SNB, EZB usw. |
| `ApprovedBy` | Fachliche Freigabe |
| `ApprovedAt` | Freigabedatum |

Klaerungsfragen an Finance:

1. Soll CHF nur Reporting-Sicht sein oder offizieller Vergleichswert?
2. Welcher Kurstyp ist fuer 2025 offiziell: Budgetkurs, Monatskurs, Transaktionskurs oder Konzernkurs?
3. Gibt es bereits eine Finance-/Treasury-Kurstabelle?
4. Sind Kurse je Jahr ausreichend oder braucht es Monatskurse?
5. Sollen historische Kurse fixiert werden, damit alte Reports reproduzierbar bleiben?

Argument:

CHF-Umrechnung darf nicht nachtraeglich als stiller Faktor in den lokalen Soll/Ist-Vergleich gemischt werden. Erst lokale Hauswaehrung sauber bestaetigen, danach CHF als separate, klar dokumentierte Reporting-Sicht rechnen.

## Vorbesprechung: Optionen Kosten

Aktueller Stand:

- Das Finance Dashboard ist aktuell auf Net Sales / Umsatzabgleich ausgerichtet.
- Kostenfelder wie `StandardCost` koennen in einzelnen Quellen vorhanden sein, sind aber nicht als fuehrende Finance-Kostenlogik dokumentiert.
- Eine Marge-, COGS- oder Deckungsbeitragsrechnung ist fachlich noch nicht als verbindliche Standardlogik festgelegt.

Moegliche Ausbaustufen:

| Option | Beschreibung | Vorteil | Risiko / Frage |
| --- | --- | --- | --- |
| A: Nur Umsatz | Dashboard bleibt bei Net Sales und Soll/Ist | Stabil, aktuell am besten abgesichert | Keine Margen-/Kostenanalyse |
| B: Standardkosten aus Quell-ERP | Je Zeile vorhandene `StandardCost`-Felder nutzen | Schnell sichtbar, wenn Daten vorhanden | Nicht alle Laender liefern vergleichbare Kosten |
| C: Zentrale SAP-Kostenreferenz | Materialkosten aus zentralem SAP/TR-AG-Stamm ableiten | Einheitliche Konzernsicht | Nicht fuer alle lokalen Artikel eindeutig |
| D: Lokale Kosten je Land | Kosten aus lokalem ERP je Land verwenden | Nahe an lokaler Finance-Sicht | Unterschiedliche Methoden, schwer vergleichbar |
| E: Finance-Kostentabelle | Finance liefert gepflegte Kosten je Material/Jahr/Waehrung | Kontrollierbar und auditierbar | Pflegeaufwand, Gueltigkeiten notwendig |

Wichtige fachliche Entscheidung:

Kosten muessen zuerst methodisch definiert werden. Sonst entsteht eine Scheingenauigkeit, weil Umsatz, Standardkosten, lokale Herstellkosten und Konzernkosten vermischt werden.

Vorschlag Kostenlogik als Stufenmodell:

1. Phase 1: Kosten nur als Diagnose anzeigen, nicht als offizielle KPI.
2. Phase 2: Abdeckung messen: wie viele Umsatzzeilen haben belastbare Kosten?
3. Phase 3: Finance entscheidet pro Land / Materialquelle, welche Kostenart fuehrend ist.
4. Phase 4: Erst danach Marge / Deckungsbeitrag als offizielle Sicht freigeben.

Moegliche Kostentabelle:

| Feld | Bedeutung |
| --- | --- |
| `Year` | Gueltiges Jahr |
| `Material` | Artikelnummer |
| `Tsc` | Optional, wenn Kosten lokal unterschiedlich sind |
| `CountryKey` | Optional fuer landesspezifische Kosten |
| `CostType` | StandardCost, ActualCost, GroupCost, BudgetCost |
| `CostValue` | Kostenwert pro Einheit |
| `Currency` | Kostenwaehrung |
| `Unit` | Einheit / Mengeneinheit |
| `Source` | SAP, lokales ERP, Finance-Upload |
| `ApprovedBy` | Fachliche Freigabe |
| `ApprovedAt` | Freigabedatum |

Klaerungsfragen an Finance:

1. Welche Kostenart soll betrachtet werden: Standardkosten, Ist-Kosten, Herstellkosten, COGS oder Budgetkosten?
2. Soll Marge lokal je Land oder konzernweit einheitlich gerechnet werden?
3. Welche Quelle ist fuer Kosten fuehrend: lokales ERP, zentrales SAP, Finance-Upload oder Treasury/Controlling?
4. Wie werden fehlende Kosten behandelt: leer, 0, geschaetzt oder ausgeschlossen?
5. Sind Kosten pro Stueck, pro Belegposition oder als Beleg-/Auftragskosten verfuegbar?
6. Muessen Kosten in Hauswaehrung oder CHF ausgewiesen werden?

Argument:

Kosten sollten nicht direkt in die aktuelle Umsatzfreigabe gemischt werden. Sinnvoll ist ein separater Ausbau mit Abdeckungsquote und klarer Kostenquelle, damit Finance zuerst die Methode freigibt und danach die Marge belastbar wird.

## Laenderstand

| Land | Stand | Offener Punkt |
| --- | --- | --- |
| CH / AT | SAP OData `ZSCHWEIZ`; Trennung ueber Buchungskreis / Reporting-Land | Pruefen, ob `FKDAT` fachlich als Buchungsdatum akzeptiert ist |
| DE | Alphaplan Excel; `NettoPreisGesamtX`; finaler 2025-File liegt technisch vor | Finance muss bestaetigen, welche Kundenlaender / Filter zum offiziellen DE-Ist gehoeren |
| ES | Sage CSV; `ImporteNeto`; Credit Notes / REC negativ; Ist `3'082'320.18 EUR` fachlich bestaetigt | Bisheriger Sollwert `3'102'333.61 EUR` ist falsch bzw. Excel-/Referenzfehler |
| FR | SAP B1/HANA; Positions-Netto passt praktisch gegen Soll | Kein grosser offener Punkt dokumentiert |
| IN | Hauswaehrung INR; Vergleich in INR | Keine CHF-Tageskurslogik fuer Standardvergleich verwenden |
| IT | SAP B1/HANA; Finance-Methode mit IT-Abgrenzung dokumentiert | Nach neuem IT-Export pruefen, ob Summe und Abgrenzung final passen |
| UK | Sage / Manual Excel; GBP; `Sales Price/Value * Quantity`; Credit Notes negativ | Differenz ca. `-5'261.91 GBP`; Sage-Export, Discounts, Freight/Charges und 2nd-party pruefen |
| US | SAP B1/HANA; USD; Positions-Netto passt praktisch gegen Soll | Kein grosser offener Punkt dokumentiert |

## Offene Punkte fuer Finance

### 1. Deutschland

Aktueller Befund:

- Voller Alphaplan-Wert: ca. `4'154'690.05 EUR`.
- Nur `Land Kunde = Deutschland`: ca. `3'455'276.64 EUR`.
- Deutschland + China: ca. `3'647'592.44 EUR`.
- Sollwert: `3'635'923.00 EUR`.

Klaerung:

- Welche Kundenlaender gehoeren offiziell zum deutschen Finance-Ist?
- Gibt es Warengruppen-, Versandbedingungs-, Kunden- oder Branchenfilter?
- Falls nach Codes gefiltert wird: Sind die Code-Spalten im Export vorhanden oder braucht es eine Mapping-Tabelle?

Argument:

Die Technik kann den Alphaplan-Export lesen. Die offene Frage ist die offizielle Finance-Abgrenzung, nicht das Dateiformat.

### 2. Italien

Aktueller Stand:

- Italien kommt aus SAP B1/HANA.
- Fuehrend ist Positions-Netto, nicht Belegkopf-Deduplizierung.
- IT-spezifische fachliche Methode ist dokumentiert:
  - `CustomerName` mit `Trafag Italia` aus externem IT-Finance-Ist ausschliessen.
  - Doppelte Einzelpositionen mit leerem `Supplier country` nur einmal zaehlen.

Klaerung:

- Neuer IT-Export muss gegen diese Methode geprueft werden.
- Finance / Italien muss bestaetigen, ob die aktuelle lokale Rhino-/B1-Auswertung dieselben Filter nutzt.
- Der bisherige harte HANA-Filter sollte mittelfristig in eine pflegbare Finance-Regel ueberfuehrt werden.

Argument:

Die alte Kundenausschluss-Kombination passte fuer 2025 rechnerisch naeher, ist aber fachlich nicht belastbar fuer Folgejahre. Die bestaetigte Methode ist wichtiger als eine einmalig passend gerechnete Zahl.

### 3. Spanien

Aktueller Stand:

- Sage-CSV ist technisch importierbar.
- `ImporteNeto` wird als Nettozeile verwendet.
- Credit Notes / REC laufen negativ.
- Ist aktuell ca. `3'082'320.18 EUR`.
- Sitzung 2026-06-01: Dieser Ist-Wert entspricht fachlich dem erwarteten Wert.
- Der bisherige Sollwert ca. `3'102'333.61 EUR` ist falsch bzw. wahrscheinlich ein Excel-/Referenzfehler.

Klaerung:

- Falschen Sollwert in `check.xlsx` / FinanceReference korrigieren.
- Klaeren, woher der falsche Sollwert `3'102'333.61 EUR` kam.
- Danach ES nicht mehr als fachliche Abweichung fuehren, sofern die Referenz korrigiert ist.

Argument:

Spanien ist technisch angebunden und fachlich plausibel. Die bisherige Abweichung entsteht aus einer falschen Soll-/Referenzzahl, nicht aus dem Sage-Ist.

### 4. UK

Aktueller Stand:

- UK ist fachlich Sage, nicht SAP B1.
- `UK_B1` ist nur der SharePoint-Ordnername.
- Fuehrende Waehrung ist GBP.
- Mapping: `Sales Price/Value * Quantity`.
- Credit Notes werden negativ behandelt.
- Ist aktuell ca. `3'533'710.09 GBP`.
- Sollwert ca. `3'538'972.00 GBP`.
- Differenz ca. `-5'261.91 GBP`.

Klaerung:

- Ist der Sage-Export fuer 2025 vollstaendig?
- Sind Discounts, Freight, Charges oder andere Sage-Felder im Sollwert enthalten?
- Gibt es UK-spezifische Intercompany-/2nd-party-Abgrenzungen?

Argument:

UK darf nicht mit SAP-B1-Regeln erklaert werden. Die Abweichung muss ueber Sage-Exportlogik und fachliche Sage-Filter geklaert werden.

### 5. CH / AT

Aktueller Stand:

- CH und AT kommen aus `ZSCHWEIZ`.
- Trennung erfolgt ueber Buchungskreis bzw. Reporting-Land.
- Mapping verwendet aktuell `FKDAT` fuer `PostingDate` und `InvoiceDate`.

Klaerung:

- Ist `FKDAT` fachlich das richtige Datum fuer die Jahresabgrenzung?
- Falls nicht: Welches SAP-Feld ist das echte Buchungsdatum?

Argument:

Die Datenquelle ist angebunden. Der kritische Punkt ist, ob Finance die aktuelle Datumsbasis als Buchungsdatum akzeptiert.

## Punkte, die nicht verwechselt werden duerfen

- `Finance Summary` ist fuehrend; `Management Analyse` erklaert und prueft.
- Intercompany wird separat gezeigt, aber nicht automatisch aus dem Standard-Ist entfernt.
- UK ist Sage, nicht SAP B1.
- DE ist Alphaplan, nicht SAP B1.
- Spartenanalyse nutzt TR-AG-/SAP-Referenz als zentrale Wahrheit.
- Wenn in der Sparten-Finanzanalyse mehr als 90% nicht zugeordnet sind, ist das nicht als fachliche Wahrheit zu akzeptieren, sondern als Mapping-/Referenzproblem zu pruefen.
- Budget-CHF ist Kontrollsicht, nicht Standardabgleich.
- Eine Zahl, die zufaellig naeher am Soll ist, ist nicht automatisch die richtige fachliche Methode.

## Vorschlag fuer heutige Entscheidungen

1. Finance bestaetigt pro Land die offizielle Vergleichslogik:
   - Quelle
   - Datum
   - Wertfeld
   - Waehrung
   - Filter
   - Intercompany-Behandlung

2. Finance entscheidet die CHF-Logik:
   - Hauswaehrung bleibt fuehrend fuer lokale Freigabe
   - CHF als separate Reporting-Sicht
   - offizieller Kurstyp und Kurstabelle
   - Datumsfeld fuer Kursanwendung, z. B. `DocDate`, `PostingDate` oder `InvoiceDate`
   - Anzeige im Dashboard, welches Datum fuer den Kurs verwendet wird

3. Finance entscheidet den Kostenumfang:
   - vorerst keine offizielle Kosten-KPI
   - oder Diagnose mit vorhandenen Kostenfeldern
   - oder definierte Kostentabelle / Kostenquelle

4. Finance priorisiert die offenen Laender:
   - DE: Kundenlaender / Filter
   - ES: Referenz-/Sollwert korrigieren, keine echte Ist-Abweichung laut Sitzung
   - UK: Sage-Differenz
   - IT: neuer Export und finale Abgrenzung
   - Spartenanalyse: >90% nicht zugeordnet fachlich unplausibel, Mapping pruefen

5. Finance liefert fuer jede offene Differenz entweder:
   - offizielle Reportfilter,
   - eine lokale Auswertung mit Filterbeschreibung,
   - oder eine fachliche Akzeptanz der Restabweichung.

## Konkrete Fragen fuer Finance

1. Welche Zahl ist pro Land offiziell fuehrend: lokale ERP-Auswertung, Rhino, `check.xlsx` oder eine andere Quelle?
2. Werden Intercompany-/2nd-party-Umsaetze im offiziellen Wert ausgeschlossen oder nur separat analysiert?
3. Gilt die Jahresabgrenzung in allen Laendern nach Buchungsdatum?
4. Welche Laender duerfen mit kleiner Restabweichung als akzeptiert gelten?
5. Welche Differenzen muessen zwingend vor Freigabe erklaert werden?
6. Wer bestaetigt fuer DE, ES, UK und IT die finalen Filter fachlich?
7. Soll CHF ein offizieller Vergleichswert oder nur eine Reporting-Sicht sein?
8. Welcher Kurstyp ist fuer CHF verbindlich?
9. Sollen Kosten jetzt Bestandteil des Finance Dashboards werden oder separat als naechste Ausbaustufe?
10. Welche Kostenquelle waere fachlich fuehrend?
11. Sind Intercompany-Umsaetze in den Standortquellen bereits herausgerechnet?
12. Auf welches Datum muessen 2025-Wechselkurse angewendet werden?
13. Warum sind in der Sparten-Finanzanalyse mehr als 90% nicht zugeordnet, obwohl Andreas das fachlich ausschliesst?

## Quellen im Repo

- `docs/rag/FINANCE.md`
- `docs/FINANCE_ENTSCHEIDE.md`
- `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`
- `docs/FINANCE_IT_VORGEHEN_2026-05-18.md`
- `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md`
- `SAGE_SPAIN_EXPORT_2026-05-05.md`
- `lastchange.md`
