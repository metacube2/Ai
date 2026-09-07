# Finance-Dashboard: Fachprüfung und Umsetzungsvorlage

Stand: 2026-09-07. Auftrag: Dokumentation und Berechnung auf innere Stimmigkeit und
betriebswirtschaftliche Fehler prüfen; Ergebnis so beschreiben, dass ein einfacheres
Modell die abgegrenzten Reparaturen umsetzen kann.

## 1. Ergebnis und Grenzen

**Das Dashboard ist derzeit nicht durchgängig fachlich konsistent.** Konkrete
Währungsfehler sind am aktuellen lokalen Code reproduziert. Weitere Risiken betreffen
Kostenrücknahmen, kostenlose Warenabgänge, fehlende Konzernkosten und Periodenzuordnung.
Die Bezeichnung Gruppenmarge darf nicht mit einer vollständig konsolidierten,
historischen Erfolgsrechnung gleichgesetzt werden.

Geprüft wurden der Finance-Dokumentationsast, relevante Standortdetails, aktuelle
Berechnungspfade und bestehende Tests. Die Produktivfreigabe
`\\trch-webapp-bidashboard.trafagch.local\BiDashboard$` war nicht erreichbar.
Deshalb sind **keine aktuell betroffenen Produktivbeträge** bestätigt. Historische
Messungen in den Quelldokumenten bleiben datierte Hinweise. Die folgenden Zahlen sind
ausdrücklich synthetische Rechenbeispiele, keine Unternehmenszahlen.

Es wurden keine Produktregeln, Produktivdaten oder Anwendungscodes geändert und nichts
deployed. Neu sind dieser Bericht und ein isoliertes Diagnoseprogramm.

## 2. Arbeitsauftrag für das umsetzende Modell

1. `router.md`, `docs/AGENT_COORDINATION.md` vollständig und `persona.md` lesen.
   Auftrag und konkrete Dateien reservieren; fremde Änderungen nicht übernehmen.
2. **Zuerst ausschließlich A1 bis A3 umsetzen**, jeweils als kleines Arbeitspaket.
   A2 vor A3 abschließen. Die vorhandenen Fachentscheide und Schalterdefaults beibehalten.
3. Pro Paket zuerst den beschriebenen Abnahmetest ergänzen. Er muss vor der Reparatur
   am tatsächlichen Fehler scheitern und danach bestehen.
4. Öffentliche Service-Ergebnisse prüfen, bei A2 zusätzlich den erzeugten Excel-Nachweis.
   Ein Test nur gegen eine neue Hilfsmethode genügt nicht.
5. B1 bis B6 sind **keine freigegebenen neuen Fachregeln**. Dort erst die konkrete
   Entscheidung dokumentieren, danach den beschriebenen Folgeauftrag ausführen.
6. Dokumentation nach Paket C berichtigen. Primärquellen und datierte Beschlüsse nicht
   überschreiben; falsche Schlussfolgerungen als solche kennzeichnen.
7. Relevante Tests, `git diff --check`, Ergebnis und verbleibende Einschränkungen
   dokumentieren. Koordination abschließen und Reservierung freigeben. Ein Deploy ist
   mit diesem Bericht nicht beauftragt.

Nicht im selben Zug umbauen: Importarchitektur, ERP-Bewertungsmethode,
Supplier-Kaskade, Sales-Type-Vorrang, MARC-Fallback, IC-Regeln oder Kursdefaults.
Keine fehlenden Kostendaten durch Schätzwerte ersetzen.

## 3. A – eindeutig abgrenzbare technische Reparaturen

### A1 – CHF-Konzernsumme trägt die falsche Währungsbeschriftung

**Priorität: hoch. Status: reproduzierter Programmfehler.**

Fundstelle: `Services/ManagementCockpitService.cs`, `BuildGroupMarginSummary`,
insbesondere Zeilen 1596–1612. Dort werden `SalesValueChf` und `CostBasisValueChf`
addiert, bei genau einer Verkaufswährung aber deren Label übernommen.
`Components/Pages/ManagementCockpit.razor` verwendet dieses Label an den Kacheln.

Reproduktion: Land IT, Umsatz 100 EUR, EUR/CHF 0.90. Das Ergebnis lautet numerisch
90, trägt aber `DisplayCurrency = EUR`. Richtig ist **90 CHF**.

Umsetzung:

- `ManagementGroupMarginSummary.DisplayCurrency` immer auf CHF setzen, da die
  zugehörigen Summen bereits CHF-Werte sind. Keine zweite Umrechnung hinzufügen.
- Landes-/Divisions-/Detailzeilen behalten ihre eigene Währung.
- Den widersprüchlichen Kommentar direkt über der Zuweisung korrigieren.
- A3 beachten: Der Deckungsbeitrag in derselben Summary ist noch nicht CHF-konform.

Abnahme: IT/EUR allein → 90 CHF; CH/CHF allein → unveränderter CHF-Betrag;
mehrere Länder → Summe ihrer CHF-Werte und Label CHF. Lokale IT-Zeile weiterhin
100 EUR. Öffentlichen Einstieg `AnalyzeFinanceSummaryAsync` verwenden.

### A2 – Kosten werden über unterschiedliche Kurse nach CHF gerechnet

**Priorität: hoch. Status: reproduzierter Programmfehler.**

Fundstellen:

- `ManagementCockpitService.BuildGroupMarginDetailRows`, etwa Zeilen 1517–1530:
  Kosten zuerst zum heutigen Kurs in Verkaufswährung, danach mit dem gewählten
  CHF-Profil nach CHF.
- `ManagementCockpitService.BuildFinanceAuditLedgerRows`, etwa Zeilen 1757–1836:
  ursprüngliche Kosten direkt mit dem gewählten Profil nach CHF.
- `Services/GroupMarginCostCurrencyConverter.cs`, `ResolveRateDate` und `Resolve`:
  die erste Umrechnung kennt nur den heutigen Tag.
- `Services/ExcelExportService.cs`: alle Aufrufstellen dieser Umrechnung und
  CHF-Ausgaben gemeinsam prüfen, nicht allein das Cockpit reparieren.

Reproduktion: Finance-Jahr 2025, Modus `FinanceYearEndRate`, Umsatz 100 EUR,
Konzernkosten 60 CHF. EUR/CHF am Jahresende 2025 = 0.90, heute = 0.80.

| Ergebnis | Heute im Code | Richtig für das CHF-Profil |
| --- | ---: | ---: |
| Umsatz CHF | 90.00 | 90.00 |
| Gruppenmarge: Kosten CHF | 67.50 | 60.00 |
| Gruppenmarge: Marge CHF | 22.50 | 30.00 |
| Prüfbuch: Kosten / Marge CHF | 60.00 / 30.00 | 60.00 / 30.00 |

Ursache: `60 / 0.80 * 0.90 = 67.50`. Ein CHF-Kostenbetrag darf durch einen
Zwischenschritt über EUR nicht seinen CHF-Wert verändern.

Umsetzung:

- CHF-Umsatz aus ursprünglichem Umsatz und seiner Währung berechnen.
- CHF-Kosten **direkt aus ursprünglicher Kostenbasis und Kostenwährung** berechnen.
  Für beide denselben durch `GroupMarginChfRateModes` bestimmten Stichtag und
  denselben Kursresolver verwenden.
- CHF-Marge = CHF-Umsatz minus CHF-Kosten. Nur mit fachlich bekannter Kostenbasis
  und verfügbaren Kursen als belastbare Marge ausgeben.
- Die ausdrücklich beschlossene lokale Tageskursanzeige kann daneben bestehen.
  Nicht still auf Jahresendkurs umstellen. Im Jahresendmodus ist die lokale
  Tageskursmarge dann keine umrechenbare Vorstufe der CHF-Marge.
- Maskierung beachten: `conversion.CostBasis` enthält im Mask-Pfad noch den Wert
  in Kostenwährung. Diesen niemals als Verkaufswährung multiplizieren oder addieren.
- Die fünf Ausgaben Cockpit-Gruppenmarge, Pivot, Prüfbuch, Nachweis und Sales_All
  anhand desselben Belegs vergleichen. Gemeinsame vorhandene Helfer nutzen; falls
  nötig eine kleine gemeinsame CHF-Berechnung ergänzen, keine neue Architektur.

Abnahme:

1. Obiges Beispiel liefert in allen CHF-Ausgaben Kosten 60 und Marge 30.
2. Tageskursmodus liefert Umsatz 80, Kosten 60, Marge 20.
3. Gleiche Umsatz-/Kostenwährung, Drittwährung als Kostenbasis und negative
   Rücklieferung mit gültiger Kostenrücknahme gesondert prüfen.
