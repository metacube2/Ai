# Management Analyse: Tabs gegen Sales_All, 2026-09-29

Stand: 2026-09-29. Auftrag von Ingo: „mache abgleich". Nur lesend, kein App-Code geaendert.

## 1. Ergebnis

**Die Tabs rechnen richtig und stimmen untereinander und mit `Sales_All` ueberein.** Fuer 2025 und
2026 und jedes der neun Laender sind Summe und Zeilenzahl in allen Tabs gleich wie in
`Sales_All_2026-09-29.xlsx`, auf den Rappen genau.

**Die Italien-Zahl selbst ist aber falsch, und zwar schon in den Rohdaten:** Der B1-Export
verdoppelt jede Position, deren Artikel-Lieferant im Mandanten mehrere Rechnungsadressen hat.
Italien-Ist 2025 liegt dadurch um 3,79 Mio. EUR zu hoch, 2026 um 3,01 Mio. EUR. Das ist `ISS-021`,
Abschnitt 3.

## 2. Tabs gegen Sales_All

Pruefweg: Kopie der Produktiv-DB (Sicherung 29.09. 13:50), Kopien der Standort-Audit-CSV und von
`Sales_All` vom 29.09., echter `ManagementCockpitService` mit `CentralSalesDataProvider` wie
produktiv (Audit-CSV als Quelle, Kostenmodus Convert, Kursprofil CurrentDailyRate). Der gepruefte
Code entspricht dem produktiven Stand `a397440`. Werkzeug `.tmp_tools/CockpitTabAbgleich0929`,
Ausgabe `data/result.txt`.

Vergleichssumme aus `Sales_All`, Blatt `Sales`: Spalte `Finance | Net Sales Actual` der Zeilen mit
`Finance | Include = TRUE`, je `Finance | Year`, `Country Key`, `Currency`.

| Tab | Gepruefter Wert | Ergebnis |
| --- | --- | --- |
| Schnelluebersicht, Finance Summary | `Rows`, `NetSalesActual` | gleich, je Land und Summe |
| Laender, Abweichungen | `CountryRows`, `DeviationRows` | gleich; jede Abweichungszeile stimmt mit der Laenderzeile ueberein |
| 3D Datenanalyse | `YearCountryRows` | gleich |
| Spartenanalyse | `ProductFinanceCountryRows`; Spartenzeilen = zugeordnet + Sonstiges | gleich |
| Gruppenmarge | `GroupMarginCountryRows`, Summe Laender = Summe Sparten = Summary | gleich |
| Finance Pivot | Gesamtsumme = Summe der Monate = Gruppenmarge in CHF | gleich, keine Zeile ohne Kurs oder TSC |
| Management Entscheidungen | uebernimmt `DeviationRows`, Gruppenmarge, Produktzuordnung | kein eigener Rechenweg |

