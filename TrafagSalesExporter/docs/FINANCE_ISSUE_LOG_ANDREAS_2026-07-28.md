# Issue-Log Andreas — Analyse und Status

Stand: 2026-07-28

Andreas hat am 2026-07-28 ein Issue-Log mit sieben Punkten eroeffnet (Nr. 1 und 2 waren im
uebermittelten Auszug leer). Alle sieben sind auf **Produktivdaten** geprueft: `trafag_exporter.db`
vom Server, Stand 2026-07-27 13:16, read-only Kopie, `CentralSalesRecords` mit `84'788` Zeilen.

Kurzfazit: **Kein einziger Punkt ist ein Rechen- oder Logikfehler im Dashboard.** Es sind
Datenherkunfts-, Vollstaendigkeits- und Normalisierungsprobleme. Zwei davon sind bereits
behoben, fuenf brauchen eine Handlung ausserhalb des Dashboard-Codes.

## Ausgefuelltes Log

| Nr | Bereich | Issue | Status | Prio | Owner | Naechster Schritt | Nachweis |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 3 | TR UK Sales | Daten TR UK fuer 2025 fehlen | **ERLEDIGT, Wert abgenommen 2026-08-11** | - | - | Keine Aktion | Zwei Stufen: die Verifikation vom 2026-07-31 belegte nur die ZEILEN (1'867 fuer 2025, 1'090 fuer 2026, alle Supplier-Felder gefuellt, `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`). Die BETRAEGE waren dabei noch falsch (`394'439` statt `3'538'972` GBP = 11 %, Stueckpreis statt Zeilenwert) und fielen erst am 2026-08-10 auf. Nach Korrekturdatei und neuem Export am 2026-08-11: `3'529'861.80` GBP = 99.7 % des Solls, Marge +33.8 % statt −502.7 %, `1'867` Zeilen. Nachweis: `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md`, Abschnitt „Abnahme 2026-08-11". |
| 4 | TR ES Sales | Daten TR ES Jan–Mai 2026 fehlen | **Bestaetigt**, Ursache klar | Mittel | Spanien / Ingo | Range-Export `2026-01-01` bis `2026-05-27` nachziehen | Jan–Apr = 0 Zeilen, Mai nur 35 (ab 28.05.). Ursache: vorhandener Export `Spain_Sales_range_20260528_to_20260603.csv`. `FINANCE_DATENLUECKEN_ANDREAS_2026-07-28.md` §2 |
| 5 | TR AT und CH | Daten ab Mitte April nur noch vereinzelt | **Ursache gefunden, teilweise behoben** | **Hoch** | Ingo | Report `Z_TRAFAG_DACH_EXPORT` auf P76 fuer `s_gjahr = 2025` nachziehen, dann `Sites.SapServiceUrl` auf `travp762` | Dashboard las vom TEST-System T76 (Daten endeten Mitte April). Report war produktiv nie fuer 2026 gelaufen. Nach Ingos Lauf am 28.07.: P76 Gjahr2026 `18'290` statt `0`. `FINANCE_CHAT_2026_LUECKE_ROOTCAUSE_2026-07-28.md` |
| 6 | Sales Database TR ES | Posting Date fehlt bei TR ES | **Bestaetigt, praeziser als vermutet** | Mittel | Spanien / Ingo | Klaeren, ob Sage ein Buchungsdatum liefern kann; die 229 Zeilen ohne jedes Datum separat pruefen | `PostingDate` fehlt bei **5'478 von 5'478** ES-Zeilen (100 %), nicht nur bei einigen. Fallback auf `InvoiceDate` greift, ausser bei **229 Zeilen ohne jedes Datum** — die fallen aus jeder Jahres-/Monatsauswertung heraus. Siehe unten §1 |
| — | Sales Database | Lieferant wird bei sehr vielen Gesellschaften nicht angezeigt | **CH-Fallback deployed; lokaler Nichttreffer-Zweig committed, noch nicht deployed** | **Sehr hoch** | **App / Standorte** | Separaten Deploy bestaetigen lassen; danach 12'023 lokale Zeilen sowie 6'749 Zeilen mit positiver lokaler Kostenbasis nachmessen | Andreas-Beschluss 11.08.: CH-MARC-Treffer = intern/TR_AG; sicherer Nichttreffer = `Lokal` mit Standardkosten der jeweiligen Gesellschaft. Fehlender Schluessel/Cache bleibt unklar; expliziter Supplier gewinnt. `FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11.md` |
| — | Sales Database | Customer Country code is not standardized | **BEHOBEN im Code** (Deploy offen) | Mittel | Ingo | Deploy + ES-Reimport, danach Stichprobe | Spanien lieferte spanische Klartextnamen. Neue Transformation `NormalizeCountryCode`, `294/294` Tests gruen. Siehe unten §2 |
| — | Sales Database | (neu gefunden, nicht von Andreas gemeldet) `CustomerCountry` bei TR DE zu 100 % leer | Offen | Mittel | Ingo | Alphaparn-Exportspalten pruefen | `7'167` von `7'167` TRDE-Zeilen ohne Kundenland. Siehe unten §2 |

