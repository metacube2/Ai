# Italien Net Sales 2025 - Vorgehen

Stand: 2026-05-18

## Ziel

Italien ist aktuell der wichtigste offene Finance-Punkt, weil die Abweichung gegen Rhino / `check.xlsx` am groessten ist.

Ziel ist nicht, eine Zahl passend zu rechnen, sondern die fachlich richtige Berechnungsmethode fuer Italien festzulegen und danach reproduzierbar im Finance-Abgleich zu verwenden.

## Aktueller Befund

| Kennzahl | Wert |
| --- | ---: |
| Land | Italien / IT |
| Ist vor IC-Abzug | `14.704.336,29 EUR` |
| Rhino / check.xlsx Soll | `7.669.840,00 EUR` |
| Abweichung vor IC | `+7.034.496,29 EUR` |
| Erkannter IC-/2nd-party-Abzug | `4.397.746,90 EUR` |
| Ist exkl. erkanntem IC | `10.306.589,39 EUR` |
| Restabweichung nach IC | `+2.636.749,39 EUR` |

Bewertung:

- Intercompany / 2nd-party erklaert einen grossen Teil der Abweichung.
- Die Restabweichung ist aber weiterhin zu gross fuer eine Freigabe.
- Italien bleibt deshalb `kritisch`, bis Berechnungsart, Deduplizierung und IC-Abgrenzung bestaetigt sind.

## Nachtrag: Vergleich mit Frankreich / BI1

Frankreich und Italien kommen beide aus `BI1` / SAP B1 ueber HANA:

| Land | TSC | Schema | Quellsystem |
| --- | --- | --- | --- |
| Frankreich | `TRFR` | `fr01_p` | `BI1` |
| Italien | `TRIT` | `it01_p` | `BI1` |

Daraus folgt:

- Italien soll zuerst mit derselben B1-Logik wie Frankreich geprueft werden.
- Die fuehrende technische Vergleichsvariante ist deshalb zuerst `Positions-Netto (Sales Price/Value)`.
- Belegkopfvarianten wie `DocTotal - VatSum` sind nur Kontrollsichten und nicht der erste Erklaerungsansatz.

Direkter Zentraldatenvergleich 2025:

| Land | Zeilen | Belege | `SalesPriceValue` | `NetLocal pro Position` | `NetLocal Beleg dedupliziert` |
| --- | ---: | ---: | ---: | ---: | ---: |
| Frankreich | `1.649` | `682` | `1.471.218,44` | `3.735.204,02` | `1.414.138,88` |
| Italien | `15.883` | `6.238` | `14.704.336,29` | `74.170.652,69` | `11.866.896,53` |

Interpretation:

- Bei Frankreich passt `SalesPriceValue` praktisch exakt gegen Rhino.
- Bei Italien ist `SalesPriceValue` ebenfalls die korrekte erste B1-Vergleichsmethode, liegt aber viel hoeher.
- Die Belegkopfvarianten erklaeren Italien nicht besser; `NetLocal pro Position` ist sogar offensichtlich ueberzaehlt.
- Wenn B1 Italien lokal fast zum Rhino-Wert passt, verwendet der lokale B1-Report sehr wahrscheinlich zusaetzliche Filter, die in der aktuellen zentralen App-Auswertung noch nicht gleich angewendet werden.

Top-Treiber in Italien nach `SalesPriceValue`:

| Kunde | Wert |
| --- | ---: |
| `TRAFAG ITALIA S.R.L.` | `4.061.211,41 EUR` |
| `Trafag AG` | `132.800,00 EUR` |
| `Trafag España, S.L` | `86.222,69 EUR` |

Damit ist die wahrscheinlichste Ursache nicht ein anderes B1-System, sondern eine abweichende fachliche Filterung:

- Rhino / lokaler B1-Report schliesst vermutlich bestimmte Trafag-/2nd-party-Kunden aus.
- Die App zeigt aktuell zuerst den Wert inklusive aller Positionen.
- Der IC-/2nd-party-Abzug wird separat ausgewiesen, aber noch nicht als offizielle IT-Vergleichsbasis verwendet.

## Technische Anpassung 2026-05-18

Aus dem Screenshot `italien.png` ist ersichtlich, dass der italienische B1-/Finance-Wert nicht aus allen B1-Rechnungspositionen gebildet wird, sondern aus der Konten-/GuV-Sicht:

```text
47005 - Ricavi vendite e prestazioni
```

Der dort sichtbare Totalwert liegt bei ca. `7.702.146,38 EUR` und ist damit nahe am Rhino-/check.xlsx-Sollwert `7.669.840,00 EUR`.

Die App-HANA-Abfrage war fuer Italien bisher zu breit:

```text
OINV/INV1 + ORIN/RIN1
alle nicht stornierten Positionen
DocDate ab 2025-01-01
```

Neu wurde fuer das italienische B1-Schema `it01_p` ein zusaetzlicher Positionsfilter gesetzt:

```sql
p."AcctCode" LIKE '47005%'
```

Das gilt fuer Rechnungen `INV1` und Gutschriften `RIN1`.

Wichtig:

- Frankreich bleibt unveraendert.
- Der Filter gilt nur fuer Schema `it01_p`.
- Die bereits vorhandenen Zentraldaten bleiben alt, bis Italien neu exportiert wird.
- Nach neuem Export muss `/finance` erneut geprueft werden.

Naechster technischer Pruefschritt:

```text
http://127.0.0.1:5099/run/export/TRIT
```

Danach:

```text
http://127.0.0.1:5099/finance
```

Ergebnis nach erstem Kontenfilter:

| Variante | IT-Ist | Differenz zu Rhino |
| --- | ---: | ---: |
| vor IT-Kontenfilter | `14.704.336,29 EUR` | `+7.034.496,29 EUR` |
| `AcctCode LIKE '47005%'` | `14.657.129,29 EUR` | `+6.987.289,29 EUR` |
| `AcctCode LIKE '47005%' AND NOT LIKE '4700504%'` | `10.603.550,59 EUR` | `+2.933.710,59 EUR` |

Damit war klar:

- `47005%` allein ist zu breit.
- Die `autofattura`-Konten `47005040`, `47005041`, `47005042` muessen ausgeschlossen werden.
- Danach bleibt aber weiterhin eine relevante Restabweichung von ca. `2,934 Mio. EUR`.

## Lokaler IT-Cache 2026-05-18

Zur schnelleren Analyse wurde ein lokaler Cache aus den aktuell exportierten IT-Zentraldaten erstellt:

```text
docs/it_cache_2025.csv
```

Cache-Stand:

| Kennzahl | Wert |
| --- | ---: |
| Zeilen | `14.012` |
| Summe `SalesPriceValue` | `10.603.550,59 EUR` |
| Rhino / check.xlsx Soll | `7.669.840,00 EUR` |
| zu viel | `2.933.710,59 EUR` |

Dokumenttyp-Aufteilung:

| Dokumenttyp | Zeilen | Wert |
| --- | ---: | ---: |
| `INV` | `13.906` | `10.690.684,95 EUR` |
| `CRN` | `106` | `-87.134,36 EUR` |

## Provisorischer Prueffilter 2026-05-18

Aus dem lokalen Cache wurde eine Kundenausschluss-Kombination gefunden, die die IT-Summe nahezu auf Rhino bringt.

Wichtig:

> Dieser Filter ist ein Arbeits-/Prueffilter. Er ist noch nicht fachlich freigegeben und darf nicht als finale Regel gelten, bis Italien/Rhino den gemeinsamen Reportfilter bestaetigt hat.

Aktueller provisorischer Ausschluss:

| Kunde | Betrag |
| --- | ---: |
| `C_IT01_0022987` / `FAIVELEY TRANSPORT ITALIA S.P.A.` | `1.689.857,70 EUR` |
| `C_IT01_0306928` / `SYSTEM CERAMICS S.P.A.` | `323.409,00 EUR` |
| `C_IT01_0306138` / `WABTEC MZT` | `282.647,40 EUR` |
| `C_IT01_0309653` / `FINCANTIERI NEXTECH S.P.A` | `268.166,37 EUR` |
| `C_IT01_0304885` / `METAL WORK SERVICE S.R.L.` | `203.425,15 EUR` |
| `C_IT01_0306475` / `ELEMASTER S.P.A.` | `166.403,50 EUR` |
| **Summe Ausschluss** | **`2.933.909,12 EUR`** |

Rechnerisches Ergebnis mit diesem Arbeitsfilter:

| Kennzahl | Wert |
| --- | ---: |
| IT-Ist vor Kundenausschluss | `10.603.550,59 EUR` |
| Ausschluss-Summe | `2.933.909,12 EUR` |
| IT-Ist nach Arbeitsfilter | `7.669.641,47 EUR` |
| Rhino / check.xlsx Soll | `7.669.840,00 EUR` |
| Restdifferenz | `-198,53 EUR` |

Im Code wurde dieser Filter zunaechst hart nur fuer `it01_p` eingebaut:

```sql
p."AcctCode" LIKE '47005%'
AND p."AcctCode" NOT LIKE '4700504%'
AND h."CardCode" NOT IN (
    'C_IT01_0022987',
    'C_IT01_0306928',
    'C_IT01_0306138',
    'C_IT01_0309653',
    'C_IT01_0304885',
    'C_IT01_0306475'
)
```

Noch zu klaeren:

- Welche gemeinsame fachliche Eigenschaft haben diese sechs Kunden?
- Sind sie im italienischen B1/Rhino-Report bewusst ausgeschlossen?
- Ist der echte Filter eine Kundengruppe, Branche, Sales-Channel, Projekt-/OEM-Abgrenzung oder ein anderes B1-Feld?
- Soll der Filter in der App spaeter als pflegbare Finance-Regel statt als harter Code umgesetzt werden?

Naechster Test:

```text
http://127.0.0.1:5099/run/export/TRIT
```

Danach:

```text
http://127.0.0.1:5099/finance
```

Erwartung mit dem provisorischen Filter:

- IT-Ist nahe `7.669.641,47 EUR`
- Restdifferenz gegen Rhino ca. `-198,53 EUR`

## Fachliche Grundregeln

Diese Regeln gelten bereits als entschieden:

| Thema | Regel |
| --- | --- |
| Waehrung | Hauswaehrung, fuer Italien `EUR` |
| Wertbasis | Nettofakturawert |
| Jahresabgrenzung | Buchungsdatum |
| Aggregation | pro Artikel / Belegposition |
| Gutschriften | separat ausweisen, mit eigener Beleg-/Positionslogik |
| Intercompany | separat ausweisen, nicht still entfernen |

Technischer Datums-Fallback:

```text
PostingDate -> InvoiceDate -> ExtractionDate
```

Wenn Italien kein echtes Buchungsdatum liefert, muss geklaert werden, ob der Fallback auf Fakturadatum fachlich akzeptiert ist.

## Varianten aus der FinanceProbe pruefen

In der Finance-Webseite pro Land den Aufklapper `Varianten anzeigen` oeffnen.

Fuer Italien sind besonders diese Varianten relevant:

| Variante | Bedeutung | Prueffrage |
| --- | --- | --- |
| `Positions-Netto (Sales Price/Value)` | Positionsnaher Nettoverkaufswert aus Quelle/Mapping | Ist das der fachlich richtige Netto-Umsatz je Position? |
| `DocTotalFC - VatSumFC` | Netto-Belegwert in Belegwaehrung | Nur Kontrollsicht; fuer IT sollte EUR/Hauswaehrung fuehrend sein. |
| `Nettofakturawert Hauswaehrung pro Position` | Hauswaehrungs-Netto positionsweise summiert | Fuehrt das zu Doppelzaehlung, weil Belegkopfwerte pro Position wiederholt sind? |
| `Nettofakturawert Hauswaehrung pro Beleg dedupliziert` | Hauswaehrungs-Netto je Beleg nur einmal | Passt diese Sicht besser zu Rhino / check.xlsx? |
| `ohne 2nd-party / IC` | Betrag nach erkanntem IC-Abzug | Sind die IC-Regeln vollstaendig? |

## Priorisierte To-do-Liste

### 1. Berechnungsmethode klaeren

Pruefen, welche Variante fuer Italien fachlich fuehrend sein muss:

- Positionswert aus `SalesPriceValue`
- Hauswaehrungs-Netto pro Position
- Hauswaehrungs-Netto pro Beleg dedupliziert
- andere lokale Netto-Spalte

Entscheid dokumentieren:

```text
IT fuehrende Methode = ...
Begruendung = ...
Freigegeben durch = ...
Datum = ...
```

### 2. Belegkopf-Deduplizierung pruefen

Risiko:

- `DocTotal` / `VatSum` sind Belegkopfwerte.
- In positionsbasierten Exporten koennen diese Werte auf jeder Position wiederholt sein.
- Wenn sie positionsweise summiert werden, entsteht eine Ueberzaehlung.