Beispiel 2025: CH 74'016'737.02 CHF (27'123 Zeilen), IT 9'642'342.19 EUR (14'429), IN
750'939'178.47 INR (4'004), in allen Spalten und in `Sales_All` gleich.

Bekannt und bestaetigt, nicht neu:

- `Net Sales Actual` ohne CHF-Schalter addiert Waehrungen („Mixed"), mit Warnhinweis. Entscheid
  Ingo vom 07.08. (`FINANCE_INDIKATOREN_PRUEFUNG_2026-08-07.md` 2b).
- Pruefbuch und Gruppenmargen-Detail liefern 1'000 von 58'756 (2025) bzw. 46'111 (2026) Zeilen
  (ISS-018.4).
- 2026 hat keine Sollwerte, deshalb ist der Tab Abweichungen fuer 2026 leer.
- Die Spartenzeilen enthalten nur zugeordnete Materialien. Indien ist fast nicht zugeordnet
  (2025 2,6 Mio. von 751 Mio. INR), ebenso die uebrigen Laender ausser CH.

**Neu beziffert, zu ISS-018.1:** Der CHF-Schalter aendert fuer 2025 Gruppenmarge und Pivot von
107'325'998.49 CHF (Kursprofil) auf 109'662'589.33 CHF (Jahresendkurs), also um 2,34 Mio. CHF.
Fuer 2026 sind beide Werte gleich. Der Pivot ist damit genauso betroffen wie die Gruppenmarge.

## 3. Befund Italien: Doppelzeilen aus der Lieferantenadresse (ISS-021)

Italien 2025 steht im Tab Laender bei 9'642'342.19 EUR gegen den Rhino-Sollwert 7'669'840 EUR
(+25,7 %). Im Mai lag Italien bei 7'669'641.47 EUR (`FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md`).

Gemessen mit demselben Code auf zwei Datenstaenden:

| Datenstand | Zeilen 2025 | eindeutige Positionen | doppelte Positionen | Wert der Zusatzzeilen | Wert ohne Doppelzeilen |
| --- | --- | --- | --- | --- | --- |
| 29.07. (Audit-CSV im Repo) | 12'258 | 9'994 | 2'264 | 1'806'787.81 EUR | 5'862'853.66 EUR |
| 29.09. | 14'974 | 9'994 | 4'980 | 3'789'405.26 EUR | 5'862'853.66 EUR |

Dieselben 4'241 Rechnungen und 9'994 Positionen, derselbe Wert ohne Doppelzeilen. Die
Doppelzeilen unterscheiden sich nur im Lieferantenland: 4'644 Paare Trafag AG mit CH und DE. 336
Paare Senseca Italy sind vollstaendig gleich. Beispiel: Rechnung 250104087, Position 0, Material
57314, 442 EUR, einmal mit `S_CH01_0065180/Trafag AG/DE` und einmal mit `.../CH`.

Ursache in `Services/HanaQueryService.cs` Zeile 560 und 622, gemeinsame Abfrage aller
B1-Standorte:

```sql
LEFT JOIN "CRD1" sup_adr ON itm."CardCode" = sup_adr."CardCode"
    AND sup_adr."AdresType" = 'B'
```

Das holt jede Rechnungsadresse des Lieferanten. Die Kundenadresse zwei Zeilen darueber ist mit
`Address = h."PayToCode"` auf eine Adresse beschraenkt, die Lieferantenadresse nicht.

**Warum die Regel vom 20.05. das nicht abfaengt:** `FinanceRuleEngine.ShouldInclude`
(`DeduplicateBlankSupplierCountry`, Zeile 60) zaehlt doppelte IT-Zeilen nur einmal, wenn das
Lieferantenland **leer** ist. Das war der damals aufgefallene Fall, und er ist seither behoben. Die
CH/DE-Paare und die IT/IT-Paare haben ein gefuelltes Land und fallen durch. Die Regel wirkt
ausserdem nur in der Finance-Sicht (Cockpit, Finance-Spalten in `Sales_All`), nicht in den
Rohzeilen der Exportdateien. Die Ursache in der Abfrage wurde nie korrigiert. Nach einer Korrektur
an der Quelle wird die Regel ueberfluessig, kann aber als Absicherung bleiben.

**Wirkung einer Korrektur:** Sie wirkt an der Quelle und damit auf alles, was danach entsteht:
Standortdateien, `Sales_All`, Audit-CSV, Nachweis-Excel, SharePoint-Upload und alle Tabs. Sie gilt
ab dem naechsten TRIT-Export und rueckwirkend fuer 2025 und 2026, weil der Export beide Jahre jedes
Mal neu liefert. Schon erzeugte Dateien bleiben unveraendert.

Warum es seit Juli mehr geworden ist, ist nicht gemessen. Vermutung: Mehr italienische Artikel
tragen jetzt Trafag AG als Lieferant, oder Trafag AG hat eine zweite Rechnungsadresse bekommen.

Andere Standorte, Datenstand 29.09.:

| TSC | doppelte Positionen 2025 / 2026 | Wert der Zusatzzeilen 2025 / 2026 | Ursache |
| --- | --- | --- | --- |
| TRIT | 4'980 / 3'573 | 3'789'405.26 / 3'014'800.15 EUR | Lieferantenadresse |
| TRFR | 10 / 10 | 20'256.00 / 8'796.80 EUR | Lieferantenadresse |
| TRIN | 17 / 13 | 3'521'534.94 / 4'099'475.00 INR | anders, Lieferantenland gleich; nicht untersucht |
| uebrige | 0 | 0 | |

**Folge fuer den Soll/Ist-Vergleich:** Der Rhino-Sollwert fuer Italien 2025 passte bisher nur,
weil 1,81 Mio. EUR Doppelzeilen darin steckten. Ohne sie liegt Italien 2025 bei 5,86 Mio. EUR,
also 1,81 Mio. EUR unter dem Sollwert. Die Italien-Methode muss danach neu abgestimmt werden.

**Nachtrag 2026-09-29, Korrektur umgesetzt (`4a1baa0`), produktiv seit 15:02 (`d8d425f`), wirksam ab dem naechsten Export.** Auf Ingos Auftrag
„fixe das mit den doppelten zeilen": Der Join nimmt die Standard-Rechnungsadresse des Lieferanten
(`OCRD."BillToDef"`), sonst eine feste Adresse je Lieferant (kleinstes Land). 772/772 Tests.
Live in `it01_p` nur lesend geprueft (`.tmp_tools/HanaSupplierAddress0929/check.sql` mit `HanaQ`):

- Trafag AG `S_CH01_0065180`: `BillToDef = main`, Adresse `main` = CH; die zweite Adresse
  `DE_FiscalRepresentative` = DE ist der Fiskalvertreter. Trafag AG bekommt damit CH.
- Senseca `S_IT01_0000106`: Standard `Bill to` (IT), zweite Adresse `SEDE OPERATIVA` (IT).
- Neue Verknuepfung ueber alle IT-Rechnungspositionen 2025 (ohne Umsatzfilter): 12'902 Zeilen
  fuer 12'902 Positionen, keine Doppelung mehr. SAP nimmt die Abfrage an.

Erwartete Wirkung ab dem naechsten TRIT-Export: Italien 2025 −3,79 Mio., 2026 −3,01 Mio. EUR in
Oberflaeche und `Sales_All`. Andreas vor dem Deploy informieren.

## 4. Aussagegrenzen

- Kein Blick in den Browser: Die Chrome-Erweiterung war nicht verbunden. Geprueft sind die Werte,
  die der Service an die Seite liefert, nicht die Darstellung.
- Nicht nachgerechnet: Daten-Heartbeat und Datenstatus (Zaehlungen, keine Betraege), Rohdaten
  Diagnose (sagt selbst, dass sie nicht mit Finance abstimmbar ist), Rechenwege innerhalb der
  Gruppenmarge (ISS-018).
- Die Ursache der indischen Mehrfachpositionen ist nicht untersucht.

## 5. Nachmessung Konto 47005 gegen Rechnungspositionen, 2026-09-29 nachmittags

Auf Ingos Frage, ob es wirklich Doppelzeilen waren, wenn Rhino denselben Wert hatte. Nur lesend in
HANA `it01_p` (`.tmp_tools/HanaSupplierAddress0929/konto47005.sql` mit `HanaQ`), Jahr 2025, Konten
`47005%` ohne `4700504%`, also genau der Kontenfilter der Export-Abfrage.

Hauptbuch `JDT1` nach Belegart:

| Belegart | Erloes 2025 |
| --- | ---: |
| 13 Ausgangsrechnung | 7'778'937.29 |
| 14 Gutschrift | −67'036.56 |
| 30 manuelle Buchung | −9'734.35 |
| **Summe** | **7'702'166.38** |
| −3 Jahresabschluss (Saldo auf null) | −7'702'166.38 |

Die Summe entspricht dem Wert aus dem Screenshot der italienischen B1-Oberflaeche vom Mai
(7'702'146.38) und liegt 0,4 % ueber Rhino (7'669'840).

Rechnungspositionen `INV1` auf denselben Konten, jede Position einmal:

| Kunde | Positionen | Rechnungen 2025 |
| --- | ---: | ---: |
| uebrige Kunden | 9'912 | 5'925'610.13 |
| C_IT01_0022987 Faiveley Transport Italia | 475 | 1'001'386.45 |
| C_IT01_0309653 Fincantieri Nextech | 224 | 185'325.12 |
| C_IT01_0304885 Metal Work Service | 171 | 176'572.80 |
| C_IT01_0306928 System Ceramics | 158 | 173'372.50 |
| C_IT01_0306475 Elemaster | 51 | 166'403.50 |
| C_IT01_0306138 Wabtec MZT | 40 | 141'323.70 |
| **sechs ausgeschlossene Kunden zusammen** | 1'119 | **1'844'384.07** |

Gutschriften `RIN1`: 67'016.47, davon 4'260 bei den ausgeschlossenen Kunden.

**Befund:** Die sechs Kunden, die der Italien-Filter vom Mai ausschliesst
(`HanaQueryService.BuildRevenueAccountFilter`), sind echte externe Kunden, und ihr Umsatz steht im
Konto 47005. Ihr Ausschluss (1,84 Mio. EUR) glich die Doppelzeilen (1,81 Mio. EUR) fast genau aus.
Nur deshalb traf das Cockpit im Mai den Rhino-Wert. Alle Rechnungspositionen einmal gezaehlt, ohne
Kundenausschluss und abzueglich der Gutschriften, ergeben 7'702'978 EUR und damit das Hauptbuch.

**Folge:** Mit der Korrektur von 15:02 allein faellt Italien auf rund 5,86 Mio. EUR und liegt dann
1,8 Mio. unter Rhino. Richtig ist erst die Kombination: Doppelzeilen weg und Kundenausschluss weg.
Offen ist noch, ob die Finance-Regel fuer Trafag Italia (Kundenname) danach weiter noetig ist.

**Nachtrag 15:23: Kundenausschluss entfernt (`f77840a`, produktiv).** Auf Ingos Auftrag „ja
ausschluss entfernen". Herkunft: provisorischer Prueffilter vom 2026-05-18
(`FINANCE_IT_VORGEHEN_2026-05-18.md`, „Arbeits-/Prueffilter, nicht fachlich freigegeben"). Am
2026-05-20 legte der Finance-Leiter eine andere Methode fest und die Doku nannte die Kundenkombination
„keine belastbare Methode fuer Folgejahre"; aus dem Code wurde sie trotzdem nie entfernt. Der
Kontenfilter `47005%` ohne `4700504%` bleibt. Erwartet ab dem Export am 30.09.: Italien 2025 rund
7,70 Mio. EUR, 0,4 % ueber Rhino. Dann nachmessen, auch ob die Finance-Regel fuer Trafag Italia
noch greift.

## 6. Nachweis 2026-09-30: Sales_All vom 30.09. gegen 29.09.

Manueller Lauf 30.09. 06:42-07:09 mit beiden Korrekturen. Vergleich der Blaetter `Sales`
(Werkzeug `CompareSalesAll.cs`):

- Mehrfachpositionen: TRIT 8'553 -> 0, TRFR 20 -> 0, TRIN 30 -> 0, alle anderen 0.
- Italien 2025: 9'642'342.19 -> **7'697'203.62 EUR**, Rhino 7'669'840 (+0,36 %), Hauptbuch 47005
  7'702'166.38. Italien 2026: 8'185'962.55 -> 6'484'585.82 EUR.
- Frankreich 2025 −20'256.00 EUR, Indien 2025 −3'521'534.94 INR (die Doppelzeilen).
- AT, CH, DE, ES, UK, US 2025 unveraendert; 2026 nur Zuwachs durch neue Rechnungen.
- Trafag AG steht in Italien jetzt mit CH (8'893 Zeilen).

Damit passt Italien zu der Erwartung von Andreas aus der Vorwoche. `ISS-021` und `ISS-030` erledigt.
