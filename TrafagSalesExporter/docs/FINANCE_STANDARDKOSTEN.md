# Finance: Standardkosten und Kostenbasis der Gruppenmarge

Stand: 2026-08-17. Zusammengefuehrt aus vier Vorgaengerdateien (Umsetzung 2026-07-14,
Arbeitsnotiz 2026-07-17, Sitzung Andreas 2026-07-27, Andreas-Beschluss 2026-08-11).

Betrifft die Kostenbasis der **Gruppenmarge**, nicht den Journal-Import
(dafuer `docs/FINANCE_JOURNAL.md`).

## 1. Geltende Regel, produktiv seit 2026-08-12

Beschluss Andreas vom 2026-08-11, umgesetzt und deployed am 2026-08-12 10:23 MESZ
(Commit `fc5ae75`, `478/478` Tests gruen).

Prioritaet bei der Lieferantenklassifikation:

1. CH/AT-TSC-Regel
2. explizit gepflegter Supplier
3. gepflegter Sales Type `FFM`, `CM` oder `LRD`
4. Materialvergleich gegen `MARC`, Werk `1100`

In Stufe 4 gilt:

| Fall | Ergebnis |
| --- | --- |
| MARC-Treffer | `SupplierType = Intern`, liefernde Gesellschaft `TR_AG` |
| sicherer MARC-Nichttreffer | `SupplierType = Lokal`, Kostenquelle `Standardkosten der lokalen Gesellschaft` |
| lokaler Standardpreis > 0 | Kostenbasis und Marge berechenbar |
| lokaler Standardpreis = 0 | Status `Standardpreis fehlt` |
| Material/TSC fehlt oder MARC-Cache leer | weiterhin `Lieferant unklar` |

`Lokal` heisst bewusst nicht `Extern`: aus dem fehlenden CH-Stammtreffer folgt, dass
lokale Kosten gelten, aber nicht, ob lokal eingekauft oder lokal gefertigt wurde.

Der umschaltbare Alt-Modus `GroupStandardCosts` kennt die Nichttrefferregel nicht und
bildet den historischen Zustand ab. Voraussetzung fuer die Wirkung ist
`ExportSettings.SupplierFallbackMode = ChPlantMaster`.

### Produktive Wirkung, gemessen 2026-08-12

Basis `96'298` Sales-Zeilen und `66'049` MARC-Materialien Werk 1100, nur
Fremdstandortzeilen ohne Supplier-Felder und ohne Sales Type:

| TSC | Kandidaten | CH-intern | Lokal | davon mit Standardkosten |
| --- | ---: | ---: | ---: | ---: |
| TRDE | 7'332 | 175 | 7'157 | 4'905 |
| TRES | 5'712 | 3'282 | 2'430 | 1'372 |
| TRFR | 2'463 | 1'176 | 1'278 | 75 |
| TRIN | 142 | 46 | 68 | 66 |
| TRIT | 5'747 | 4'795 | 945 | 194 |
| TRUS | 1'554 | 1'343 | 145 | 137 |
| **Gesamt** | **22'950** | **10'817** | **12'023** | **6'749** |

## 2. Kostenquelle je Land

| Land | Quelle | Fuellgrad |
| --- | --- | --- |
| CH/AT | `VBRP-WAVWR / FKIMG`, Fallback `MBEW-STPRS` | rund 96 % laut SAP-Messung |
| DE | Alphaplan: `NettoPreisGesamt - RohertragGesamt`, geteilt durch die Menge | 68.5 % |
| IT, IN, US, FR, ES | SAP B1 `INV1.StockPrice` / `RIN1.StockPrice` (Belegposition) | IT 95.7 %, IN 99.4 %, US 92.3 %, ES 80.9 %, FR 51.4 % |
| UK | keine — Sage liefert keine Kostenspalte | 0 % |

FR hat bei rund der Haelfte der B1-Zeilen keinen `StockPrice`; das ist eine
Stammdatenfrage an FR, kein Anbindungsfehler.

### Die zentrale Falle: Stueckpreis gegen Zeilensumme

`ManagementCockpitService.ResolveGroupMarginCostBasis` rechnet **`Menge x StandardCost`**,
`StandardCost` muss also ein **Stueckpreis** sein:

- `MBEW-STPRS` gilt je `PEINH` Stueck und wird durch die Preiseinheit geteilt.
- `VBRP-WAVWR` ist eine Zeilensumme und wird durch die Menge geteilt.
- Der Alphaplan-Rohertrag ist eine Zeilensumme und wird durch die Menge geteilt.

Ohne diese Normalisierung waere die Kostenbasis still um genau diesen Faktor zu hoch.
`PEINH = 1` heute schuetzt nicht: ein einziges Material mit `PEINH = 100` genuegt.