## §1 Issue 6 im Detail: Posting Date bei TR ES

| TSC | Zeilen | ohne `PostingDate` | ohne **jedes** Datum |
| --- | --- | --- | --- |
| TRES | 5'478 | **5'478 (100 %)** | **229** |
| TRUK | 1'088 | 6 | 6 |
| alle uebrigen | 76'... | 0 | 0 |

Andreas' Beobachtung ist zutreffend und sogar systematischer als gemeldet: Spanien liefert
**gar kein** Buchungsdatum. Die App hat dafuer eine dokumentierte Fallback-Kette
(`PostingDate` -> `InvoiceDate` -> `ExtractionDate`), weshalb die meisten ES-Zeilen trotzdem
korrekt einem Jahr zugeordnet werden.

Der eigentliche Defekt sind die **229 Zeilen ohne jedes Datum**. Sie erscheinen in Andreas'
Pivot nirgends, weil sie keiner Jahres-/Monatsspalte zugeordnet werden koennen.

**Bewusst nicht „gefixt":** Ein `PostingDate = InvoiceDate` zu setzen waere eine fachliche
Annahme (Buchungs- vs. Fakturadatum sind verschiedene Konzepte) und wuerde die 229 Zeilen
ohnehin nicht retten. Beides gehoert fachlich geklaert, nicht technisch erraten.

### LOESBAR: Sage Spanien HAT ein Buchungsdatum — es ist nur nicht im Export

Geprueft am 2026-07-28 an den Sage-Tabellenauszuegen (`SageSpainExportPackage/v2/`):

| Tabelle | Datumsspalten | Im Export enthalten? |
| --- | --- | --- |
| `CabeceraAlbaranCliente` (Lieferschein-Kopf) | `FechaAlbaran`, `FechaFactura`, `FechaCreacion` | **ja** — das ist die aktuelle Exportquelle |
| `LineasAlbaranCliente` (Lieferschein-Positionen) | `FechaRegistro` | **ja** (als `LineRegistrationDate`) |
| **`FacturasTB` (Rechnungen)** | **`FechaAsiento`**, `FechaOperacion`, `FechaRegistro`, `FechaVencimiento`, `FechaEnvio`, `Fecha347` | **NEIN — nicht gejoint** |

**`FechaAsiento` ist das Buchungsdatum** („asiento" = Buchungssatz im spanischen
Rechnungswesen). Fuellgrad in der Stichprobe: **3'788 von 3'788 Zeilen = 100 %**, 318
verschiedene Datumswerte. Es ist ein eigenstaendiges Datum, kein Duplikat von
`FechaFactura` (233 verschiedene Werte, andere Verteilung).

Ursache der Luecke: Der Spanien-Export liest **Lieferschein**-Tabellen
(`CabeceraAlbaranCliente` + `LineasAlbaranCliente`), nicht die **Rechnungs**-Tabelle
`FacturasTB`. Dort liegt das Buchungsdatum.

**Loesungsweg:** `FacturasTB` im Exportskript joinen und `FechaAsiento` als zusaetzliche
Spalte ausgeben (z. B. `PostingDate`), danach im Spanien-Mapping auf
`SalesRecord.PostingDate` mappen. Das ist eine Aenderung an
`Export-SageSpainSalesCsv.ps1` auf dem spanischen Sage-Server — nicht an der App. Gute
Gelegenheit: Santi exportiert den fehlenden Zeitraum ohnehin gerade manuell.

Offen dabei: ueber welchen Schluessel `FacturasTB` mit den Lieferschein-Positionen
verbunden wird (Rechnungsnummer/-serie/-jahr), und ob Gutschriften denselben Weg nehmen.
Das muss Santi bzw. wer das Skript gebaut hat beantworten.

## §2 Issue „Customer Country not standardized": Befund und Fix

### Befund — es war nicht „unstandardisiert", sondern zwei getrennte Probleme

`CustomerCountry`-Fuellgrad je TSC:

| TSC | Zeilen | leer | Format der vorhandenen Werte |
| --- | --- | --- | --- |
| **TRDE** | 7'167 | **7'167 (100 %)** | — kein Wert vorhanden |
| **TRES** | 5'478 | 0 | **spanische Klartextnamen** statt ISO-Codes |
| TRFR | 2'562 | 53 (2.1 %) | ISO-2 |
| TRIT | 19'530 | 222 (1.1 %) | ISO-2 |
| TRIN | 6'973 | 71 (1.0 %) | ISO-2 |
| TRUS | 1'484 | 13 (0.9 %) | ISO-2 |
| TRCH / TRAT / TRUK / TRES | — | 0 | ISO-2 (ausser ES) |

Alle Nicht-ES-Werte sind saubere 2-Buchstaben-Codes (65 verschiedene, keine
Gross-/Kleinschreibungsprobleme, keine Ziffern). Die gemeldete Inkonsistenz kommt
**ausschliesslich aus Spanien**:

| Wert (TRES) | Zeilen | | Wert | Zeilen |
| --- | --- | --- | --- | --- |
| `ESPAÑA` | 3'815 | | `GUATEMALA` | 84 |
| `BRASIL` | 227 | | `ECUADOR (Inc.GALAPAGOS)` | 78 |
| `PORTUGAL` | 215 | | `PARAGUAY` | 38 |
| `PERÚ` | 202 | | `EL SALVADOR` | 35 |
| `MÉXICO` | 194 | | `ALEMANIA` | 23 |
| `ARGENTINA` | 170 | | `ESTADOS UNIDOS DE AMÉRICA` | 19 |
| `COLOMBIA` | 167 | | `COSTA RICA` | 14 |
| `CHILE` | 157 | | weitere (FRANCIA, REPÚBLICA DOMINICANA, PANAMÁ, INDIA, CHINA, AUSTRIA, BOLIVIA) | je < 10 |

### Fix — umgesetzt, `294/294` Tests gruen

Neue Value-Transformation **`NormalizeCountryCode`**, analog zur bereits vorhandenen
`NormalizeCurrencyCode`:

- `Services/TransformationStrategies.cs` — `NormalizeCountryCodeTransformationStrategy`
  mit den 22 spanischen Namen aus den Produktivdaten plus englischen/deutschen Varianten
  als Robustheitsreserve. Vergleich **ohne Diakritika**, damit `PERÚ` und `PERU` gleich
  behandelt werden.
- `Program.cs` — DI-Registrierung.
- `Services/TransformationCatalog.cs` — Beschreibung fuer die Transformations-UI.
- `Services/DatabaseSeedService.cs` — zwei Default-Regeln fuer `MANUAL_EXCEL` (Spanien
  haengt dort): `CustomerCountry` und `SupplierCountry`. Additiv, per Admin-UI abschaltbar.
- Tests in `TransformationStrategiesTests.cs` und `DatabaseInitializationServiceTests.cs`.

**Bewusste Design-Entscheidung:** Unbekannte Klartextwerte bleiben **unveraendert** stehen,
statt geraten oder geleert zu werden. Ein nicht gemappter Name faellt so auf und kann
ergaenzt werden — ein stillschweigend falscher Code waere schlimmer als ein sichtbar
unnormalisierter Name.

**Wirksam wird der Fix erst nach Deploy und einem ES-Reimport**, weil die Transformation
beim Import greift (bestehende Zeilen in `CentralSalesRecords` werden nicht rueckwirkend
umgeschrieben).

### Nebenbefund: TR DE hat gar kein Kundenland

Nicht von Andreas gemeldet, aber im selben Zug gefunden: **alle 7'167 TRDE-Zeilen** haben
ein leeres `CustomerCountry`. Das ist kein Normalisierungs-, sondern ein Mapping-Problem —
der Alphaplan-Export liefert die Spalte offenbar nicht. Haengt sehr wahrscheinlich mit dem
DE-Supplier-Befund zusammen (TRDE hat auch bei `SupplierName`/`SupplierNumber` 0 von 7'167
Zeilen gefuellt). Beides gemeinsam mit dem Alphaplan-Export klaeren.

## Was NICHT im Dashboard-Code liegt

Fuenf der sieben Punkte sind ausserhalb der App zu loesen:

- **Issue 3/4** — fehlende Exporte aus UK bzw. Spanien.
- **Issue 5** — SAP-Report-Lauf auf P76 (2025 fehlt noch) und danach die Umstellung der
  Quelle auf das Produktivsystem.
- **Issue 6** — fachliche Klaerung, ob Sage ein Buchungsdatum liefern kann.
- **Supplier-Issue** — der technische Stammdaten-Fallback ist umgesetzt; offen bleiben
  echte Quell-/Pflegeluecken sowie die separate CH/AT-Ausnahmefrage fuer Materialien mit
  externem Einkaufsbeleg. Deployment wartet auf den gemeinsamen SAP-ZDISPO-Release.

Der einzige rein technische Punkt war die Laendercode-Normalisierung — der ist erledigt.