4. Fehlender Kurs oder Mask-Modus produziert keine unter falscher Währung
   aufsummierte Kostenbasis; offene Werte/Status bleiben erkennbar.
5. Rohwerte, gewähltes Kursprofil und angewandte Kurse bleiben nachvollziehbar.

### A3 – Deckungsbeitrag wird in der Konzernsumme unkonvertiert addiert

**Priorität: hoch, sobald variable Kosten geliefert werden. Status: reproduziert.**

Fundstelle: `ManagementCockpitService.SumContributionMargin`, etwa Zeile 1619.
Es wird `ContributionMarginValue` addiert. Laut
`ContributionMarginCalculator.Result` liegt dieser Wert in Verkaufswährung vor.
Die Summary ist jedoch eine CHF-Konzernsumme. Bei mehreren Währungen entsteht eine
dimensionswidrige Addition, bei einer Fremdwährung ein falscher CHF-Betrag.

Reproduktion: Umsatz 100 EUR, variable Kosten 20 EUR, EUR/CHF 0.90.
Summary-DB heute **80**, korrekt in CHF **72**. Bei derzeit fehlendem
Fix/Variabel-Split bleibt der Fehler verdeckt; die dokumentierten bisherigen
Importe liefern diese Felder noch nicht.

Umsetzung:

- Lokalen DB erhalten. Für die Konzernsumme einen eigenen nullable CHF-DB aus
  Umsatz und variablen Kosten mit dem in A2 vereinheitlichten Profil bilden.
- Nicht den bereits lokal zum Tageskurs umgerechneten DB blind multiplizieren,
  falls ursprüngliche variable Kosten eine andere Währung haben.
- Nur tatsächlich abgedeckte CHF-DB-Werte summieren. Fehlender Split bleibt null;
  explizit gelieferte variable Kosten 0 sind dagegen bekannt und zulässig.
- Abdeckungszahl zur summierten Menge passend berechnen. Keine Gesamtkonzern-
  DB-Aussage vortäuschen, wenn nur eine Teilmenge einen Split hat.

Abnahme: 100 EUR minus 20 EUR bei 0.90 → 72 CHF; zusätzlich 100 GBP minus
30 GBP bei 1.10 → Gesamt-DB 149 CHF. Ohne Split → null, mit explizitem Split 0
→ voller CHF-Umsatz als DB. Landeswerte bleiben 80 EUR bzw. 70 GBP.

## 4. B – fachliche Risiken mit konkretem Entscheidungsbedarf

### B1 – fehlende Konzernkosten fallen teilweise auf IC-Einkaufspreise zurück

**Priorität: hoch. Mechanismus reproduziert; fachliche Erweiterung offen.**

`GroupMarginCostRules.GroupDistributionWithoutGroupCost` schützt ausschließlich
Sales Type LRD. Eine IT-Zeile mit Lieferant Trafag AG, fehlendem Konzernkostensatz,
Umsatz 100 und lokalem Standardpreis 70 erhält dagegen `OK`, Kosten 70 und Quelle
`Interner Standardpreis`. Der lokale Einkaufspreis kann die interne Gewinnmarge
enthalten. Das ist keine bestätigte Konzernherstellkostenbasis.

Entscheidung für Andreas: Soll bei erkanntem **anderen** Konzernlieferanten ohne
passenden Konzernkostensatz grundsätzlich `Konzernkosten fehlen` gelten?
Eigenfertigung am verkaufenden Standort ausdrücklich getrennt entscheiden.
Nicht pauschal alle Intern-Zeilen sperren: FFM/CM können eigene Fertigung darstellen.

Nach Bestätigung: Regel in `GroupMarginCalculator.cs` vor `LocalStandardCost`
ergänzen; vorhandenen Status und dessen zentrale Behandlung wiederverwenden.
Abnahmefälle: IT←CH ohne Treffer offen; IT←CH mit Treffer korrekt; LRD unverändert;
echter Drittlieferant unverändert; lokale Eigenfertigung entsprechend Entscheid.
Cockpit und Excel müssen denselben Status/Kostenpfad zeigen.

Die dokumentierten 19'221 Zeilen ohne LRD mit erkanntem Konzernlieferanten sind
eine Kandidatenmenge, **keine bestätigte Fehleranzahl**. Tatsächliche Betroffenheit
benötigt einen Join gegen die aktuellen Konzernkosten.