### Warum Material UND Bewertungskreis im Schluessel stehen

MBEW ist je Material **und** Bewertungskreis verschluesselt (CH = 1100, AT = 1200, per
`T001K` bestaetigt). Ein Join nur ueber das Material gaebe CH-Zeilen den
oesterreichischen Preis. Der Umsatz-Service liefert keinen Bewertungskreis, er wird aus
dem Land der Zeile abgeleitet.

### Guardrail

Schlaegt das Lesen der Standardpreise fehl, laeuft der Umsatzimport trotzdem durch
(Warning im Eventlog, `StandardCost` bleibt 0). Ein Kostenproblem darf nie den
taeglichen Umsatzexport eines ganzen Landes verhindern.

### Umsetzungsorte

| Baustein | Ort |
| --- | --- |
| MBEW-Leser | `Services/SapGatewayStandardCostReader.cs` |
| Zuordnung Land zu Bewertungskreis | `Services/StandardCostEnricher.cs` |
| Einhaengepunkt | `Services/DataSources/SapGatewayDataSourceAdapter.cs` |
| Deutschland | `Services/ManualExcelImportService.DeriveAlphaplanUnitCost` |
| Klassifikation | `Services/GroupMarginSupplierClassifier.cs` |
| Berechnung | `Services/GroupMarginCalculator.cs` |
| Tests | `TrafagSalesExporter.Tests/StandardCostTests.cs` |

## 3. Konzern-Standardkosten: genau drei Gesellschaften

Entscheid Andreas 2026-07-27, im Wortlaut „Trafag, das ist ja die drei — weiter wollen
wir nicht gehen":

1. **Trafag AG** — umgesetzt, `GroupStandardCosts`, MBEW-STPRS Bewertungskreis 1100, CHF
2. **Trafag Italien**
3. **Trafag Indien**

Magnetic Sense ist **keine** vierte Quelle. Andreas: „Fuer Magnetic Sense benoetigen wir
aus meiner Sicht keine Daten." Datenbefund deckt sich: `SupplierName LIKE '%MAGNET%'`
ergibt 0 Zeilen, Magnetic Sense kommt ausschliesslich als Kunde vor (101 Zeilen, alle
TRDE) und ist kundenseitig bereits als IC-Marker gesetzt.

Verlinkung:

```
Lieferant = Trafag AG       -> Standardkosten aus TR-AG-Tabelle
Lieferant = Trafag Italien  -> Standardkosten aus TR-IT-Tabelle
Lieferant = Trafag Indien   -> Standardkosten aus TR-IN-Tabelle
sonst                       -> Standardkosten der verkaufenden Landesgesellschaft
```

### Datenqualitaets-Caveat

Konzernvorgabe ist Moving Average, aber laut Andreas halten sich nicht alle Gesellschaften
daran; manche nutzen noch LIFO oder aehnliches. **Bei Abweichungen zwischen den drei
Tabellen zuerst die Bewertungsmethode pruefen, bevor ein Datenfehler vermutet wird.**

## 4. TR IT: warum der B1-Artikelstamm leer ist

Dieser Befund wurde zweimal falsch interpretiert und ist der wichtigste Merksatz des
Themas.

Live gegen `it01_p` (BI1-HANA `travtrp0:30015`, read-only) am 2026-07-27 gemessen:

| Feld | Ergebnis |
| --- | --- |
| `OITM.PrdStdCst` | 0 bei allen 40'478 Artikeln, Feld komplett unbenutzt |
| `OITM.AvgPrice` | > 0 bei nur 248 von 40'478 |
| `OITW.AvgPrice` | 0 bei allen 1'902'456 Lagerzeilen |

Ursache ist die Bewertungsmethode `OITM.EvalSystem`, auf der fachlich richtigen Basis
aktiver Lagerartikel (`InvntItem = 'Y'` und `validFor = 'Y'`):

| EvalSystem | Aktive Lagerartikel | Anteil | davon `AvgPrice` > 0 |
| --- | ---: | ---: | ---: |
| `B` Charge/Serie | 31'600 | 99.1 % | 0 |
| `A` Moving Average | 296 | 0.9 % | 224 (75.7 %) |
| `S` Standardpreis | 6 | 0.02 % | 0 |

Bei Serien-/Chargenbewertung fuehrt B1 die Kosten **je Charge**, nicht im Artikelstamm.
`AvgPrice = 0` ist damit architektonisch erwartungskonform und wird sich nie fuellen.
Eine „TR-IT-Standardkostentabelle aus dem Artikelstamm" kann es also nicht geben — ein
monatlicher Export dieser Felder lieferte dauerhaft Nullen.

