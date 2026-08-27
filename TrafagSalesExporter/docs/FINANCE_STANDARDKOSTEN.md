# Finance: Standardkosten und Kostenbasis der Gruppenmarge

Stand: 2026-08-27

Zusammengefuehrt aus vier Vorgaengerdateien (Umsetzung 2026-07-14, Arbeitsnotiz
2026-07-17, Sitzung Andreas 2026-07-27, Andreas-Beschluss 2026-08-11); Konzernkosten
TR IT/TR IN produktiv nachgemessen am 2026-08-25, siehe Abschnitt 3.
Die Sitzung mit Andreas vom 2026-08-27 steht in Abschnitt 11; ihre offenen Folgefragen
sind in Abschnitt 7 nachgezogen.

Betrifft die Kostenbasis der **Gruppenmarge**, nicht den Journal-Import
(dafuer `docs/FINANCE_JOURNAL.md`).

## 1. Geltende Regel, produktiv seit 2026-08-12

Beschluss Andreas vom 2026-08-11, umgesetzt und deployed am 2026-08-12 10:23 MESZ
(Commit `fc5ae75`, `478/478` Tests gruen).

Prioritaet bei der Lieferantenklassifikation:

1. CH/AT-TSC-Regel
2. **gepflegter Sales Type `FFM`, `CM` oder `LRD`** (Entscheid Ingo, 2026-08-27)
3. explizit gepflegter Supplier
4. Materialvergleich gegen `MARC`, Werk `1100`

**Stufe 2 und 3 waren bis zum 2026-08-27 vertauscht.** Ingo hat entschieden: Wo die Quelle
einen Sales Type fuehrt, entscheidet dieser — auch dann, wenn die Lieferantenfelder etwas
anderes sagen. Steht kein Sales Type, gilt weiterhin der Lieferantentext. Praktisch betrifft
die Regel nur TRIN, weil bisher nur Indien das Feld fuehrt.