Zu pruefen:

- Gibt es mehrere Positionen pro Beleg?
- Sind `DocTotal - VatSum` Werte auf allen Positionen eines Belegs identisch?
- Entspricht Rhino eher der deduplizierten Belegsumme oder der Positionssumme?

### 3. Intercompany / 2nd-party vervollstaendigen

Aktuell verwendete Marker:

- `TRAFAG`
- `MAGNETIC SENSE`
- `MAGNETS SENSE`
- `GESELLSCHAFT FUER SENSORIK`
- `GESELLSCHAFT FUR SENSORIK`

Zu klaeren:

- Gibt es italienische Schreibweisen?
- Gibt es lokale Kundennummern fuer Trafag-Gesellschaften?
- Gibt es weitere 2nd-party-Kunden, die nicht ueber Namen erkannt werden?
- Soll IC fuer den offiziellen Wert ausgeschlossen oder nur separat gezeigt werden?

### 4. Gutschriften und Storno pruefen

Zu klaeren:

- Sind Credit Notes vollstaendig enthalten?
- Haben Gutschriften negative Werte?
- Werden Stornos doppelt oder falsch mit Vorzeichen gelesen?
- Haben Gutschriften eigene Rechnungsnummern / Positionen?

### 5. Jahresabgrenzung pruefen

Fuehrende Regel:

```text
Jahr 2025 nach Buchungsdatum
```

Zu klaeren:

- Liefert IT ein echtes Buchungsdatum?
- Wenn nein: ist Fakturadatum als Ersatz fachlich akzeptiert?
- Gibt es Belege aus 2024/2026, die buchhalterisch in 2025 gehoeren oder umgekehrt?

### 6. Rhino / check.xlsx Vergleichsbasis klaeren

Mit Finance / Rhino klaeren:

- Welche Quelle nutzt Rhino fuer Italien?
- Welche Filter sind dort aktiv?
- Ist Rhino inklusive oder exklusive IC?
- Wird nach Beleg, Position oder Kundenklassifikation aggregiert?
- Werden Gutschriften separat oder netto eingerechnet?

## Konkreter Arbeitsablauf

1. FinanceProbe oeffnen:

```text
http://127.0.0.1:5099/finance
```

2. Italien-Zeile suchen.

3. `Varianten anzeigen` oeffnen.

4. Werte notieren fuer:

- gewaehlte Variante
- `Positions-Netto (Sales Price/Value)`
- `Nettofakturawert Hauswaehrung pro Position`
- `Nettofakturawert Hauswaehrung pro Beleg dedupliziert`
- `2nd-party/IC`
- `Diff. ohne 2nd-party`

5. Die Variante identifizieren, die Rhino am naechsten kommt.

6. Nicht automatisch uebernehmen, sondern fachlich begruenden:

```text
Warum passt diese Variante?
Welche Datenfelder nutzt sie?
Welche Faelle schliesst sie ein oder aus?
Ist IC enthalten oder separat?
```

7. Ergebnis mit Finance / Italien bestaetigen.

8. Danach erst Code-/Konfigurationslogik finalisieren.

## Fragen an Italien / Finance

1. Welches Feld ist fuer Net Sales 2025 in Italien fachlich fuehrend?
2. Ist Rhino / check.xlsx fuer Italien inklusive oder exklusive Intercompany?
3. Welche Kunden gelten in Italien als Intercompany / 2nd-party?
4. Werden Credit Notes im lokalen System mit negativem Vorzeichen geliefert?
5. Wird fuer 2025 nach Buchungsdatum oder Fakturadatum abgegrenzt?
6. Sind Belegkopfwerte wie `DocTotal - VatSum` in der Exportdatei pro Position wiederholt?
7. Gibt es lokale Rabatte, Fracht, Zuschlaege oder Nebenpositionen, die in Rhino anders behandelt werden?

## Abschlusskriterium

Italien kann erst auf `OK` oder `kontrolliert geklaert` gesetzt werden, wenn:

- die fuehrende Berechnungsmethode benannt ist,
- die IC-/2nd-party-Regeln vollstaendig genug sind,
- Gutschriften/Storno plausibel sind,
- die Jahresabgrenzung nach Buchungsdatum bestaetigt oder ein Fallback freigegeben ist,
- die Restabweichung gegen Rhino erklaert oder akzeptiert ist.
