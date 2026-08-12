# Supplier-Luecke: Analyse auf Produktivdaten

Stand: 2026-07-28

Anlass: Aktionspunkt aus der Sitzung mit Andreas vom 2026-07-27 — Andreas hatte im Auszug
„ca. 79'000" bzw. „64'000" Zeilen ohne Lieferant gezaehlt (Zahl im Gespraech unklar) und
gesagt, das sei „zu viel", ein Lieferant „muesste ja eigentlich immer da sein". Ursache war
offen. Siehe `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitt 2.

Datenbasis: **Produktive Datenbank** `trafag_exporter.db` vom Server
(`\\trch-webapp-bidashboard.trafagch.local\BiDashboard$\`), Stand **2026-07-27 13:16**,
als read-only Kopie ausgewertet (WAL war 0 Bytes, Kopie damit konsistent). Tabelle
`CentralSalesRecords`, `84'788` Zeilen.

## 1. Andreas' Zahl ist bestaetigt: 69'919 Zeilen

| Kennzahl | Wert |
| --- | --- |
| Zeilen gesamt | 84'788 |
| Zeilen mit ALLEN DREI Supplier-Feldern leer | **69'919** |
| Anteil | **82.5 %** |

Andreas' Groessenordnung („60-79 Tsd.") trifft zu. Es ist kein Zaehl- oder Filterfehler in
seinem Excel-Auszug.

## 2. Kernbefund: Die drei Felder sind immer GEMEINSAM leer

Das ist der eigentlich diagnostische Punkt. Je TSC gilt ausnahmslos:

`Anzahl ohne SupplierNumber` = `Anzahl ohne SupplierName` = `Anzahl ohne SupplierCountry`
= `Anzahl mit allen drei leer`

| TSC | Zeilen | alle 3 leer | Anteil leer | -> mit Lieferant |
| --- | --- | --- | --- | --- |
| TRCH | 39'043 | 39'043 | **100 %** | 0 |
| TRDE | 7'167 | 7'167 | **100 %** | 0 |
| TRES | 5'478 | 5'478 | **100 %** | 0 |
| TRAT | 1'463 | 1'463 | **100 %** | 0 |
| TRUK | 1'088 | 1'088 | **100 %** | 0 |
| TRIN | 6'973 | 6'164 | 88.4 % | 809 |
| TRFR | 2'562 | 2'429 | 94.8 % | 133 |
| TRUS | 1'484 | 1'478 | 99.6 % | 6 |
| TRIT | 19'530 | 5'609 | 28.7 % | **13'921** |