Damit ist die seit dem 2026-08-05 offene Frage beantwortet, welches Feld bei Widerspruch gilt
(`docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 3b). Sie loest zugleich eine
Asymmetrie auf: fuer die Klassifikation gewann bisher der Lieferantentext, fuer die
Kostenbasis der Sales Type — dieselbe Zeile war also gleichzeitig „extern" und
„Konzernvertrieb".

Steht `LRD`, kommt die Ware von Trafag AG, und damit greift die Schweizer Kostenquelle
`MBEW-STPRS` im Bewertungskreis `1100`.

**Produktive Wirkung, gemessen am 2026-08-27** (nur TRIN fuehrt das Feld, Italien hat es bei
allen Zeilen leer):

| Fall | Zeilen | Wirkung |
| --- | ---: | --- |
| Sales Type `FFM`, aber **fremder** Lieferant gepflegt | `41` | wechselt von `Extern` auf `Intern` |
| Sales Type `FFM` mit gepflegtem Trafag-Lieferanten | `37` | liefernde Gesellschaft kann von `TR_AG` auf `TR_IN` wechseln, sofern dort Trafag AG steht; die genaue Aufteilung ist nicht gemessen |
| Sales Type `LRD` mit Lieferant Trafag | `516` | unveraendert, beide Wege ergeben `TR_AG` |
| Sales Type gepflegt, Lieferantenfelder leer | `6'379` | unveraendert, der Sales Type entschied dort schon vorher |

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
2. **Trafag Italien** — im Code umgesetzt am 2026-08-25, B1-Belegkosten `StockPrice`, EUR
3. **Trafag Indien** — im Code umgesetzt am 2026-08-25, B1-Belegkosten `StockPrice`, INR

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

### Umsetzung TR IT/TR IN vom 2026-08-25

Der HANA-Import baut fuer die beiden eigenen Gesellschaften jetzt je einen getrennten
Kostenbereich in `GroupStandardCosts` auf (`TRIT` beziehungsweise `TRIN`). Quelle ist der
bereits gelesene positive Beleg-Stueckpreis `INV1.StockPrice` beziehungsweise
`RIN1.StockPrice`; je Material gilt der juengste beobachtete Wert nach
`PostingDate ?? InvoiceDate ?? ExtractionDate`. Das entspricht dem bestehenden aktuellen
Snapshot-Modell der TR-AG-Kostentabelle und erfindet keinen Artikelstammwert, den B1 bei
Chargenbewertung nicht fuehrt.

TR IN verwendet bevorzugt `GroupMaterialNumber` (Trafag-Sachnummer), wenn gepflegt, sonst
die lokale Materialnummer. TR IT folgt derselben Schluesselregel. Werte mit `0`, leerem
Materialschluessel oder einer von der Gesellschaftswaehrung abweichenden Kostenwaehrung
werden verworfen. Liefert ein Import gar keinen gueltigen Kostenwert, bleibt der bestehende
Kostenbereich erhalten; ein Quellausfall darf den letzten brauchbaren Stand nicht leeren.

Die Gruppenmarge ersetzt danach bei erkanntem Lieferanten Trafag Italia beziehungsweise
Trafag Indien den lokalen IC-Preis durch den passenden Konzernkostenwert. Die Kostenquelle
wird explizit als `Konzernkosten TR IT (B1 StockPrice)` beziehungsweise
`Konzernkosten TR IN (B1 StockPrice)` ausgewiesen.

**Produktiv deployed am 2026-08-25 15:21:** Funktionscommit `b83ee84`; Server-DLL und
lokaler Release-Build sind bitgleich, und alle vier neuen Wirktokens wurden in der
ausgelieferten DLL nachgewiesen. Die Befuellung selbst wurde am selben Tag um 16:15 nach
je einem TR-IT- und TR-IN-Import nachgemessen, siehe den folgenden Abschnitt.

### Produktive Nachmessung vom 2026-08-25 16:15

Die oben offene Befuellung ist erledigt und gemessen. Grundlage sind zwei produktive
Exporte desselben Tages: `Sales_All_2026-08-25.xlsx` von 15:35, also nach dem Deploy und
vor den Importen, und `Sales_All_2026-08-25 (1).xlsx` von 16:15, nach je einem TR-IT- und
TR-IN-Import. Beide Male hat das Blatt `Gruppenmarge Details` 94'751 Datenzeilen. Der
Vergleich lief zeilenweise ueber den Schluessel Jahr, Land, TSC, Rechnung, Position,
Material und Umsatz, nicht ueber die Zeilennummer: **die Zeilenreihenfolge der beiden
Exporte ist nicht identisch**, ein Positionsvergleich meldet 87'476 falsche Treffer.

Kostenquellen vorher und nachher:

| Kostenquelle | 15:35 | 16:15 |
| --- | ---: | ---: |
| Konzernkosten TR AG (MBEW-STPRS) | 65'672 | 65'672 |
| Standardkosten der lokalen Gesellschaft | 11'075 | 11'075 |
| Kosten aus Verkaufszeile | 7'501 | 7'501 |
| Interner Standardpreis | 10'253 | 4'022 |
| **Konzernkosten TR IN (B1 StockPrice)** | 0 | **6'119** |
| **Konzernkosten TR IT (B1 StockPrice)** | 0 | **112** |
| Konzernkosten fehlen | 140 | 140 |
| Lieferant unklar | 110 | 110 |

Genau `6'231` Zeilen haben gewechselt, alle in dieselbe Richtung von
`Interner Standardpreis` auf die beiden neuen Quellen. Keine andere Quelle hat sich
veraendert, keine Zeile hat ihre Kostenbasis verloren, der Anteil mit Kostenbasis bleibt
bei `94.2 %` (`89'298` auf `89'299` von `94'751`). Abgedeckt sind `1'242` Materialien
ueber TR IN und `40` Materialien ueber TR IT.

**Die A2/A3-Faelle rechnen jetzt auf Ist-Kosten statt auf dem Verrechnungspreis.** Nur
Zeilen mit gleicher Waehrung, deshalb direkt vergleichbar:

| verkauft | Konzernkosten | Zeilen | Umsatz EUR | Kosten alt | Kosten neu | Delta |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| TRIT | TR IT | 88 | 145'750 | 113'854 | 84'124 | -26.1 % |
| TRFR | TR IT | 21 | 36'023 | 18'044 | 13'035 | -27.8 % |

**Die groesste Wirkung liegt bei Indiens eigenen Verkaeufen**, `6'090` der `6'119`
TR-IN-Zeilen. Dort ersetzt der materialbezogene juengste positive Belegwert den bisher
zeilenbezogenen Standardpreis:

| Jahr | Zeilen | Umsatz INR | Kosten alt | Kosten neu | Delta |
| --- | ---: | ---: | ---: | ---: | ---: |
| 2025 | 3'348 | 548'375'007 | 222'584'821 | 245'987'594 | +10.5 % |
| 2026 | 2'742 | 473'026'345 | 199'323'195 | 197'822'061 | -0.8 % |
| Summe | 6'090 | 1'021'401'352 | 421'908'016 | 443'809'656 | +5.2 % |

Fuer 2025 sinkt die indische Marge damit um rund `23.4` Mio INR, fuer 2026 bleibt sie
praktisch unveraendert. Der Ausschlag im Vorjahr ist die direkte Folge der Regel
„juengster positiver Wert": ein heutiger Kostenwert wirkt auf alte Zeilen. Damit hat die
noch offene Frage an Andreas erstmals eine Zahl.

**Neuer Nebeneffekt, den der Guard korrekt abfaengt:** `32` Zeilen sind von `OK` auf
`Kostenwaehrung abweichend` gewechselt, weil ein INR- oder EUR-Kostenwert gegen einen
Umsatz in anderer Waehrung steht.

| verkauft | Konzernkosten | Zeilen |
| --- | --- | ---: |
| TRUK | TR IN | 24 |
| TRIT | TR IN | 5 |
| TRUK | TR IT | 3 |

Beispiel: Rechnung `0000041734/1`, Material `52473`, Umsatz `3'203.40` GBP, Kostenbasis
vorher `2'290.50` GBP, jetzt `86'988.57` INR. Diese Zeilen tragen bewusst keine Marge
mehr, statt eine plausibel aussehende falsche auszuweisen. Sie brauchen eine
Umrechnungsentscheidung.

Die `140` Zeilen `Konzernkosten fehlen` sind unveraendert geblieben. Das ist richtig: es
sind TRIN-Zeilen mit Sales Type `LRD`, deren liefernde Gesellschaft Trafag AG ist, die
neuen indischen Kosten duerfen dort nicht greifen.

**Die Lieferantenerkennung war nie der Engpass.** Alle in den Daten vorkommenden
Schreibvarianten treffen die Regexes in `Services/GroupMarginSupplierClassifier.cs`:
`Trafag Italia S.r.l.`, `Trafag Italia S.r.l` ohne Schlusspunkt,
`Trafag Controls India Pvt. Ltd.` und `Trafag Controls India Pvt Limited`.

**Auswertungsfalle fuer kuenftige Messungen:** Das Blatt `Gruppenmarge Summary` enthaelt
in den Spalten E bis M nur `SUMIFS`- und `COUNTIFS`-Formeln ohne gespeicherte Ergebnisse.
Wer die Mappe maschinell liest statt in Excel zu oeffnen, sieht dort leere Zellen und
haelt das faelschlich fuer fehlende Daten. Richtig ist, aus `Gruppenmarge Details` selbst
zu aggregieren.

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
| Gilt der Schweizer `STPRS` als Konzern-Herstellkostenbasis, sobald CH das Material im Werkstamm 1100 fuehrt, unabhaengig von der liefernden Gesellschaft? Hintergrund: Italiens `StockPrice` ist bei Trafag-Sachnummern der Einkaufspreis und liegt im Mittel beim `3.48`-fachen des Schweizer Werts (Abschnitt 10). **Dringlicher seit der Sitzung vom 2026-08-27:** Andreas hat dort alle drei Konzernquellen als Herstellkosten bestaetigt, ohne diese Messung zu kennen (Abschnitt 11, B3) | Andreas |
| **Neu am 2026-08-27:** Bleibt Stufe 4 der Kaskade, also der Abgleich gegen `MARC` Werk 1100, bestehen? Andreas hat entschieden, dass ohne Lieferant die lokalen Standardkosten gelten (Abschnitt 11, B1). Heute setzt ein MARC-Treffer stattdessen `Intern / TR_AG`; betroffen sind `10'817` von `22'950` Kandidatenzeilen. Gehoert zusammen mit der Frage darueber entschieden, weil beide am selben Hebel ziehen | Andreas |
| Umrechnungsregel fuer Konzernkosten in fremder Waehrung. `32` Zeilen (TRUK/TRIT mit TR-IN- oder TR-IT-Kosten) tragen seit dem 2026-08-25 bewusst keine Marge mehr und stehen auf `Kostenwaehrung abweichend`. **Am 2026-08-27 umgesetzt, noch nicht deployed:** Kostenbasis wird mit dem Tageskurs umgerechnet, die Konzernsumme steht in CHF (Abschnitt 11, B5/B6). Offen bleiben Kursquelle, Stichtag und Pflegeprozess der offiziellen Reportingumrechnung, gefuehrt als `ISS-008` | Andreas / Finance |
| Fachlich bestaetigen, ob der juengste positive Belegkostenwert dauerhaft gilt oder ein Durchschnitt/Stichtag noetig ist. Gemessene Wirkung: Indiens Kostenbasis 2025 `+10.5 %`, 2026 `-0.8 %` | Andreas |
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

## 10. Praemisse „produziert wird nur in Trafag CH" — geprueft am 2026-08-27

Ingo hat am 2026-08-27 die Frage gestellt, ob die drei Konzernkostenquellen ueberhaupt
tragen, wenn nur die Schweiz fertigt. Gemessen read-only gegen die Produktivdatenbank mit
`.tmp_tools/CheckProductionOrigin`, Stand der Kostentabellen 2026-08-27 07:44 bis 07:49.

**Die Praemisse trifft fuer Indien nicht zu und fuer Italien weitgehend schon.** Das ist
keine Wertung des Standorts, sondern eine Aussage darueber, was die jeweilige Zahl misst.

### Indien fertigt nachweislich selbst

| Rolle im Artikelstamm (`OITM.U_Tasc_ST`) | Zeilen | Materialien |
| --- | ---: | ---: |
| `FFM` Eigenfertigung | 6'171 | 1'241 |
| `LRD` Bezug von Trafag AG | 776 | 135 |
| `(leer)` | 462 | 135 |
| `CM` Auftragsfertigung fuer Trafag AG | 23 | 2 |

Von den `6'171` `FFM`-Zeilen fuehrt die Schweiz bei genau `3` dasselbe Material. Indien
fertigt also einen eigenen Produktbestand, keine Schweizer Ware. `6'522` der indischen
Zeilen tragen ueberhaupt keinen Lieferanten, was zur Eigenfertigung passt; nur `760`
Zeilen kommen von Trafag AG.

Gegenprobe ueber die Kostenhoehe: `623` Materialien liegen in beiden Kostentabellen. Der
mittlere Faktor INR zu CHF ist `109` bei einem Kurs von rund `95`, und `338` der `623`
sind in Indien **guenstiger** als in der Schweiz. Ein Verrechnungspreis waere systematisch
teurer. Die indische Zahl verhaelt sich wie eine eigene Kostenbasis.

Ingo hat `FFM` am 2026-08-05 selbst bestaetigt („in Indien hergestellt"), siehe
`docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md` Abschnitt 2.

### Italien sieht ganz anders aus

Italien fuehrt **keinen** Sales Type: alle `19'968` Zeilen sind leer. Die Rolle des
Standorts laesst sich aus dem Feld nicht ablesen. Die Lieferantenseite ist dafuer
eindeutig:

| Lieferant in Italiens eigenen Verkaufszeilen | Zeilen |
| --- | ---: |
| Trafag AG | 10'699 |
| Trafag Italia S.r.l. | 1'018 |
| Trafag Controls India Pvt. Ltd. | 342 |
| uebrige, extern oder ohne Lieferant | 7'909 |

Italien kauft also weit ueberwiegend bei Trafag AG und verkauft weiter. Entsprechend
zerfaellt die TR-IT-Kostentabelle in zwei Haelften:

| Nummernkreis | Materialien | davon auch in CH |
| --- | ---: | ---: |
| `M_IT01_*`, italienische Hausnummer | 1'726 | 0 |
| numerische Trafag-Sachnummer | 837 | **837, also alle** |
| sonstiges | 668 | 30 |

### Der eigentliche Befund: bei Trafag-Sachnummern misst Italien den Einkaufspreis

Fuer die `867` Materialien, die beide Tabellen fuehren, liegt der italienische Stueckwert
im Mittel beim **`3.48`-fachen** des Schweizer `MBEW-STPRS`. `416` liegen ueber dem
Doppelten, `296` ueber dem Dreifachen, nur `169` sind vergleichbar und `48` guenstiger.
Der Wechselkurs erklaert davon nichts, EUR zu CHF liegt bei rund `1.05`.

Stichprobe mit identischer Materialbezeichnung in beiden Standorten, Menge je Zeile in
Klammern:

| Material | Bezeichnung | CH `STPRS` | IT `StockPrice` | Umsatz je Stueck |
| --- | --- | ---: | ---: | ---: |
| `17000` | 920 DIFF.-PRESSOSTAT PD | `112.69` CHF | `826.77` EUR (1) | `1'599` EUR |
| `30111` | ND2.5 DIFF. TRANSMITTER | `187.85` CHF | `822.76` EUR (1) | `1'175` EUR |
| `52535` | 8736 3-STAGE DENSITY MONITOR | `165.79` CHF | `1'100.00` EUR (1) | `2'066` EUR |
| `44537` | 8783 3-STAGE HYBRID DENSITY MONITOR | `137.52` CHF | `619.66` EUR (2) | `2'337` EUR |
| `62045` | 8782 2-STAGE HYBRID DENSITY MONITOR | `173.43` CHF | `344.63` EUR (4) | `1'906` EUR |
| `64939` | 8736 3-STAGE DENSITY MONITOR | `150.05` CHF | `340.69` EUR (3) | `1'880` EUR |

Die Bezeichnungen sind zeichengleich, es ist dasselbe Produkt. Die drei Zeilen mit Menge 1
schliessen die bekannte Stueckpreis-gegen-Zeilensumme-Falle aus Abschnitt 2 aus: der Wert
ist ein echter Stueckwert und liegt trotzdem beim Sieben- bis Achtfachen.

Damit ist der italienische `StockPrice` bei Trafag-Sachnummern **der Einkaufspreis von
Trafag AG**, also genau der IC-Verrechnungspreis, den die Gruppenmarge herausrechnen soll.
Die Warnung in Abschnitt 4 („Kauft TRFR von Trafag Italia, ist TRFRs `StockPrice` der
IC-Verrechnungspreis") gilt eine Ebene hoeher auch fuer Italien selbst.

### Warum trotzdem heute nichts falsch gerechnet wird

Die TR-IT-Kosten greifen nur, wenn Trafag Italia als **Lieferant erkannt** ist. Genau dort
fuehrt die Schweiz das Material praktisch nie:

| Zeilen mit Lieferant Trafag Italia | Zeilen | davon fuehrt CH dasselbe Material |
| --- | ---: | ---: |
| TRIT | 1'018 | **0** |
| TRFR | 44 | **0** |
| TRUK | 14 | 1 |

Dasselbe fuer Trafag India: TRIT `342` Zeilen mit `1` Ueberschneidung, TRUK `108` Zeilen
mit `31`. Italiens `10'699` Zeilen mit Lieferant Trafag AG laufen ueber die
TR-AG-Klassifikation und damit korrekt auf den Schweizer `STPRS`.

**Die `837` ueberteuerten Trafag-Sachnummern liegen also in der Tabelle, werden aber nicht
gezogen.** Das Risiko ist angelegt, nicht aktiv. Es wird aktiv, sobald Italien bei einer
Trafag-Sachnummer sich selbst als Lieferant pflegt — dann kaeme statt der Herstellkosten
der Einkaufspreis in die Konzernmarge, mit dem gemessenen Faktor drei bis acht.

### Empfehlung, Entscheid steht bei Andreas

Eine enge, gut begruendbare Regel: **Fuehrt die Schweiz ein Material im Werkstamm 1100,
gilt der Schweizer `STPRS` als Konzern-Herstellkostenbasis, unabhaengig davon, welche
Gesellschaft geliefert hat.** Die lokale B1-Kostenbasis der TR IT/TR IN kaeme nur noch bei
Materialien zum Zug, die die Schweiz nicht fuehrt — also bei genau dem eigenen
Produktbestand, fuer den sie gedacht war.

Wirkung heute nach der Messung oben: praktisch keine Zeile aendert sich, `1` Zeile in TRUK
bei Italien und `32` bei Indien. Die Regel ist damit fast wirkungsneutral einzufuehren und
schliesst den Fehler, bevor er entsteht. Umgesetzt ist sie **nicht**; das ist ein
Fachentscheid.

### Zwei Nebenbefunde

- Italien fuehrt **keinen Sales Type**. Was fuer Indien seit dem 2026-08-05 die
  Klassifikation traegt, fehlt in Italien vollstaendig. Ohne dieses Feld bleibt die
  italienische Rolle je Artikel unbestimmt.
- In Italiens B1 traegt `Trafag AG` bei `10'699` Zeilen das Lieferantenland **`DE`**, bei
  Trafag UK dagegen korrekt `CH`. Ein Stammdatenfehler in Italiens Lieferantenstamm, ohne
  heutige Wirkung auf die Kostenlogik, aber irrefuehrend in jeder Lieferantenauswertung.

## 11. Sitzung mit Andreas vom 2026-08-27

Andreas und Ingo sind die vereinfachte Arbeitsmappe
`docs/Standortkosten_Logik_2026-08-26.xlsx` im Blatt **Flussdiagramm** gemeinsam
durchgegangen. Grundlage dieses Abschnitts ist das Transkript der Aufzeichnung. Es wurde
in dieser Sitzung **kein Anwendungscode geaendert**; Ingo hat ausdruecklich vorgegeben,
zunaechst nichts zu programmieren.

### Beschluesse

| Nr | Beschluss | Begruendung von Andreas |
| --- | --- | --- |
| B1 | **Kein Lieferant im Feld heisst: Standardkosten der verkaufenden Landesgesellschaft.** | Steht kein Lieferant, wird angenommen, dass es ein externer ist. Der Fall soll nicht zu weiteren Ausnahmen fuehren. |
| B2 | **Steht ein Lieferant, gilt dessen Kostenquelle:** Trafag AG, Trafag Italia oder Trafag India. | Andere liefernde Gesellschaften kommen fachlich nicht vor. |
| B3 | Alle drei Quellen sind **im ersten Schritt die Herstellkostenseite**. | So war es bereits vereinbart. Siehe aber die Einschraenkung unter „Abweichungen". |
| B4 | **Der Sales Type gehoert in den oberen Block**, nicht in einen eigenen Sonderfallzweig unten. | Damit faellt der untere Zweig weg und die Regel bleibt in einer Kaskade lesbar. Der Sales Type betrifft nur Indien; Italien hat Andreas selbst als Sales-Type-Fall verworfen. |
| B5 | **Die Konzernmarge gibt es nur in CHF.** Lokale Sichten bleiben in Lokalwaehrung, eine Anzeige in weiteren Waehrungen waere spaeter reine Umrechnung. | In unterschiedlichen Waehrungen ist eine Konzernmarge fachlich sinnlos. |
| B6 | **Kein Rueckrechnen auf historische Kurse.** Andreas haelt die Umrechnung fuer unkritisch, weil keine volatile Waehrung im Bestand ist; er nennt USD, EUR, GBP und INR. Interessanter findet er, mit aktuellen Kursen zu simulieren, um die Richtung der Margen zu sehen. | Zeitpunktgenaue Kurse machen die Rechnung kompliziert, ohne den Erkenntniswert zu erhoehen. |

Ausdruecklich vertagt hat Andreas die Blaetter **Sonderfaelle** und **Standorte**. Zuerst
sollen die Grundregeln durchgezogen werden.

### Vereinbartes Vorgehen

1. Ingo passt die Arbeitsmappe an den Sitzungsstand an.
2. Danach wird `Sales_All` neu generiert.
3. Andreas prueft die Felder gegen ein Vergleichs-Excel und meldet zurueck.

Korrekturen an der Logik sollen den Weg ueber die Mappe nehmen, nicht ueber Zuruf: Wenn
etwas nicht passt, wird es zuerst im Excel korrigiert und erst dann im Code nachgezogen.

### Abweichungen gegen den heutigen Stand

Diese drei Punkte gehoeren zu Andreas zurueck, bevor er das Vergleichs-Excel prueft.
Sonst prueft er gegen eine Annahme statt gegen die Daten.

**1. B3 trifft fuer Italien nachweislich nicht zu.** Abschnitt 10 dieser Datei, gemessen
am Vormittag desselben Tages, zeigt: Italiens `StockPrice` ist bei Trafag-Sachnummern der
Einkaufspreis von Trafag AG und liegt im Mittel beim `3.48`-fachen des Schweizer
`MBEW-STPRS`. Das ist ein Verrechnungspreis, keine Herstellkosten. Andreas hat B3 mit
hoher Wahrscheinlichkeit ohne Kenntnis dieser Messung bestaetigt.

**2. B1 ist einfacher als die produktive Regel.** Nach Abschnitt 1 laeuft heute eine
vierstufige Kaskade. Stufe 4 vergleicht das Material gegen `MARC` Werk `1100` und setzt
bei einem Treffer `SupplierType = Intern` mit liefernder Gesellschaft `TR_AG`, also
Schweizer Kosten und gerade nicht lokale. Gemessen am 2026-08-12 betrifft das `10'817`
von `22'950` Kandidatenzeilen. Wird B1 woertlich umgesetzt, wechseln diese Zeilen auf
lokale Kosten.

Beide Punkte ziehen am selben Hebel in **entgegengesetzte Richtungen**: Der Italien-Befund
spricht dafuer, den Schweizer `STPRS` auszuweiten, auch wenn ein Lieferant genannt ist.
B1 wuerde ihn einschraenken. Andreas sollte das in einem Zug entscheiden, nicht getrennt.

**3. B5/B6 stehen gegen den Waehrungs-Guard vom 2026-08-25.** Produktiv tragen seit dem
2026-08-25 bewusst `32` Zeilen keine Marge und stehen auf `Kostenwaehrung abweichend`,
weil ein INR- oder EUR-Kostenwert gegen einen Umsatz in anderer Waehrung steht. Die
Sitzung gibt die Richtung vor, naemlich umrechnen statt sperren. Die Regel selbst fehlt
weiter: Kursquelle, Stichtag und Pflegeprozess sind offen und werden in `ISS-008` als
sechs Entscheide bei Finance gefuehrt.

### Umsetzung des Waehrungsteils am 2026-08-27

B5 und B6 sind umgesetzt, `630/630` Tests gruen. **Noch nicht deployed**, der Produktivstand
rechnet bis zur Freigabe unveraendert weiter.

**1. Umrechnen ist die Regel, nicht mehr die Ausnahme.** Der Schalter
`ExportSettings.GroupMarginCostCurrencyMode` existierte seit dem 2026-07-15 und stand auf
`Mask`, ausdruecklich „bis der Fachentscheid vorliegt". Der Entscheid liegt jetzt vor, also
ist `Convert` der Default. `Mask` bleibt waehlbar, ist aber die bewusst gesetzte Ausnahme;
`NormalizeMode` faellt entsprechend auf `Convert` zurueck statt auf `Mask`.

Bestehende Datenbanken tragen den alten Wert `Mask` ausdruecklich in der Zeile, ein
geaenderter Spalten-Default wirkt dort nicht. `DatabaseSchemaMaintenanceService` zieht den
Entscheid deshalb **einmal je Datenbank** nach, gesteuert ueber die Markerspalte
`GroupMarginCostCurrencyDecision20260827Applied`. Wer den Schalter danach wieder auf `Mask`
stellt, behaelt diese Wahl.

**2. Tageskurs statt Jahreskurs.** `GroupMarginCostCurrencyConverter` fragte den Kurs bisher
zum 31.12. des Finance-Jahres der Zeile ab, rechnete also auf historische Kurse zurueck.
Jetzt gilt der laufende Tag. Der Parameter `year` ist aus der Signatur entfallen, statt still
ignoriert zu werden; `ContributionMarginCalculator` folgt derselben Regel.

**Bewusste Folge:** dieselbe Zeile kann an zwei Tagen zwei Margen ergeben, wenn sich der Kurs
dazwischen aendert. Zwei Nachweis-Excel aus verschiedenen Wochen sind daher nicht mehr
zeichengleich. Genau das ist gewollt, weil mit aktuellen Kursen gerechnet und mit anderen
Kursen simuliert werden soll.

**3. Die Konzernmarge wird in CHF ausgewiesen.** Hier lag ein echter Fehler, der ohne B5 nicht
aufgefallen waere: `BuildGroupMarginSummary` addierte `SalesValue` und `CostBasisValue` ueber
alle Laender, obwohl beide in der jeweiligen Verkaufswaehrung stehen. CHF, EUR, GBP und INR
landeten unkonvertiert in derselben Summe; die Kachel trug dann nur das Label `Mixed`. Das war
keine Konzernzahl, sondern eine Addition ungleicher Groessen.

Jede Detailzeile traegt jetzt zusaetzlich `SalesValueChf`, `CostBasisValueChf` und
`MarginValueChf`, umgerechnet mit demselben Tageskurs. Die Konzernsumme rechnet ueber diese
Felder und zeigt `CHF`. Fehlt fuer eine Waehrung ein Kurs, bleibt die Zeile **aus der Summe
heraus** und wird in `MissingGroupCurrencyRateRows` gezaehlt und als Hinweis ausgewiesen; eine
gefuellte Zahl ohne Vorbehalt waere gefaehrlicher als eine sichtbar offene Position.

Landes- und Divisionszeilen bleiben in Lokalwaehrung. Das ist Andreas' „lokale Sicht in
Lokalwaehrung" und zugleich unveraendert gegenueber heute.

**Nicht angefasst:** die offizielle Umrechnung des Umsatzes nach CHF (Group-Currency-Ansicht,
Pruefbuch, `ResolveChfRate`) rechnet weiter mit dem Jahreskurs. Andreas' Aussage bezog sich im
Gespraech auf die Kostenbasis der Gruppenmarge. Kursquelle, Stichtag und Pflegeprozess der
offiziellen Reportingumrechnung haengen an `ISS-008` und sind nicht entschieden.

**Ebenfalls nicht angefasst:** der Deckungsbeitrag in der Konzernsumme summiert weiter ueber
Lokalwaehrungen. Das faellt heute nicht auf, weil kein Quellsystem den fix/variabel-Split
liefert und der Wert produktiv leer bleibt. Sobald eine Quelle ihn liefert, gilt hier
dieselbe CHF-Regel wie fuer die Marge.

### Nebenbefund zur Reihenfolge im Flussdiagramm

B4 beschreibt keine Codeaenderung, sondern eine Korrektur der Darstellung. Der Sales Type
ist im Code bereits **Stufe 3** der Kaskade und steht damit dort, wo Andreas ihn haben
will. Falsch ist nur die Anordnung in der Mappe.

Umgekehrt fehlt der Sales Type im Gesamtexport, gefuehrt als `ISS-013`. Ohne dieses Feld
kann Andreas die indische Steuerung im Vergleichs-Excel nicht nachvollziehen. `ISS-013`
gehoert deshalb **vor** die Vergleichsrunde, nicht danach.

**Am 2026-08-27 umgesetzt, noch nicht deployed.** `Sales Type` und `Trafag Sachnummer`
stehen jetzt additiv am Ende des zentralen `Sales_All` (Spalten `53` und `54`) und im
Nachweisblatt `Gruppenmarge Details` (Spalten `27` und `28`), beide im Hilfeblatt fachlich
beschrieben. Bewusst der Rohwert aus dem Artikelstamm und keine Deutung: leer heisst nicht
„extern", sondern nur, dass die Quelle das Feld nicht fuehrt.

## Querverweise

- Gruppenmarge-Fachlogik: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`
- SAP-Spezifikation WAVWR: `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`
- Supplier-Klassifikation und Laenderstatus: `docs/FINANCE_SUPPLIER.md`
- ABAP-Analysereport STPRS: `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md`
- Vereinfachte Arbeitsmappe mit Flussdiagramm, Grundregel zuerst:
  `docs/Standortkosten_Logik_2026-08-26.xlsx`
- Ausfuehrliche technische Arbeitsmappe je Standort:
  `docs/Standardkosten_Standorte_2026-08-26.xlsx`