### B2 – Menge null/zero, reine Wertgutschriften und kostenlose Waren

**Priorität: hoch. Fehlerverhalten reproduziert; Belegarten müssen getrennt werden.**

`GroupMarginCalculator.Magnitude` setzt bei Menge 0 den Stückpreis als Zeilenkosten
an, also implizit eine Einheit. `ContributionMarginCalculator` wiederholt dies.
Zusätzlich folgt das Kostenvorzeichen dem Umsatzvorzeichen.

Reproduktion: reine Preisgutschrift ohne Warenrücknahme, Umsatz −10, Menge 0,
Stückkosten 70 → Code Kosten −70, Marge **+60**. Wirtschaftlich wären Kosten 0,
Marge **−10** richtig. Bei einer echten Retoure kann eine Kostenrücknahme dagegen
richtig sein. Der Umsatz allein unterscheidet diese Fälle nicht.

Zweiter Befund: `AnalyzeFinanceSummaryAsync` setzt `Include = rawInclude &&
value != 0m` (etwa Zeile 378). Eine kostenlose Warenzeile mit Menge 1 und Kosten
60 wird dadurch aus den Gruppenmargendetails und deren Kosten entfernt.
Die Diagnose bestätigt: zusätzliche Gratiszeile, unveränderte Gesamtkosten.

Entscheidungsfragen: Welche Beleg-/Positionstypen belegen tatsächliche
Warenrücknahmen? Welche kostenlosen Warenabgänge gehören in diese Margensicht?
Wie wird eine wirklich fehlende Menge von einer gültigen Nullmenge unterschieden?

Nach Entscheidung:

- Zeilenkosten nicht durch eine erfundene Menge 1 ersetzen. Unbekannte Menge
  offen ausweisen; gültige reine Wertkorrektur mit bestätigten Kosten 0 behandeln.
- Warenretoure und Preis-/Bonusgutschrift anhand belegter Quellfelder trennen.
  Keine Textheuristik erfinden.
- Umsatzrelevanz und Kostenrelevanz getrennt behandeln. Kostenrelevante
  Nullumsätze aufnehmen, ohne Soll/Ist-Umsatz und dessen Zeilenzähler umzudeuten.
- Bekannte Nullkosten von fehlenden Kosten unterscheiden; Status-/Nullability-
  Anpassungen durch Cockpit, Prüfbuch und Excel durchführen.

Abnahme: normale Ware 100−60=40; echte Retoure −100−(−60)=−40;
Preisgutschrift −10−0=−10; Gratisware 0−60=−60, Prozentwert null;
fehlende Menge/Kosten offen. Gegen Quelle und Hauptbuch abstimmen.

### B3 – fehlende Kostenwährung wird als passende Währung behandelt

**Priorität: hoch bei betroffenen Daten. Mechanismus reproduziert.**

`GroupMarginCostCurrencyConverter.IsMismatch` verlangt zwei gefüllte Währungen.
Mit Umsatzwährung EUR, Kosten 60 und leerer Kostenwährung gibt `Resolve` daher
`IsMasked=false` zurück. Im Prüfbuch ersetzt ein eigener Fallback die fehlende
Kostenwährung durch die Umsatzwährung. Beides ist eine unbelegte Annahme.

Entscheidung: Welche Quellen garantieren bei fehlender Kostenwährung ausdrücklich
die Hauswährung? Nur solche belegten Quellverträge dürfen normalisieren.
Sonst Kostenwährung offen ausweisen und keine belastbare Marge rechnen.

Nach Entscheidung Converter, Prüfbuch und Excel gemeinsam ändern. Abnahme:
fehlende Währung ohne Quellgarantie offen; mit belegter Hauswährung korrekt
normalisiert; explizite gleiche/abweichende Währung unverändert korrekt.

### B4 – Konzernsumme ist nicht automatisch konsolidiertes Ergebnis

**Priorität: hoch für die Interpretation. Bekannte Scope-Grenze, kein neuer Beschluss.**

Die Finance-Aggregation markiert `IsIntercompany`; die Gruppenmarge filtert jedoch
auf `Include`, nicht generell auf externe Kunden. Die separate Größe Ist ohne IC
ersetzt keine vollständige Konsolidierung. SupplierType Intern beschreibt die
Beschaffung und ist nicht gleichbedeutend mit Innenumsatz auf Kundenseite.