**Es gibt keine einzige Zeile, in der nur ein oder zwei der drei Felder fehlen.** Das ist
kein Datenqualitaetsproblem („Lieferant vergessen zu pflegen"), sondern ein
**Mapping-/Quellenproblem**: Die Lieferanteninformation kommt entweder komplett durch oder
gar nicht. Damit ist der Befund vom 2026-07-17 auf Produktivdaten bestaetigt und
praezisiert:

- **Strukturell 100 % leer (5 Laender):** CH, AT, DE, ES, UK — die Quelle liefert kein
  Lieferantenfeld bzw. es existiert kein Mapping.
- **Teilweise gefuellt (B1-Laender):** IT (71 % gefuellt, klar am besten), IN (12 %),
  FR (5 %), US (0.4 %) — dort kommt der Wert aus `OITM.CardCode` (Standardlieferant im
  Artikelstamm), der oft nicht gepflegt ist.

### Offene Frage zu Deutschland

TRDE hat in der Produktiv-DB **0 von 7'167 Zeilen** mit Lieferantenname oder -nummer. In
einer lokalen Entwickler-Momentaufnahme vom 2026-07-02 waren dagegen 1'764 TRDE-Zeilen mit
`SupplierName = 'Trafag AG'` vorhanden. Ob das ein Rueckschritt (Alphaplan-Export liefert
die Spalten `Lieferanten Nummer`/`Name Lieferant`/`Land Lieferant` nicht mehr) oder nur ein
Unterschied zwischen zwei verschiedenen Datenbestaenden ist, ist **nicht geklaert** — die
lokale Datei ist eine Dev-Kopie, nicht ein frueherer Produktivstand. Zu pruefen: liefert der
aktuelle Alphaplan-Export diese Spalten noch?

## 3. Was die Luecke konkret kostet: 63'008 maskierte Zeilen

Der teure Fall sind Zeilen, die eine **verwertbare Kostenbasis haben**, deren Marge aber
allein wegen der leeren Lieferantenfelder auf `-` steht (Status `Lieferant unklar` erzwingt
offene Kostenbasis, unabhaengig von den Kosten):

| TSC | Zeilen mit Kosten UND Lieferant unklar |
| --- | --- |
| TRCH | **37'680** |
| TRIN | 6'132 |
| TRDE | 4'918 |
| TRIT | 4'844 |
| TRES | 4'440 |
| TRAT | 1'462 |
| TRUS | 1'339 |
| TRFR | 1'193 |
| **Summe** | **63'008** |

Das sind **74 % aller Zeilen**. Die am 2026-07-16 mit viel Aufwand hergestellte
CH/AT-Kostenbasis (`WAVWR`/`STPRS`, Fuellgrad TRCH 96.5 % / TRAT 99.9 %) wirkt sich dadurch
in der Gruppenmarge auf **keiner einzigen Zeile** aus — 37'680 CH-Zeilen und 1'462
AT-Zeilen haben Kosten und zeigen trotzdem keine Marge.

**Damit hat die offene Fachfrage an Andreas vom 2026-07-17 eine Zahl:** Die Regel
„alle drei Supplier-Felder leer -> Lieferant unklar -> keine Marge" maskiert 63'008 von
84'788 Zeilen. Solange sie gilt, ist die Gruppenmarge fuer den Grossteil des Konzerns
strukturell nicht darstellbar — nicht wegen fehlender Kosten, sondern wegen fehlender
Lieferantentexte.

## 4. Nebenbefund: Magnetic Sense in Produktivdaten

- `SupplierName LIKE '%MAGNET%'` -> **0 Zeilen** (bestaetigt: nie Lieferant)
- `CustomerName LIKE '%MAGNET%'` -> **1 Zeile**

In der Dev-Momentaufnahme vom 2026-07-02 waren es 101 Kundenzeilen (alle TRDE). Der
Unterschied haengt sehr wahrscheinlich mit dem DE-Befund aus Abschnitt 2 zusammen
(veraenderte Alphaplan-Spalten/Kundennamen). Fuer Andreas' Entscheid „fuer Magnetic Sense
brauchen wir keine Daten" ist das unerheblich; fuer die DE-Kundenausschlussregel
(`FinanceRuleEngine`, Marker `Magnetic Sense`) heisst es: die Regel greift produktiv
praktisch nicht mehr. Ob das gewollt ist, waere mit Andreas zu klaeren.

## 5. Kostenbasis-Fuellgrad produktiv (Nebenergebnis, Stand 2026-07-27)

| TSC | Zeilen | mit `StandardCost` > 0 | Anteil |
| --- | --- | --- | --- |
| TRAT | 1'463 | 1'462 | 99.9 % |
| TRIN | 6'973 | 6'934 | **99.4 %** |
| TRCH | 39'043 | 37'680 | 96.5 % |
| TRIT | 19'530 | 18'695 | 95.7 % |
| TRUS | 1'484 | 1'344 | 90.6 % |
| TRES | 5'478 | 4'440 | 81.1 % |
| TRDE | 7'167 | 4'918 | 68.6 % |
| TRFR | 2'562 | 1'319 | 51.5 % |
| TRUK | 1'088 | **0** | 0 % |

Bestaetigt auf Produktivdaten: der CH/AT-WAVWR-Fix vom 2026-07-16 ist wirksam (96.5 % /
99.9 %), FR hat weiterhin das bekannte `StockPrice`-Stammdatenproblem (51.5 %), UK hat
unveraendert keine Kostenquelle (jetzt 1'088 Zeilen statt vorher 5).

**Fuer Indien besonders relevant:** 6'934 von 6'973 Zeilen mit Kosten (99.4 %), und
**1'430 von 1'434 Materialien** (99.7 %) haben eine eigene Kostenbasis. Der fuer TR IT
freigegebene Belegebenen-Weg ist damit fuer TR IN auf Produktivdaten belegt tragfaehig —
ohne dass ein Artikelstamm-/Bewertungsmethoden-Check noetig waere.

## 6. Was NICHT geklaert werden konnte

Die eigentliche Ausgangsfrage („wie sieht die Bewertungsmethode in Indiens B1 aus, analog
zum TR-IT-Befund") bleibt **offen**. Indiens HANA (`20.197.20.60:30015`, Schema
`TRAFAG_LIVE`, fachlich SAP B1 trotz Quellsystem-Code `SAGE`) ist vom Entwicklungsrechner
nicht erreichbar; der Produktivserver erreicht sie (TRIN-Export 2026-07-27 erfolgreich,
6'973 Zeilen), aber eine Remote-Ausfuehrung auf dem Server war in der Session nicht
moeglich (Berechtigungsebene hat `Invoke-Command` blockiert). Der Server-Share ist lesbar,
enthaelt aber nur die App-Datenbank, keine B1-Artikelstammdaten.

Wege, das nachzuholen (nur falls TR IN analog zu TR IT auf Moving Average angesprochen
werden soll — fuer die Umsetzung selbst NICHT noetig):
1. `.tmp_tools/HanaQ` auf den Server bringen und dort gegen die Server-DB laufen lassen
   (Verbindungsdaten kommen dann aus der Server-DB, keine Credentials auf der Kommandozeile).
2. Netzwerkfreigabe fuer den Entwicklungsrechner auf `20.197.20.60:30015`.

Abzufragen waeren dieselben Punkte wie bei Italien: `OITM.EvalSystem`-Verteilung (gefiltert
auf `InvntItem='Y' AND validFor='Y'`), Fuellgrad `OITM.AvgPrice`/`PrdStdCst`/`OITW.AvgPrice`,
Gegencheck `ManBtchNum`/`ManSerNum`.

## 7. Zwei weitere Produktivbefunde, die die Priorisierung umdrehen

### 7a. `GroupStandardCosts` ist produktiv WEITERHIN LEER (0 Zeilen)

Die TR-AG-Konzernkostenanbindung wurde am 2026-07-15 gebaut und deployed (Commit `5efeed7`),
war am 2026-07-16 als „produktiv noch nicht wirksam" dokumentiert (Root Cause damals:
`Sites.SapServiceUrl` fuer ZSCHWEIZ zeigt auf `travt762`/Test statt `travp762`/Prod,
zusaetzlich haengender `mbewSet`-Read). **Stand 2026-07-27, also 12 Tage spaeter, hat die
Tabelle `GroupStandardCosts` immer noch `0` Zeilen.** Das bereits gebaute Feature ist damit
unverändert wirkungslos.

Folge: TR-AG-gelieferte Zeilen nutzen weiterhin die lokale Kostenbasis. Bei TRIT-Zeilen ist
das der **IC-Verrechnungspreis** — also genau der Wert, den die Gruppenmarge laut
`Mappe1.xlsx` ersetzen soll. Und weil TRIT Supplier-Felder hat (71 % gefuellt), sind diese
Zeilen **nicht maskiert**: sie zeigen eine Marge, die auf der fachlich falschen Kostenbasis
beruht. Das ist schlechter als ein „-", weil es nicht als offen erkennbar ist.

### 7b. Die geplante TR-IT/TR-IN-Anbindung betrifft nur 443 Zeilen (0.5 %)

Verteilung der Zeilen nach erkannter liefernder Gesellschaft (Produktivdaten 2026-07-27):

| Liefernde Gesellschaft | Zeilen | Anteil | Status |
| --- | --- | --- | --- |
| extern bzw. Lieferant leer | 77'182 | 91.0 % | davon 69'919 mangels Supplier-Feldern maskiert |
| TR AG | 7'163 | 8.4 % | Code gebaut, aber Kostentabelle leer (7a) -> inert |
| **TR IN** | **314** | **0.37 %** | geplante Arbeit |
| **TR IT** | **129** | **0.15 %** | geplante Arbeit |
| Summe | 84'788 | 100 % | |

**Die fuer diese Woche zugesagte TR-IT-/TR-IN-Verlinkung wuerde also 443 von 84'788 Zeilen
betreffen (0.5 %).** Im Vergleich:

| Massnahme | betroffene Zeilen | Anteil |
| --- | --- | --- |
| Supplier-Regel entscheiden (Abschnitt 3) | 63'008 | 74 % |
| `GroupStandardCosts` befuellen (7a) | 7'163 | 8.4 % |
| TR-IT/TR-IN-Anbindung (7b) | 443 | 0.5 % |

## 8. Empfehlung (nach Hebel sortiert)

1. **Supplier-Regel-Entscheid bei Andreas einholen** — jetzt bezifferbar: 63'008 maskierte
   Zeilen, davon 39'142 CH/AT. Kostet nur eine Nachricht, entscheidet aber, ob die
   Gruppenmarge fuer 74 % der Zeilen ueberhaupt darstellbar wird. Groesster Hebel im
   gesamten Thema.
2. **`GroupStandardCosts`-Befuellung reparieren** (7a) — bereits gebautes Feature aktivieren
   statt neues bauen. Naechster Schritt laut Doku: `Sites.SapServiceUrl` fuer ZSCHWEIZ auf
   `travp762` korrigieren, ZSCHWEIZ-Import erneut anstossen, danach `GroupStandardCosts` und
   eine frische `Sales_All` gegenpruefen (Stichprobe: `SupplierName LIKE '%Trafag AG%'` im
   Blatt „Gruppenmarge Details" muss `CostSource = Konzernkosten TR AG (MBEW-STPRS)` zeigen).
   Betrifft 7'163 Zeilen und behebt zusaetzlich das Problem falsch ausgewiesener Margen.
3. **DE-Supplier-Spalten pruefen** (Abschnitt 2) — 7'167 Zeilen komplett ohne Lieferant.
4. **TR-IT-/TR-IN-Anbindung** (7b) — fachlich freigegeben und richtig, aber mit 443 Zeilen
   der kleinste Hebel. Nicht vor 1 und 2 einplanen; gegenueber Andreas ggf. neu terminieren.
5. TR-IN-Bewertungsmethode (Abschnitt 6) — nachrangig, nur fuer eine Paola-analoge Anfrage.