**Die Kosten existieren auf Belegebene.** Fuer 2026 verkaufte Materialien: 2'019 von
2'082 (97.0 %) haben `INV1.StockPrice` > 0, gegenueber 0 mit `PrdStdCst`. Andreas hat am
2026-07-27 diesen Belegebenen-Weg freigegeben: „Die aus deiner Sicht einfachste Loesung
wuerde ich im ersten Schritt umsetzen. Eine zusaetzlich kalkulierte Groesse benoetigen
wir vorerst nicht."

**Wichtige Einschraenkung:** Ein hoher `StockPrice`-Fuellgrad loest die Gruppenmarge
nicht automatisch. Kauft TRFR von Trafag Italia, ist TRFRs `StockPrice` der
IC-Verrechnungspreis — genau der Wert, den die Gruppenmarge ersetzen soll. Nur wenn
Trafag Italia dasselbe Material **selbst** verkauft, ist TRITs eigener `StockPrice` die
gesuchte eigene Kostenbasis.

### Lehre fuer die Doku-Praxis

Die urspruengliche Aussage „TR IT pflegt keine Kosten" stand an drei Stellen im Code und
in der Doku, zitierte aber dreimal **denselben** Eintrag ohne Materialnummern oder
Abfrageergebnis. Eine dreifach zitierte Einzelaussage ist keine dreifache Bestaetigung.
Ein Nullwert ohne notierte Bewertungsmethode ist kein Befund, sondern eine offene Frage.

## 5. TR IN

Vom Entwicklungsrechner nicht erreichbar (`20.197.20.60:30015`, Timeout, VPN/Firewall);
der Produktivserver erreicht die Quelle taeglich. Fuer die Umsetzung kein Blocker:
Belegebene ist mit `6'349` von `6'384` Zeilen (99.5 %) sogar besser gefuellt als Italien.
Ein `EvalSystem`-Check waere nur noetig, wenn man TR IN analog zu TR IT auf
Moving-Average-Bewertung ansprechen wollte.

Fuer Abfragen gegen Standortsysteme, die nur der Server erreicht, siehe
`docs/router/plattform.md`, Abschnitt Server-Analyse.

## 6. TR IT Bewertungsmethode: Umstellung auf 2027 verschoben

Paola Castagna (`Paola.Castagna@trafag.com`) hat die Analyse bestaetigt: die Umstellung
von Charge auf Moving Average fuer die rund 31'600 Artikel ist technisch als Massenupdate
machbar. Offen bleibt, ob SAP den Durchschnittspreis danach automatisch fortrechnet oder
ob eine einmalige Bewertungsaktion noetig ist.

Italien hat ueber uebergeordnete Stelle gebeten, die neue Bewertungspolitik **erst ab
2027** zu starten (Kosten des B1-Partners VARONE, Arbeitslast, Verifikation des neuen
Bestandswerts, Margenauswirkung, neue interne Prozesse).

**Das blockiert das Reporting nicht.** Der freigegebene Weg `INV1.StockPrice` arbeitet auf
Belegebene und funktioniert unabhaengig von der Bewertungsmethode. Die
Moving-Average-Umstellung ist ein Bilanzierungs- und Governance-Thema, kein
Reporting-Blocker. Dieser Punkt gehoert in jede Antwort an Italien, sonst entsteht der
Eindruck, das Projekt haenge an Italiens Bewertungsmethode.

Saubere Abgrenzung fuer jede Antwort: die Bewertungsmethode veraendert die
Bestandsbewertung und damit die bilanzielle COGS. Das ist **nicht** identisch mit der
Reporting-Marge im Dashboard.

## 7. Offene Punkte

| Punkt | Bei wem |
| --- | --- |
| `GroupStandardCostAreas.ByEntity` enthaelt nur `TrAg`. `ResolveDeliveringEntity` erkennt TR IT/TR IN am Namen, der `TryGetValue` schlaegt aber fehl und die Zeile faellt **still** auf lokale Kosten zurueck | Code |
| Kostenquelle TR IT/TR IN aus Belegzeilen ableiten, analog `SapGatewayDataSourceAdapter.PersistGroupStandardCostsAsync` (befuellt heute nur TR AG) | Code |
| Welcher Stand je Material gilt: letzter Verkauf, Durchschnitt oder Stichtag | Andreas, nur implizit als Teil des „einfachsten Wegs" mitgemeint |
| Materialien, die TR IT/TR IN nur weiterliefern und nie selbst verkaufen, haben keinen eigenen Kostenwert | Andreas |
| UK ohne Kostenquelle; FR nur zur Haelfte gefuellt | Standorte |
| Fix-/Variabel-Split fuer den Deckungsbeitrag wird von keinem Quellsystem geliefert; `StandardCostVariable`/`StandardCostFixed` und `ContributionMarginCalculator` sind vorbereitet, die DB bleibt bewusst leer | Quellsysteme |
| Angemeldeter Sichtprueflauf der Lokal-Zahl im Cockpit. Der HTTP-`200` auf `/management-cockpit` belegt Erreichbarkeit, nicht die Anzeige hinter dem Finance-Unlock | Ingo |