Beispiel: CH produziert für 60 und verkauft intern für 100; eine Tochter verkauft
extern für 150. Bei Konzernkosten 60 auf beiden Verkaufszeilen ergibt Addieren
40+90=130. Die externe Marge der Kette beträgt 150−60=90.
Lagerbestände mit internen Gewinnen benötigen darüber hinaus eigene Eliminierungen.

Entscheidung: Ist die Kennzahl eine Standortsumme, eine externe Vertriebs-
Gruppenmarge oder eine konsolidierte Erfolgsrechnung? Erst dann IC-Abgrenzung und
Bezeichnung festlegen. Nicht sämtliche Intern-Supplier-Zeilen ausschließen.
Kontrollfall ist die obige zweistufige Kette einschließlich Bestandsfall.

Als fachlicher Vergleich verlangt IFRS 10 B86(c) die vollständige Eliminierung
konzerninterner Erträge/Aufwendungen und interner Ergebnisse. Ob Trafag nach IFRS
berichtet, wurde nicht festgestellt; dies ist kein behaupteter Compliance-Verstoß.
[Quelle: IFRS 10](https://www.ifrs.org/content/dam/ifrs/publications/pdf-standards/english/2022/issued/part-a/ifrs-10-consolidated-financial-statements.pdf?bypass=on).

### B5 – heutige Kosten und Kurse sind keine historischen Ist-Kosten

**Priorität: hoch für Vorjahresvergleiche. Bewusste Methodik mit offener Abgrenzung.**

`B1GroupStandardCostBuilder` verwendet standardmäßig den jüngsten positiven
Beleg-Stückkostenwert je Material und ersetzt beim Aufbau den jeweiligen
Kostenbestand. Die Tabelle hat keine periodengültige Beleghistorie. Damit können
neue Importe alte Jahresmargen verändern. Schweizer STPRS ist ebenfalls ein
aktueller Standardwert. Ein Konzernkostentreffer kann den aus WAVWR importierten
historischen lokalen Wert übersteuern.

`AveragePositive` berechnet außerdem den einfachen Durchschnitt der Zeilenwerte:
1 Stück zu 100 und 99 Stück zu 10 → **55**. Ein mengengewichteter Vergleich wäre
10.90. Auch dieser wäre noch kein ERP-Moving-Average, weil dafür Bestands- und
Zugangsvorgänge erforderlich sind. **Nicht einfach `Average` durch einen gewichteten
Durchschnitt ersetzen und das als gelöste Lagerbewertung bezeichnen.**

Entscheidung: Soll die Kennzahl aktuelle Standardkosten simulieren oder historische
Ist-/Standardkosten einer abgeschlossenen Periode darstellen? Entsprechend
Kennzahl/Kostenstand kenntlich machen oder periodengültige Kosten beschaffen.
Abnahme für eine historische Sicht: Ein Import 2026 verändert 2025 ohne explizite
Neubewertung nicht. Für eine aktuelle Simulation muss die Neubewertung erklärbar sein.

Ein heutiger Tageskurs oder ein pauschaler Jahresendkurs für alle Erfolgsgrößen
ist ebenfalls eine Managementbewertung. IAS 21.39–40 nennt für Erträge/Aufwendungen
Transaktionskurse bzw. geeignete Periodendurchschnitte; IAS 2.21 erlaubt
Standardkosten als Näherung, wenn sie tatsächliche Kosten annähern. Die bestehende
Managemententscheidung wird dadurch nicht automatisch falsch, benötigt aber eine
klare Abgrenzung zur Rechnungslegung.
[IAS 21](https://www.ifrs.org/content/dam/ifrs/publications/pdf-standards/english/2022/issued/part-a/ias-21-the-effects-of-changes-in-foreign-exchange-rates.pdf?bypass=on),
[IAS 2](https://www.ifrs.org/content/dam/ifrs/publications/pdf-standards/english/2021/issued/part-a/ias-2-inventories.pdf).

Mehrstufige Lieferketten zusätzlich beachten: StockPrice von Italien kann bereits
einen schweizerischen Transferpreis enthalten. Ein hoher Kostenquotient allein
beweist den Warenursprung jedoch nicht; Materialführung in MARC ist ebenfalls
kein Herstellnachweis. Keine pauschale Ausweitung Schweizer Kosten daraus ableiten.

### B6 – undatierte/unfakturierte Zeilen und abweichende Perioden in Marktsegmenten

**Priorität: hoch für Cut-off, mittel für Vergleichsberichte.**

`FinanceRuleEngine` fällt ohne Rechnungs-/Buchungsdatum auf `ExtractionDate` zurück.
Die Rechenprobe mit ES, Rechnungsnummer 0, Umsatz 100 und ohne beide Daten wird
eingeschlossen und dem 07.09.2026 zugeordnet. Die Spanien-Query exportiert
`BillingStatus`, filtert die Auswahl aber über das Datumsprädikat. Der dokumentierte
Delta-Befund vom 02.09. enthält tatsächlich unfakturierte Lieferscheinzeilen.
Ein Extraktionsdatum belegt keine Umsatzrealisierung.

Den entschiedenen ES-Vorrang `InvoiceDate` **nicht eigenmächtig rückgängig machen**.
Entscheidung getrennt einholen: Welche unfakturierten/undatierten Positionen dürfen
überhaupt in Actuals, und mit welchem belegten Leistungs-/Abgrenzungsdatum?
Unbekannte Fälle sollten sichtbar separat bleiben, statt mit jedem Import eine
scheinbare Finance-Periode zu erhalten. Auch Proforma-Belegarten im SAP-Pfad
brauchen einen aktuellen Statusnachweis; alte Zahlen belegen heutigen Einschluss nicht.

Nach Entscheidung Regel/Quellmapping ergänzen. Abnahme: undatierte Position wird
durch neuen Extraktionslauf nicht still einem anderen Geschäftsjahr zugeordnet;
echte ES-Rechnung folgt weiter dem beschlossenen Rechnungsdatum.

Zusätzlicher Vergleichsfehler: `MarketSegmentPageService` (etwa Zeilen 163/393)
und `MarketSegmentExportService` (etwa Zeilen 71/165) verwenden weiterhin
`PostingDate ?? InvoiceDate ?? ExtractionDate` und rohe Verkaufsdaten. Sie wenden
die Finance-Include-Regeln nicht an. ES-Rechnung vom 31.12.2025 mit Buchung
02.01.2026 erscheint damit im Marktsegment 2026, in Finance 2025.
Entscheiden: identischer Finance-Scope oder ausdrücklich separate Rohdatensicht.
Nur im ersten Fall die Finance-Regelauswertung wiederverwenden. Eine Behauptung
vollständiger Summengleichheit ist im aktuellen Zustand falsch.

## 5. C – Dokumentation berichtigen, keine weiteren Fachregeln erfinden

### C1 – Italiens B1-Bewertungsmethode: universelle Unmöglichkeit nicht belegt

`FINANCE_IT_BEWERTUNGSMETHODE_PAOLA_2026-09-04.md`, besonders Abschnitte 2–4,
und der Finance-Router verallgemeinern die Mail zu: bestehende B1-Artikel könnten
ihre Bewertungsmethode selbst bei Nullbestand nicht ändern; deshalb müssten rund
31'600 Artikel neu codiert werden.

SAP dokumentiert für Business One 10.0 ausdrücklich einen Methodenwechsel und
nennt Voraussetzungen wie Nullbestand in allen Lagern und keine offenen
bestandsrelevanten Belege. Das Handbuch zeigt auch Wechsel zwischen Serial/Batch
und Moving Average. SAP empfiehlt unter bestimmten Umständen neue Artikel wegen
möglicher Abweichungen; eine Empfehlung ist keine generelle technische Sperre.
[SAP-Handbuch, Abschnitte 3.2–3.4](https://help.sap.com/doc/b1665b97c2514be6bae63e5762c18c74/10.0/en-US/How_to_Set_Up_and_Use_Serial_Batch_Valuation_Method.pdf).

Umsetzung nur an der Einordnung: Mail unverändert als Primärquelle erhalten,
pauschale technische Schlussfolgerung und Routertitel korrigieren. Klar trennen:
Paolas Aussage zur eigenen Installation versus allgemein dokumentierte SAP-Funktion.
Version, Patchstand, konkrete Sperrmeldung, offene Belege und Besonderheiten mit
dem B1-Betreuer in einer Testumgebung klären. **Keine Artikelmigration oder Änderung
der Bewertung beauftragen.** Die Kostenfolgen für 31'600 Neucodierungen bleiben
bis dahin ein Szenario, keine nachgewiesene technische Notwendigkeit.

### C2 – konkrete Widersprüche und bereits erledigte Punkte

| Dokument / Stelle | Befund | Korrekturauftrag |
| --- | --- | --- |
| `docs/rag/FINANCE_FORMELN.md` | Teile beschreiben noch Mask-Default, alte Kosten-/Supplierlage und allgemeine PostingDate-Priorität | An aktueller Implementierung und datierten Beschlüssen ausrichten: Convert, IT/IN-Kosten und ES-InvoiceDate berücksichtigen |
| `FINANCE_LIEFERANT_STANDARDKOSTEN_WORKFLOW_2026-09-02.svg` | Nennt ES als SAP B1 und UK ohne Kostenspalte/0 %, während Standortdaten ES als Sage und UK mit Kosten ausweisen | Quellsysteme berichtigen; Füllgrade mit Datum/Grundgesamtheit ausweisen, aktuelle Werte erst messen |
| Dasselbe SVG, Schweizer Kostenschalter | Beschreibt fehlenden Guard als aktuellen Codefehler | Bereits repariert: `deliveringEntity is not null && ShouldUseSwissStprs(context)` steht im Code. Laut Koordination am 02.09. deployed; nicht erneut implementieren |
| Dasselbe SVG, Schlussabschnitt | Nutzt 22'950 alte Kandidaten trotz 18'981 im aktualisierten Hauptteil | Historische und aktuelle Grundgesamtheiten trennen |
| `FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` | Kopf/Schluss nennen Umsetzung offen; spätere Abschnitte und Code belegen Umsetzung. CM wird teils Pflegefall, teils kein Pflegefall genannt | Aktuellen Auftrag und Historie trennen; keine obsolete Pflegeanforderung versenden |
| `FINANCE_STANDARDKOSTEN.md`, `FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` | Historischer Belegkostenanspruch und aktuelle Konzernkostenübersteuerung werden nicht überall klar auseinandergehalten | Kostenquelle und zeitlichen Bewertungszweck explizit unterscheiden; B5 referenzieren |
| `FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` und Kursunterlagen | Statusaussagen zu vorbereiteten/produktiven Kursmodi sind teilweise zeitlich überholt | Aktuelle Codefähigkeit von datiertem Deploynachweis unterscheiden; A2 widerlegt derzeit vollständige Rechengleichheit |
| Issue-Log / offene Punkte | Einzelne Stati tragen ältere Aussagen trotz späterer Umsetzungen und Entscheide | Pro Issue letzten Beleg prüfen, nicht pauschal schließen; Codefix ist nicht dasselbe wie fachliche Freigabe oder aktuelle Datenabstimmung |

Originale Beschlüsse haben weiter Gültigkeit, bis sie ausdrücklich ersetzt sind.
Freigegebene Mixed-Summen in der alten Soll/Ist-Sicht sind ein bekannter Kompromiss,
keine mathematisch sinnvolle Konzernsumme. Ihre Warnung erhalten und nicht als
neuen unerkannten Programmfehler verkaufen.

## 6. Bereits stimmige Grundlagen erhalten

- Gemeinsamer `GroupMarginCalculator` für Cockpit und Excel verhindert wesentliche
  frühere Abweichungen. Nicht erneut duplizieren.
- Marge und Deckungsbeitrag sind begrifflich getrennt; ohne gelieferten variablen
  Kostenanteil bleibt der DB offen.
- Stückkosten/Zeilenkosten werden grundsätzlich unterschieden; Preiseinheit bei
  SAP-Standardkosten und Mengenbezug bei WAVWR bleiben nötig.
- Einfache echte Warenretouren werden vorzeichenrichtig zurückgerechnet. B2
  betrifft die unzulässige Verallgemeinerung auf sämtliche Gutschriften.
- LRD ohne Konzernkostentreffer bleibt bereits offen. Diesen Schutz erhalten.
- Journalimport ist ein eigener Datenbestand. Er ist nicht automatisch ein
  Nachweis, dass sämtliche Umsatz-/Margenzeilen mit dem Hauptbuch abgestimmt sind.
- Schweizer Kostenschalter-Guard und Excel-Defaultkorrektur vom 02.09. sind im
  aktuellen Code vorhanden. Keine Doppelreparatur aus alten Diagrammen ableiten.

## 7. Reproduzierbare Prüfung und Abschlusskriterien

Diagnoseprogramm: `.tmp_tools/FinanceReview0907/Program.cs`, Projekt im selben
Verzeichnis. Verwendet eine neue SQLite-In-Memory-Datenbank und den echten
`ManagementCockpitService`; keine App, keine Produktivverbindung.

```powershell
dotnet run --project .tmp_tools/FinanceReview0907 -c Release
```

Es bestätigt den **Fehlerzustand vor Reparatur**, nicht die fachliche Korrektheit.
Die vorhandenen Assertions am Ende erwarten bisheriges Verhalten. Das umsetzende
Modell soll daraus dauerhafte Regressionstests mit den korrekten Sollwerten machen;
das Diagnoseprogramm nach einer Reparatur nicht als Erfolgstest missverstehen.

Ergebnisse vom 07.09.2026:

```text
CHF_LABEL: sales=90.00, label=EUR; expected 90 CHF
RATE_PROFILES: group cost=67.50, margin=22.50; ledger cost=60, margin=30
CONTRIBUTION_SUM: 80.00; CHF expected 72
IC_MISSING: OK / Interner Standardpreis / 70; LRD bleibt offen
ZERO_QUANTITY_CREDIT: cost=-70, margin=60, status=OK
MISSING_COST_CURRENCY: masked=False, basis=60
AVERAGE: 55; quantity weighted comparator=10.9
UNDATED_ES: include=True, period=2026-09-07
FREE_GOODS: includes free goods=False; cost unchanged=True
```

Zusätzlich **235/235 bestehende relevante Tests bestanden**, keine übersprungenen.
Release-Build erfolgreich mit vorhandenen Razor-/Analyzerwarnungen.

```powershell
dotnet test TrafagSalesExporter.Tests/TrafagSalesExporter.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~FinanceRuleEngineTests|FullyQualifiedName~GroupMargin|FullyQualifiedName~CurrencyExchangeRateServiceTests|FullyQualifiedName~ContributionMarginCalculatorTests|FullyQualifiedName~B1GroupStandardCostBuilderTests|FullyQualifiedName~FinanceReconciliationServiceTests|FullyQualifiedName~ManagementCockpitServiceTests" --verbosity quiet
```

Grüne Bestandstests widerlegen die Befunde nicht: Sie decken gerade die gezeigten
Zusammenstellungen und fachlichen Abgrenzungen nicht vollständig ab.

Fertig ist ein Reparaturpaket erst, wenn sein neuer Test vor/nach der Änderung
aussagekräftig ist, alle betroffenen Ausgaben übereinstimmen, die dokumentierte
Kosten-/Kursbasis stimmt und offene Werte nicht als belastbare Marge erscheinen.
Produktive finanzielle Auswirkung anschließend read-only quantifizieren, sobald
die Quelle erreichbar ist; kein Betrag aus diesem Bericht darf als Live-Schaden gelten.

## 8. Prüfumfang und Einstiegspunkte

Dokumentationsweg: `router.md` → `docs/router/finance.md`, ergänzt über
`docs/router/standortdaten.md` um Spanien, Indien und Feldlücken.
Inhaltlich berücksichtigt wurden Finance-Kurzstand und Formeln, Fachentscheide,
Länderformeln, Datenfluss, Prozessablauf, Währungsworkflow, Gruppenmarge,
Standardkosten, Supplier einschließlich SVG, Italien-Bewertungsmethode, Journal,
WAVWR-Spezifikation, Nachweis, Schulung, Budgetfragen, Anzeige-/Indikatorenprüfung,
UK-Wertfehler, offene Punkte/Issue-Log sowie Marktsegmente/Railway und Exportkonzept.

Die ausführbaren Hauptpfade wurden gegen `FinanceRuleEngine`,
`ManagementCockpitService`, `ExcelExportService`, `GroupMarginCalculator`,
`GroupMarginSupplierClassifier`, `GroupMarginCostCurrencyConverter`,
`ContributionMarginCalculator`, `B1GroupStandardCostBuilder`,
`CurrencyExchangeRateService` und die Marktsegmentservices geprüft.

Keine Vollprüfung jeder historischen Exportdatei, jeder Office-Anlage und aller
ERP-Originalbuchungen. Insbesondere fehlen ein aktueller Live-Datenabgleich,
vollständige Hauptbuchabstimmung und Bestätigung installationsabhängiger SAP-Fragen.
Dieser Bericht ist eine technische und fachliche Prüfung mit umsetzbaren Befunden,
keine Bestätigung der Fehlerfreiheit des Rechnungswesens.
