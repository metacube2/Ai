# Finance Fragenkatalog: Budget-CHF-Spalten

Stand: 2026-06-15

Zweck: Dieses Dokument ist als fachlicher Fragenkatalog fuer den Finanzchef gedacht. Ziel ist eine zusaetzliche Budget-CHF-Sicht im zentralen Excel/Dashboard, ohne die bestehenden Local-Currency-Originalwerte zu ersetzen.

## Kurzfazit

Ein Teil ist bereits aus frueheren Finance-/Andreas-Dokumenten beantwortet. Nicht mehr grundsaetzlich offen ist:

- Fuehrende Finance-Sicht bleibt Local Currency / Hauswaehrung je Land.
- CHF ist eine separate Reporting-/Kontrollsicht.
- Fuer CHF-Budget sollen Budgetkurse verwendet werden, nicht Tageskurse/SNB/ECB.
- Indien wird fachlich in INR ausgewertet.
- Intercompany wird separat abgegrenzt und ausgewiesen.

Noch offen ist vor allem:

- Welche konkreten Budgetkurse je Finance-Jahr verbindlich sind.
- Welche Quelle/Freigabe fuer diese Budgetkurse gilt.
- Wie die neue Budget-CHF-Spalte im zentralen Excel und Dashboard genau benannt, gerundet und kontrolliert werden soll.

## Bereits beantwortet / nicht nochmals als offene Grundsatzfrage stellen

| Thema | Bisheriger Entscheid / Dokumentstand | Quelle |
| --- | --- | --- |
| Fuehrende Waehrung | Rechnungen werden in der Hauswaehrung / Local Currency des Landes ausgewertet. | `../entscheide.md`, `FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` |
| Standard-Soll/Ist | Standard-Ist je Land wird in Hauswaehrung auf Basis Nettofakturawert berechnet. | `../entscheide.md`, `FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` |
| CHF-Umrechnung | CHF-Sicht nur separat mit Budgetkursen; keine SNB-/Tageskurse fuer den Standardabgleich. | `../entscheide.md` |
| Budget-CHF-Rolle | Budget-CHF ist Kontroll-/Reporting-Kandidat und ersetzt nicht den Standardabgleich in Hauswaehrung. | `FINANCE_KURS_WORKFLOW_2026-06-09.md`, `rag/FINANCE.md` |
| Indien | Indien wird in INR ausgewertet; CHF-Budgetwert ist nicht der Hauptvergleich. | `../entscheide.md`, `FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` |
| Jahresabgrenzung | FinanceDate basiert auf PostingDate, sonst InvoiceDate, sonst ExtractionDate; DE kann per Finance-Regel auf 2025 gezwungen werden. | `FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` |
| Zentrales Excel | `Finance Summary` und `Finance Details` wenden keine UI-Zielwaehrung an; sie bleiben nach vorhandener Finance-Waehrung gruppiert. | `FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` |
| ERP-Belegkurs | `DocumentRate` ist Quellinformation aus ERP/SAP/B1, aber nicht automatisch der Kurs fuer Dashboard-/Budget-CHF. | `FINANCE_KURS_WORKFLOW_2026-06-09.md` |

## Vorschlag fuer die neue Analyse-Erweiterung

Die bestehenden Finance-Werte bleiben unveraendert:

```text
Finance | Net Sales Actual
Finance | Currency
Finance | Year
```

Es werden nur zusaetzliche Analyse-Spalten angehaengt:

```text
Finance | Budget Year
Finance | Budget Rate to CHF
Finance | Net Sales Actual CHF Budget
Finance | Budget Rate Source
Finance | Budget Rate Missing
```

Berechnungsvorschlag:

```text
Finance | Net Sales Actual CHF Budget
  = Finance | Net Sales Actual
    * Budgetkurs(Local Currency -> CHF, Finance | Budget Year)
```

Wichtig: Diese Budget-CHF-Spalte soll explizit nur Budgetkurse verwenden, also nicht die normale Anzeige-Umrechnung mit offenen ECB-Kursen. Der technische Grund ist, dass offene Tages-/ECB-Kurse sonst spaeter einen Budgetkurs uebersteuern koennen.

## Aktueller technischer Kursstand zur Bestaetigung

Aktuell sind Budgetkurse fuer 2025 und 2026 in der Datenbank vorhanden. Bitte fachlich bestaetigen oder durch Finance ersetzen lassen.

| Waehrung | Budget 2025 nach CHF | Budget 2026 nach CHF |
| --- | ---: | ---: |
| CHF | 1.000000 | 1.000000 |
| EUR | 0.950000 | 0.940000 |
| USD | 0.850000 | 0.800000 |
| GBP | 1.130000 | 1.090000 |
| INR | 0.01099989 | 0.00909091 |
| CNY | 0.11764706 | 0.11764706 |
| CZK | 0.03900156 | 0.03846154 |
| PLN | 0.220000 | 0.220000 |
| JPY | 0.00640000 | 0.00571429 |

Hinweis: In der allgemeinen Kurstabelle koennen daneben auch ECB-Tageskurse stehen. Fuer `Finance | Net Sales Actual CHF Budget` sollte deshalb nach aktuellem Verstaendnis nur `Budget <Jahr>` als Kursquelle erlaubt sein.

## Umsetzungsannahmen ohne neue Rueckfrage

Diese Punkte werden nicht nochmals als offene Frage an den Finanzchef gestellt, weil sie fachlich bereits dokumentiert sind:

```text
Local Currency / Hauswaehrung bleibt fuehrend.
Budget-CHF ist nur Zusatz-/Kontrollsicht.
Es werden Budgetkurse verwendet, keine Tages-/SNB-/ECB-Kurse.
Massgebend fuer das Kursjahr ist Finance | Year.
CHF wird mit Faktor 1.0000 behandelt.
Je Finance-Jahr wird der entsprechende Budgetkurs verwendet.
Intercompany bleibt separat auswertbar; die Hauptspalte folgt Finance | Net Sales Actual.
```

## Nur noch offene Fragen an den Finanzchef

### 1. Verbindliche Budgetkurse und Freigabe

Die technische Tabelle enthaelt bereits Budgetkurse fuer 2025 und 2026. Offen ist nur die fachliche Freigabe.

Bitte liefern oder bestaetigen:

```text
Finance Year | From Currency | To Currency | Budget Rate | Quelle | Freigegeben durch | Freigabedatum
```

Minimal benoetigt:

```text
2025: EUR, USD, GBP, INR -> CHF
2026: EUR, USD, GBP, INR -> CHF
```

Optional, falls Finance diese Waehrungen im Reporting braucht:

```text
CNY, CZK, PLN, JPY -> CHF
```

### 2. Pflegeverantwortung fuer Budgetkurse

Wer ist fachlicher Owner und wer pflegt die Kurse im Dashboard?

Bitte klaeren:

```text
Fachlicher Owner
Technische Pflegeperson / Stellvertretung
Zeitpunkt der jaehrlichen Pflege
Freigabeprozess bei Kursaenderung
```

### 3. Spaltenumfang im zentralen Excel

Welche Zusatzspalten sollen tatsaechlich angehaengt werden?

Vorschlag:

```text
Finance | Budget Year
Finance | Budget Rate to CHF
Finance | Net Sales Actual CHF Budget
Finance | Budget Rate Source
Finance | Budget Rate Missing
```

Bitte streichen oder ergaenzen, falls Finance andere Spaltennamen oder weniger Spalten will.

### 4. Verhalten bei fehlendem Budgetkurs

Was soll passieren, wenn fuer eine Waehrung oder ein Finance-Jahr kein Budgetkurs vorhanden ist?

Vorschlag:

```text
CHF-Budgetwert bleibt leer
Finance | Budget Rate Missing = TRUE
Zeile wird in Kontrollsumme "Budgetkurs fehlt" gezaehlt
Keine stille 0 ohne Warnung
```

Bitte bestaetigen oder anderes Verhalten vorgeben.

### 5. Rundung

Wie soll fuer Finance gerundet werden?

Vorschlag:

```text
Zeilenwert intern ungerundet berechnen
Anzeige/Excel auf 2 Dezimalstellen
Kontrollsummen erst nach Summierung runden
```

Bitte bestaetigen, falls Finance stattdessen pro Zeile gerundet summieren will.

### 6. Ort der Anzeige

Wo soll die neue Budget-CHF-Sicht sichtbar sein?

Bitte auswaehlen:

```text
Zentrales Excel Blatt Sales
Zentrales Excel Blatt Finance Details
Zentrales Excel Blatt Finance Summary
Dashboard Finance Summary
Dashboard Laenderansicht
Dashboard Abweichungen
```

Vorschlag:

```text
Excel: Sales, Finance Details und Finance Summary
Dashboard: Finance Summary und Laenderansicht
```

### 7. DE / Alphaplan 2026

Aktueller Stand: DE/Alphaplan wird per Finance-Regel noch auf Finance-Jahr 2025 gezwungen. Damit wuerde auch die Budget-CHF-Spalte fuer diese DE-Zeilen Budget 2025 verwenden.

Offene Finance-Frage:

```text
Ab wann soll DE/Alphaplan fachlich als Finance-Jahr 2026 laufen?
Welche Bedingung muss vorher erfuellt sein?
```

### 8. Kontrollnachweis fuer Review

Welche Kontrollsicht braucht Finance zur Freigabe der neuen Budget-CHF-Zahl?

Vorschlag:

```text
Kontrollsumme je Finance Year / Country / Local Currency
Net Sales Actual Local
Budget Rate to CHF
Net Sales Actual CHF Budget
Budget Rate Missing Rows
Export der verwendeten Budgetkurse
```

## Empfohlene Beschlussformulierung

Wenn Finance zustimmt, waere die fachliche Regel:

```text
Die bestehende Finance Summary bleibt in Local Currency / Hauswaehrung je Land fuehrend.
Zusaetzlich wird eine Budget-CHF-Reporting-Sicht ergaenzt.
Diese verwendet ausschliesslich freigegebene Budgetkurse je Finance-Jahr.
Massgebend fuer die Kurswahl ist Finance | Year.
Die Originalwerte in CentralSalesRecords und die Local-Currency-Finance-Werte werden nicht ueberschrieben.
Fehlende Budgetkurse werden sichtbar markiert und nicht still mit 0 versteckt.
```

## Technische Konsequenz fuer Umsetzung

Nicht die bestehende allgemeine Wechselkursaufloesung verwenden, wenn offene ECB-/Tageskurse in der Kurstabelle stehen. Fuer Budget-CHF braucht es eine gezielte Budgetkurs-Aufloesung:

```text
CurrencyExchangeRates
WHERE IsActive = true
  AND Notes = 'Budget <FinanceYear>'
  AND FromCurrency = FinanceCurrency
  AND ToCurrency = 'CHF'
```

Damit bleibt die Budget-CHF-Spalte stabil und reproduzierbar.