## 8. Der SAP-Report, der CH/AT befuellt

Die CH/AT-Zeilen entstehen nicht direkt aus einer Tabelle, sondern werden vom ABAP-Report
**`Z_TRAFAG_DACH_EXPORT`** in die Tabelle `ZSCHWEIZ` geschrieben. Der Report laeuft seit
2026-08-12 als taeglicher Batchjob auf P76.

**Namensfalle:** Der Report heisst im System `Z_TRAFAG_DACH_EXPORT`. Die lokale Datei
`docs/abap/Z_TRAFAG_SCHWEIZ_EXPORT.abap` und der `REPORT`-Kopf im Quelltext tragen noch den
alten Namen `Z_TRAFAG_SCHWEIZ_EXPORT` — **dieser Name existiert in keinem der beiden
Systeme.** Beim Suchen nicht darauf verlassen.

Betriebseigenschaften:

- Der Report macht **UPSERT** (`MODIFY zschweiz`), kein `DELETE`. Ein Lauf fuer ein Jahr
  ergaenzt Zeilen und fasst andere Jahre nicht an; im Quelltext gibt es keine
  DELETE-Anweisung auf `ZSCHWEIZ`.
- Er ist **wiederholbar**, derselbe Lauf zweimal erzeugt dasselbe Ergebnis.
- `COMMIT WORK AND WAIT` in Chunks, also kein Riesen-Commit.
- Selektion: Buchungskreise `1100` (CH) und `1200` (AT) plus Geschaeftsjahr.

**Nach einem Deploy neuer Felder muss der Report einmal ueber den vollen historischen
Bestand laufen.** Der UPSERT ergaenzt neue Felder nur bei einem erneuten Lauf ueber
dieselben Zeilen — sonst bleibt zum Beispiel `WAVWR_DC` fuer bereits bestehende Zeilen
leer. Voraussetzung in `ZSCHWEIZ` (SE11): `WAVWR_DC` als CURR mit gleicher Laenge und
Dezimalstellenzahl wie `NETWR_DC`, sowie `STPRS_HC` als CURR.

**Was ausdruecklich NICHT der Fix ist:** `Sites.SapServiceUrl` von Test auf Produktion
umstellen, **solange der Report auf P76 fuer den betroffenen Zeitraum nicht gelaufen ist**.
Das wuerde die vorhandenen Zeilen entfernen statt Daten zu ergaenzen. Eine frueher notierte
Empfehlung in diese Richtung war gefaehrlich und ist zurueckgezogen.

## 9. Erledigte Fragen, damit sie nicht neu gestellt werden

- **`mbewSet` haengt reproduzierbar** (drei Versuche 2026-07-15/16, auch nach
  App-Neustart, ohne Fehlerlog trotz 5-Minuten-Timeout). Arbeitshypothese: `$top=1000`
  wird vom Z-Service nicht serverseitig durchgesetzt und es kommen fast alle rund 68'000
  Materialien in einer Antwort. Deshalb wurde auf den WAVWR-Weg gewechselt.
- **`Sites.SapServiceUrl` fuer ZSCHWEIZ zeigte auf den Test-Server `travt762` statt
  `travp762`.** Reine Konfigurationsaenderung, war auch Ursache fuer „CH/AT sieht 2026
  nicht".
- **Waehrungsmisch-Bug `Marge Original`** — gefixt 2026-07-15 ueber
  `ExportSettings.GroupMarginCostCurrencyMode` (Mask/Convert).
- **Fuellgrad nie mit `Spalte > 0` messen.** `StandardCost` ist eine TEXT-Spalte, in
  SQLite ist Text groesser als jede Zahl, das ergibt falsche 100 %. `CAST(... AS REAL)`
  verwenden. Dieser Fehler hat am 2026-07-16 eine ganze Messreihe verfaelscht.
- **Grundgesamtheit bei Fuellgraden filtern.** Die B1-Aussage „nur 40.6 % der
  Moving-Average-Artikel haben `AvgPrice`" war durch Nicht-Lagerartikel verzerrt; auf
  aktiven Lagerartikeln sind es 75.7 %. Die daraus gezogene Schlussfolgerung war
  entsprechend nicht belegbar.

## Querverweise

- Gruppenmarge-Fachlogik: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`
- SAP-Spezifikation WAVWR: `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`
- Supplier-Klassifikation und Laenderstatus: `docs/FINANCE_SUPPLIER.md`
- ABAP-Analysereport STPRS: `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md`
