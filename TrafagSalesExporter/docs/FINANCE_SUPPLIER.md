# Finance: Supplier-Klassifikation, Laenderstatus und CH-Werkstamm-Fallback

Stand: 2026-08-31. Zusammengefuehrt aus vier Vorgaengerdateien (Lueckenanalyse
2026-07-28, Laenderstatus und Handoff 2026-08-11, Fallback-Umschalter 2026-08-11).

Issue ISS-003. Fuer den Status je Punkt gilt
`docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, nicht dieses Dokument.

## 1. Die Regel, produktiv seit 2026-08-11 und 2026-08-12

Hat ein Fremdstandort alle drei Supplier-Felder leer und entscheidet auch kein Sales Type
(`FFM`, `CM`, `LRD`), wird die normalisierte Trafag-Materialnummer gegen den Artikelstamm
der Trafag AG Schweiz geprueft:

| Fall | Ergebnis |
| --- | --- |
| Treffer in `MARC`, Werk `1100` | `Intern`, liefernde Gesellschaft `TR_AG` |
| sicherer Nichttreffer bei geladenem Cache | `Lokal`, Standardkosten der lokalen Gesellschaft |
| explizit gepflegter Supplier | hat Vorrang vor diesem Fallback; davor greifen die CH/AT-Regel und seit 2026-08-27 der Sales Type (Reihenfolge: CH/AT-Regel, Sales Type, expliziter Supplier, Fallback) |
| Materialnummer fehlt oder Cache leer | `Unklar` |

CH/AT selbst bleiben unberuehrt, dort gilt die vorhandene TSC-Regel.

**Warum `MARC` Werk 1100 und nicht die mandantenweite `MARA`:** MARC belegt, dass das
Material im CH-Werkstamm gefuehrt wird. Der Treffer ist ein Konzern-Stammdaten-Fallback,
aber ausdruecklich kein Produktions- oder Warenbewegungsnachweis.

**Warum die Tabelle `GroupMaterialMasters` von `GroupStandardCosts` getrennt ist:** Ein
MARC-Treffer darf intern klassifizieren, aber keine erfundene Kostenbasis erzeugen. Echte
Konzernkosten kommen ausschliesslich aus der gemeinsamen Tabelle `GroupStandardCosts`. Sie
wird aus drei Quellen gefuellt: TR AG aus `MBEW-STPRS`, TR IT und TR IN seit 2026-08-25 aus
den B1-Belegkosten (`StockPrice`). (Formulierung am 2026-09-29 praezisiert; vorher stand hier
"ausschliesslich aus MBEW".)

### Umschalter

`Admin Bereich > Settings > Export Einstellungen`, Feld
`Supplier-Fallback ohne Lieferantenangabe`:

- `Neu: CH-Werkstamm (MARC 1100)` — Default, produktiv
- `Alt: CH-Kostentabelle (MBEW 1100)` — historisches Verhalten

Zusatzoption: `Lokale Standardkosten bei fehlendem Lieferanten` bedeutet bewusst: kein
Lieferant -> Kosten der verkaufenden Gesellschaft, auch bei MARC-Treffer.

Gespeichert in `ExportSettings.SupplierFallbackMode`, im Konfigurationsexport mitgefuehrt.
Dashboard, Finance-Pruefbuch, zentrale Excel und Nachweis-Excel nutzen denselben Modus.
Ist nach einer Migration noch kein MARC-Cache vorhanden, faellt der neue Modus
voruebergehend automatisch auf den alten MBEW-Fallback zurueck.

Davon getrennt steht unter Settings die **Kostenquelle bei internem Lieferanten**:
`Kosten der liefernden Gesellschaft` (Default) oder `Schweizer STPRS bei MARC Werk 1100`.
Dieser zweite Schalter aendert nur die Kostenquelle, nie die Lieferantenklassifikation.
Sales Type und explizit gepflegte Lieferanten behalten in allen Modi Vorrang.

`SapGatewayPlantMaterialReader` liest beim CH/AT-SAP-Export genau einmal `MARCSet` mit
`Matnr,Werks`, filtert Werk 1100 clientseitig und ersetzt den Cache atomar. Bei Fehler
oder leerer Antwort bleibt der bisherige Cache erhalten.

### Gemessener Unterschied Alt gegen Neu

| Kennzahl | Alt: MBEW 1100 | Neu: MARC 1100 | Differenz |
| --- | ---: | ---: | ---: |
| CH-Materialien | 63'550 | 66'047 | +2'497 |
| interne Treffer in 22'840 Fallback-Zeilen | 10'097 | 10'817 | +720 |
| betroffene Verkaufsmaterialien | — | 392 | +392 |
| entfallende bisherige Treffer | — | — | **0** |

Die 720 zusaetzlichen Zeilen sind 3,2 % der Fallback-Kandidaten und 0,7 % aller
Sales-Zeilen. Davon 674 auf TRIT, 28 TRFR, 10 TRUS, 8 TRDE.

## 2. Warum die Supplier-Felder fehlen — der diagnostische Kernbefund

Gemessen auf Produktivdaten. Je TSC gilt **ausnahmslos**:

`ohne SupplierNumber` = `ohne SupplierName` = `ohne SupplierCountry` = `alle drei leer`

**Es gibt keine einzige Zeile, in der nur ein oder zwei der drei Felder fehlen.** Das ist
kein Datenqualitaetsproblem im Sinne von „Lieferant vergessen zu pflegen", sondern ein
**Mapping- und Quellenproblem**: die Lieferanteninformation kommt entweder komplett durch
oder gar nicht.

- **Strukturell 100 % leer:** CH, AT, DE, ES — die Quelle liefert kein Lieferantenfeld
  beziehungsweise es existiert kein Mapping. Bei CH/AT ist das dauerhaft so: die Quelle
  ist die Verkaufsfaktura (`FinanzdataSchweizOeSet`, VBRK/VBRP), und eine
  Verkaufsrechnung kennt keinen Vorlieferanten. Bei ES fehlt das Supplier-Mapping im
  Import ganz, bei DE haengt es an den Exportspalten von Alphaplan.
- **Teilweise gefuellt, B1-Laender:** IT am besten, dann IN, FR, US — dort kommt der Wert
  aus `OITM.CardCode`, dem Standardlieferanten im Artikelstamm, der oft ungepflegt ist.
  Nur in dieser Gruppe ist „pflegen" ueberhaupt eine sinnvolle Bitte, und zwar je Artikel
  statt je Belegzeile.
- **UK: geloest, kein Sonderfall mehr.** Fruehere Fassungen dieser Datei fuehrten UK in
  der ersten Gruppe. Das ist falsch. Die Lieferanten- und Kostenspalten sind in der
  UK-Datei **seit jeher vorhanden und gefuellt** (gemessen 2026-07-29 an
  `Sales_TRUK_2026-05-11.xlsx`: Lieferantenfelder `1881/1881`), sie waren nur nie
  gemappt. Seit dem Nachzug in `DatabaseSeedService.EnsureUkManualExcelMapping` steht
  TRUK bei `3'064` von `3'064` Zeilen, also 100 % (Abschnitt 3).

Diese Unterscheidung ist der Grund, warum eine pauschale Bitte um Feldpflege an die
Standorte falsch waere: bei vier Laendern gibt es schlicht nichts zu pflegen.

**UK ist zugleich der Beweisfall fuer Vorrangregel 3 im `router.md`** — erst die eigene
Export- und Mappingseite pruefen, dann den Standort fragen. Solange die Spalten ungemappt
waren, kamen alle UK-Zeilen ohne Lieferant und ohne Kostenbasis in die zentrale Tabelle,
und die UK-Gruppenmarge war damit ueberhaupt nicht berechenbar. Genau dieselbe Pruefung
steht fuer Deutschland noch aus, siehe die offene Frage zu Deutschland in Abschnitt 3.

## 3. Laenderstatus

### Live gemessen am 2026-09-02 gegen `Sales_All_2026-09-01.xlsx`

Das ist der Export, den Andreas ansieht, und damit nach Vorrangregel 1 der gueltige Stand.
Werkzeug `.tmp_tools/CheckSupplierClaims0902`, read-only, `105'282` Verkaufszeilen.

| Land | TSC | Zeilen | alle 3 Felder | Quote | nur 1 oder 2 |
| --- | --- | ---: | ---: | ---: | ---: |
| Oesterreich | TRAT | 1'888 | 0 | 0,0 % | 0 |
| Schweiz | TRCH | 49'747 | 0 | 0,0 % | 0 |
| Deutschland | TRDE | 7'612 | 0 | 0,0 % | 0 |
| Spanien | TRES | 7'071 | 0 | 0,0 % | 0 |
| Frankreich | TRFR | 2'653 | 135 | 5,1 % | 0 |
| Indien | TRIN | 7'476 | 910 | 12,2 % | 0 |
| Italien | TRIT | 24'095 | 24'078 | **99,9 %** | 0 |
| UK | TRUK | 3'119 | 3'119 | **100,0 %** | 0 |
| USA | TRUS | 1'621 | 6 | 0,4 % | 0 |
| **Gesamt** | | **105'282** | **28'248** | **26,8 %** | **0** |

**Der Kernbefund aus Abschnitt 2 ist damit auf der vollen Grundgesamtheit bestaetigt:**
`0` von `105'282` Zeilen haben nur eines oder zwei der drei Felder.

**Zwei Aenderungen gegenueber der historischen Tabelle unten.** Italien ist von 71,2 % auf
99,9 % gesprungen, weil Paola Castagna `OITM.CardCode` doch gepflegt hat (Abschnitt 8);
es bleiben nur noch `17` leere Zeilen. Dadurch steigt die Gesamtquote von 18,7 % auf
26,8 %. Der Auftrag „verbleibende TRIT-Zeilen nach Ursache segmentieren" ist damit
erledigt bis auf diese 17 Zeilen.

Ebenfalls live gemessen und fuer die Klassifikation wichtig: **Sales Type fuehrt
ausschliesslich Indien** (`7'012` von `7'476` Zeilen: `FFM` 6'213, `LRD` 776, `CM` 23).
Alle acht anderen Standorte stehen auf `0`, Italien bei allen `24'095` Zeilen.

**Nicht in `Sales_All` enthalten:** eine Spalte `Cost Source` und der Margen-Status. Wer
die Kostenlogik pruefen will, braucht das Nachweis-Excel, Blatt `Gruppenmarge Details`.
In `Sales_All` sind seit dem 2026-08-27 nur `Sales Type` und `Trafag Sachnummer`
(Spalten 53 und 54) enthalten, damit lassen sich die Stufen 1 bis 3 nachvollziehen.

### Historischer Stand vom 2026-08-11

Basis `neu.xlsx` vom 2026-08-11 mit 96'233 Sales-Zeilen. Damalige Gesamtquote:
`18'263` von `97'537` Zeilen (18,7 %) mit allen drei Feldern. Die folgende Tabelle ist der
Stand jenes Tages und wird als Historie stehen gelassen; gueltig ist die Messung oben.

| Land | TSC | Zeilen | alle 3 Felder | Quote | Naechster Schritt |
| --- | --- | ---: | ---: | ---: | --- |
| Schweiz | TRCH | 47'142 | 0 | 0,0 % | Herstellerregel beibehalten, nur SAP-markierte Fremdbezugs-Ausnahmen mit Andreas klaeren |
| Oesterreich | TRAT | 1'790 | 0 | 0,0 % | gemeinsam mit CH entscheiden, keine Feldpflege |
| Deutschland | TRDE | 7'332 | 0 | 0,0 % | Quellfeld und Mapping festlegen, neu laden |
| Spanien | TRES | 5'697 | 0 | 0,0 % | Quellfeld bestaetigen, Mapping ergaenzen |
| Frankreich | TRFR | 2'598 | 135 | 5,2 % | ungefuellte Zeilen nach Quelle segmentieren |
| Indien | TRIN | 7'116 | 828 | 11,6 % | **keine Massenpflege**, Sales Type deckt 94,0 % ab |
| Italien | TRIT | 19'955 | 14'208 | 71,2 % | verbleibende Zeilen nach Ursache segmentieren |
| UK | TRUK | 3'064 | 3'064 | 100,0 % | nur Regression ueberwachen |
| USA | TRUS | 1'539 | 6 | 0,4 % | Quellfeld pruefen, Ersatzklassifikation suchen |

Umsatzwerte werden bewusst nicht ueber Laender addiert, weil sie in lokalen Waehrungen
vorliegen.

**Indien ausdruecklich nicht um Massenpflege bitten.** Der aktuelle `Sales Type`
klassifiziert 6'686 von 7'116 Zeilen (94,0 %) funktional. Nur die 430 Zeilen ohne Sales
Type und bekannte Widersprueche gezielt klaeren. Details:
`docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`.

### Offene Frage zu Deutschland

TRDE hat produktiv 0 Zeilen mit Lieferantenname oder -nummer. In einer lokalen
Entwickler-Momentaufnahme vom 2026-07-02 waren 1'764 TRDE-Zeilen mit
`SupplierName = 'Trafag AG'` vorhanden. Ob das ein Rueckschritt ist (Alphaplan-Export
liefert die Spalten `Lieferanten Nummer`/`Name Lieferant`/`Land Lieferant` nicht mehr)
oder nur ein Unterschied zweier Datenbestaende, war lange ungeklaert.

**Geprueft am 2026-09-29 (`ISS-003.3`):** Unsere Export-Query liest keine Lieferantenspalte.
`alphaplanExport.ps1` und `alphaplandeltaexport.ps1` lesen nur `dbo.Belege` und `dbo.BelegePositionen`;
`ArtikelID` wird ausgewaehlt, aber nirgends verknuepft. Auch der Import
(`ManualExcelImportService.ReadAlphaplanInvoicePair`) setzt keines der drei Lieferantenfelder. Alphaplan
fuehrt die Lieferanten aber: das alte Excel-Beispiel `DE_Beispiel_Export_Daten.xlsx` hat `Lieferanten
Nummer`/`Name Lieferant`/`Land Lieferant`, die 1'764 Zeilen stammen vermutlich aus diesem alten Weg. Es
ist also ein Rueckschritt durch den Wechsel auf die Beleg-CSVs. Tabelle und Spalte sind wegen des
fehlenden Schemas offen (`docs/STANDORT_DE_ALPHAPLAN.md` Abschnitt 6).

## 4. Was die Luecke gekostet hat

Historischer Befund vom 2026-07-27, der die Prioritaet begruendet hat: `63'008` von
`84'788` Zeilen (74 %) hatten eine verwertbare Kostenbasis, zeigten aber wegen des Status
`Lieferant unklar` keine Marge. Die mit viel Aufwand hergestellte CH/AT-Kostenbasis wirkte
sich dadurch auf **keiner einzigen Zeile** aus.

Genau dieses Problem loesen der CH-Werkstamm-Fallback und Andreas' lokale Standardkosten:
von 22'950 Kandidaten werden 10'817 `Intern` und 12'023 `Lokal`, nur 110 bleiben `Unklar`.

**Ein Hinweis, der weiter gilt:** Zeilen mit gefuellten Supplier-Feldern sind *nicht*
maskiert und zeigen eine Marge — auch dann, wenn die Kostenbasis fachlich der
IC-Verrechnungspreis statt der Konzernkosten ist. Das ist schlechter als ein sichtbares
Minuszeichen, weil es nicht als offen erkennbar ist.

### Warum genau, am Code nachgelesen am 2026-09-02

Der Schutz dagegen existiert, ist aber **enger als oft angenommen**. Die Regel
`GroupMarginCostRules.GroupDistributionWithoutGroupCost` in
`Services/GroupMarginCalculator.cs` prueft ausschliesslich, ob der **Sales Type `LRD`** ist:

```csharp
ResolveSalesTypeRole(line.SalesType) == SalesTypeRoles.GroupDistribution
    ? new GroupMarginCostBasis(0m, line.StandardCostCurrency, IsGroupCostMissing: true)
    : null
```

Sie prueft **nicht**, ob eine liefernde Konzerngesellschaft erkannt wurde. Eine Zeile mit
erkanntem Lieferanten `Trafag Italia` oder `Trafag AG`, fuer deren Material kein
Konzernkostensatz existiert, faellt deshalb still auf die letzte Regel `LocalStandardCost`
durch. Sie zeigt eine Marge auf dem lokalen IC-Preis, traegt die Kostenquelle
`Interner Standardpreis` und ist an **keiner** Stelle als offen erkennbar.

Da nur Indien einen Sales Type pflegt, existiert dieser Schutz praktisch **nur fuer
Indien**. Fuer Italien und alle uebrigen Standorte gibt es ihn nicht.

**Obergrenze, live gemessen am 2026-09-02** gegen `Sales_All_2026-09-01.xlsx` mit
`.tmp_tools/CheckSupplierClaims0902`: `19'221` Zeilen tragen eine erkannte liefernde
Konzerngesellschaft **ohne** Sales Type `LRD`, also 18,3 % aller `105'282`
Verkaufszeilen.

| Verkaufender Standort | erkannte liefernde Gesellschaft | Zeilen |
| --- | --- | ---: |
| TRIT | TR_AG | 14'536 |
| TRUK | TR_AG | 2'839 |
| TRIT | TR_IT | 1'028 |
| TRIT | TR_IN | 366 |
| TRIN | TR_AG | 244 |
| TRUK | TR_IN | 108 |
| TRFR | TR_IT | 44 |
| TRFR | TR_AG | 40 |
| TRUK | TR_IT | 14 |
| TRUS | TR_AG | 2 |
| **Gesamt** | | **19'221** |

**Das ist ausdruecklich eine Obergrenze und nicht die betroffene Menge.** Wie viele dieser
Zeilen wirklich keinen Konzernkostensatz finden, laesst sich aus `Sales_All` nicht messen,
weil die Datei keine Kostenquelle fuehrt. Dafuer braucht es eine Messung gegen
`GroupStandardCosts` in der Produktivdatenbank, nach dem Muster von
`.tmp_tools/CheckProductionOrigin`. Die Groessenordnung genuegt aber, um den Punkt auf die
Tagesordnung zu setzen.

Wer die Regel auf weitere Faelle ausdehnen will, setzt eine zusaetzliche benannte Regel vor
`LocalStandardCost` — die Kette ist genau dafuer gebaut. Das ist ein Fachentscheid und
keine reine Technikfrage, deshalb hier nur dokumentiert und nicht umgesetzt.

**Nebenwirkung derselben Konstruktion:** Regel 1 verlangt `groupCost.UnitCost > 0`. Ein
Konzernkostensatz mit Wert `0` und ein voellig fehlender Eintrag sind in der Ausgabe
deshalb nicht unterscheidbar, beide erscheinen bei `LRD` als `Konzernkosten fehlen`.

## 5. CH/AT: Kostenbasis und Beschaffungsindizien

CH/AT haben 0 % Supplier-Fuellung aus strukturellem Grund: die Verkaufsfakturaquelle
besitzt keinen Vorlieferanten. Die TSC-Regel klassifiziert sie als `Intern / TR_AG`.

**Das ist keine Zirkularreferenz.** Klassifikation und Kostenbasis sind getrennte,
endliche Regeln. Das echte Risiko liegt woanders: die pauschale Herstellerregel koennte
einzelne zugekaufte Handels- oder Ersatzteile ebenfalls als intern behandeln.

| Kennzahl | Ergebnis |
| --- | ---: |
| CH/AT-Sales-Zeilen | 48'932 |
| unterschiedliche Materialien | 8'557 |
| Zeilen mit Standardkosten > 0 | 47'350 (96,8 %) |
| Zeilen mit zugeordneter Produktsparte | 48'752 (99,6 %) |
| TRCH-Fremdwaehrungszeilen | 18'723 |
| davon Kostenwaehrung = Belegwaehrung (WAVWR-Pfad) | 18'068 |
| davon positiver CHF-Fallback | 104 |

Die zentrale DB speichert nur den aufgeloesten Stueckpreis, nicht den Rohwert `WAVWR_DC`.

### Beschaffungsindizien, eine Vorpruefung und keine Produktivregel

| Klasse | Materialien | CH/AT-Zeilen | Anteil | Bedeutung |
| --- | ---: | ---: | ---: | --- |
| intern gut gestuetzt | 734 | 8'045 | 16,4 % | Stuecklisten-Indiz oder gleicher Artikel anderswo nur mit Trafag/GFS-Supplier |
| Fremdbezugs-Pruefliste | 1'191 | 5'910 | 12,1 % | echter Einkaufsbeleg zum Material, kein automatischer Beweis fuer Handelsware |
| ohne direkten Nachweis | 6'632 | 34'977 | 71,5 % | Cache liefert kein Indiz in beide Richtungen |

**Wichtige Grenze:** `MaterialUsageCache` enthaelt nur 105 Zeilen. Fehlende BOM-Evidenz
darf deshalb **nicht** als fehlende Eigenfertigung interpretiert werden.

### Priorisierte Stichprobe fuer Andreas

| Material | Bezeichnung | Zeilen | Einkaufsnachweis | Warum relevant |
| --- | --- | ---: | --- | --- |
| F88103 | 8854 Transmitter EX | 65 | STS Sensor Technik Sirnach | fertiges Produkt, fachlich am wichtigsten |
| E11155 | ASIC TRAFAG TR5 | 18 | Aptasic, BESKZ F | staerkstes Fremdbezugsindiz |
| E11221 | ASIC TRAFAG TX2a MLPQ32 | 71 | Presto Engineering France | zugekaufte Elektronik |
| E11228 | ASIC TRAFAG TX2D | 22 | Presto Engineering France | zugekaufte Elektronik |
| E11220 | ASIC TRAFAG TX1b | 13 | Presto / Aptasic | zugekaufte Elektronik |
| C13614 | Diagnostic Valve Block | 56 | Sole Solution / Hwajin | moeglicher Handelsfall |
| C15414 | Vessel Flange | 46 | Plattner / CPT | zugekauftes Mechanikteil |
| D34604 | Cover with opening coated | 35 | Fuchia Electron | zugekauftes Mechanikteil |
| E01389 | Schnappschalter Marquardt | 30 | Omni Ray / Marquardt | zugekauftes Bauteil |
| D85031 | Metallbalg NG36 | 12 | Heitz GmbH | zugekauftes Teil |
| R13025 | Gehaeuse-Unterteil Industat | 4 | an TRIN: Somax Enterprise | einziger externer Kreuzstandort-Hinweis |

## 6. Einzige offene Fachentscheidung

Andreas muss **nicht** alle offenen Materialien einzeln pruefen. Benoetigt wird nur:

> Gilt fuer Verkaeufe von TRCH/TRAT die Herstellerregel `Intern / TR_AG` auch dann, wenn
> zum verkauften Material ein externer Einkaufsbeleg existiert, oder sollen solche
> Materialien als Ausnahme nach `Extern` klassifiziert werden?

Abnahmeweg:

1. Stichprobe oben einordnen, insbesondere `F88103` und `R13025`.
2. Erzeugen externe Einkaufsbelege **keine** Ausnahme, bleibt die heutige Regel; die 6'632
   Cache-Luecken brauchen keine Einzelpruefung.
3. Erzeugen sie eine Ausnahme, zuerst eine fachliche Zusatzbedingung definieren
   (Materialart, Verkaufsrolle), **nicht** blind alle 1'191 Materialien umklassifizieren.
4. Optional den Stuecklisten-Cache vollstaendig laden.

### Nachtrag 31.08.2026: technische Umsetzung liegt vor, Fachentscheid steht noch aus

Fuer den Fall, dass Andreas die Ausnahme will, existiert jetzt ein Schalter — die
Fachentscheidung selbst ist damit **nicht** getroffen, `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`
fuehrt ISS-003.4 weiterhin als `Offen, wartet auf Entscheid`.

`Admin Bereich > Settings > Export Einstellungen`, Feld
`CH/AT: Herstellerregel gegen Fremdbezugsbeleg`:

- `Herstellerregel gilt immer` — Default, produktiv, heutiges Verhalten unveraendert
- `Fremdbezugsbeleg bricht die Herstellerregel` — die in diesem Abschnitt beschriebene Ausnahme

**Mechanik:** der Schalter greift ausschliesslich am TSC-Kurzschluss aus Abschnitt 5
(`IsIntercompanySellingTsc`, jede TRCH/TRAT-Zeile ist unbedingt `Intern / TR_AG`), nicht am
generischen MARC-1100-Fallback aus Abschnitt 1 fuer Fremdstandorte — die beiden Mechanismen
bleiben getrennt.

**Datengrundlage, kein neuer SAP-Zugriff:** die Evidenz kommt aus dem bereits geladenen
Einkauf-Cache (`PurchasingEkpoCache`/`PurchasingEkkoCache`) des bestehenden
Einkauf-Dashboards. Ein Material zaehlt als Fremdbezug bei einer aktiven, nicht geloeschten
Position (`Loekz` leer) mit einem Lieferanten ohne Trafag/GFS-Marker.

**Nachgemessen 31.08.2026 gegen die Produktiv-DB:**

| Kennzahl | Ergebnis |
| --- | ---: |
| Materialien mit aktivem Fremdbezugsbeleg, verknuepft mit TRCH/TRAT-Verkaufszeilen | 1'201 |
| davon betroffene CH/AT-Verkaufszeilen | 5'886 |
| Vergleich zur Pruefliste oben | 1'191 / 5'910 — deckt sich, kleine Drift durch Datenstand |
| `PurchasingEkkoCache`, Bukrs-Verteilung | 176'202 von 176'203 Zeilen `Bukrs = 1100` |

**Wichtige Einschraenkung fuer Andreas:** der Einkauf-Cache ist praktisch ausschliesslich
Bukrs `1100` (Schweiz) gefuellt. Fuer TRAT liegt aktuell kaum eigene Fremdbezugsevidenz vor
— der Schalter wirkt also faktisch fast nur auf TRCH-Zeilen, auch wenn er formal fuer
TRCH/TRAT gemeinsam gilt.

Diese Einschraenkung steht jetzt auch im Hilfetext des Schalters selbst und in der
Cockpit-Meldung, nicht nur hier.

**Status:** produktiv deployed am 31.08.2026 um 11:32 (655/655 Tests gruen). Der
produktive Default ist read-only nachgemessen `MarcForeignProcurementMode = Ignore`, das
bisherige Verhalten bleibt also bestehen; die Fachentscheidung von Andreas steht weiterhin
aus. Noch nicht visuell im angemeldeten Browser gegengeprueft. Umsetzung: `Models/ExportSettings.cs`
(`MarcForeignProcurementMode`), `Services/GroupMarginSupplierClassifier.cs`,
`Services/ForeignProcurementEvidenceStore.cs` (neu), `Services/GroupMarginCalculator.cs`,
`Services/ManagementCockpitService.cs`, `Services/ExcelExportService.cs`,
`Services/SettingsPageService.cs`, `Models/ConfigTransferPackage.cs` /
`Services/ConfigTransferService.cs`, Schema-Spalte in
`Services/DatabaseInitializationService.SchemaSql.cs` /
`Services/DatabaseSchemaMaintenanceService.cs`, UI-Text in `Components/Pages/Settings.razor`
inkl. aller sechs Fremdsprachen in `Services/UiTextGeneratedTranslations.cs`.

## 7. Weitere offene Punkte

- DE-Supplier-Spalten: eigene Export-Query am 2026-09-29 geprueft, sie liest keine Lieferantenspalte
  (siehe Abschnitt 3). Naechster Schritt ist ein read-only Spaltenauszug aus
  `INFORMATION_SCHEMA.COLUMNS` (`%Liefer%`, `%Artikel%`, `%Kredit%`) durch die IT vor Ort, danach Query
  ueber `ArtikelID` und Import erweitern. Keine Bitte um Datenpflege.
- ES: Die aktive Export-SQL liest nur `CabeceraAlbaranCliente`/`LineasAlbaranCliente`; die dort
  vorhandenen `CodigoFabricanteLc`/`CodigoFamiliaFabricanteLc` sind leer. Einen Artikel-Lieferanten
  zeigen unsere Schema-Auszuege nicht; Lieferantennummern stehen nur auf der Einkaufsseite
  (`LineasAlbaranProveedor` u. a. mit `CodigoProveedor` und `CodigoArticulo`). Naechster Schritt:
  Spaltenauszug `%Proveedor%`/`%Articulo%`; eine Herleitung aus der Einkaufshistorie waere ein
  Fachentscheid.
- US: Quellfeld und Mapping bestimmen.
- FR: ungefuellte Zeilen nach Quelle und Artikelstamm segmentieren.
- Verbleibende TRIT-Zeilen nach Ursache segmentieren. Der grosse Teil ist mit Abschnitt 8 geklaert; offen bleiben `11` Zeilen ohne Treffer in der Uebergangsliste.

## 8. Italien: Uebergangsweise Lieferantenzuordnung (Stand 2026-08-26)

### Warum es sie gibt

Trafag Italia hat auf die Bitte vom 2026-07-31 geantwortet. Paola Castagna hat die 942
Artikel ohne `OITM.CardCode` geprueft und den Lieferanten je Artikel geliefert. Ihre
Rueckmeldung vom 2026-08-26 enthaelt aber eine ausdrueckliche Einschraenkung:

> *"At the moment I have only updated the Excel file itself, not yet the item master in B1.
> The vendors have not been maintained on OITM.CardCode yet ... I do not have access to mass
> updates via DTW and will need to enter them manually one by one."*

Der Import liest den Lieferanten ausschliesslich aus `OITM.CardCode`
(`Services/HanaQueryService.cs`). Ohne Pflege im Artikelstamm bliebe die Antwort damit
wirkungslos, obwohl die fachliche Arbeit gemacht ist. Bis Italien die Pflege nachgezogen hat,
dient die Rueckmeldung deshalb als Ersatzquelle.

### Wie sie funktioniert

Tabelle `SupplierMaterialOverrides` je Standort und normalisierter Materialnummer, befuellt
aus einer im DLL eingebetteten Liste (`Data/supplier_overrides_TRIT_2026-08-26.csv`, `941`
Zeilen). Angewendet wird sie in `Services/DataSources/HanaDataSourceAdapter.cs`, direkt vor
der Konzernkosten-Fortschreibung.

Zwei Regeln sind hart abgesichert (`SupplierMaterialOverrideStoreTests`):

1. **Es wird nie ueberschrieben.** Der Ersatzwert greift nur, wenn alle drei Lieferantenfelder
   leer sind. Sobald Italien einen Artikel im Stamm pflegt, gewinnt automatisch wieder die
   Quelle und der Eintrag wird wirkungslos. Die Loesung baut sich also von selbst ab.
2. **Eine Zuordnung wirkt nie ueber den Standort hinaus**, auch wenn dieselbe Materialnummer
   bei einem anderen TSC vorkommt.

Von den 942 Zeilen der Rueckmeldung sind `941` verwertbar. Die eine verworfene Zeile ist eine
Bonusgutschrift ohne Artikelnummer (`PREMIO PER RAGGIUNGIMENTO FATTURATO ANNO 2025`), die im
Artikelstamm gar nicht existieren kann. Ebenfalls nachgetragen: das bei
`OFFICINE MECCANICHE M.A.M. S.R.L.` fehlende Lieferantenland `IT`, von Paola am 2026-08-26
bestaetigt.

### Gemessene Wirkung, Produktivexport `Sales_All_2026-08-25 (1).xlsx`

Werkzeug `.tmp_tools/CheckItalySupplierImpact`, read-only. Von `19'179` TRIT-Zeilen sind
`5'088` ohne jeden Lieferanten; die Liste trifft davon `5'077`, es bleiben `11` offen.

| Wird zu | Zeilen | Bisherige Kostenquelle | Wirkung |
| --- | ---: | --- | --- |
| TR AG | 4'089 | `Konzernkosten TR AG (MBEW-STPRS)` | **keine** — der Material-Fallback erkennt sie heute schon, sie stehen bereits auf `Kostenwaehrung abweichend` |
| TR AG | 12 | `Standardkosten der lokalen Gesellschaft` | verlieren die Marge, weil CHF-Kosten gegen EUR-Umsatz stehen |
| TR IT | 301 | `Standardkosten der lokalen Gesellschaft` | **Verbesserung**, Kosten in EUR, kein Waehrungskonflikt; darunter `134` Zeilen mit heute `Standardpreis fehlt` |
| extern | 675 | `Interner Standardpreis` (670) | **Korrektur** — diese Zeilen gelten heute faelschlich als konzernintern, obwohl ITEC, Senseca und Eletta Flow Fremdlieferanten sind |

**Wichtige Richtigstellung zur ersten Einschaetzung.** Die Sorge, die `4'101` Trafag-AG-Zeilen
wuerden durch den Waehrungs-Guard ihre Marge verlieren, hat sich nicht bestaetigt: `4'089`
davon rechnen bereits heute auf TR-AG-Konzernkosten und tragen bereits den Status
`Kostenwaehrung abweichend`. Die Zuordnung macht diese Klassifikation nur explizit statt
abgeleitet. Betroffen sind lediglich `12` Zeilen.

Der eigentliche Gewinn liegt woanders: `670` Zeilen mit rund `474'000` EUR Umsatz werden heute
als konzernintern behandelt, obwohl der Lieferant ein Dritter ist.

### Wann sie wieder verschwindet

Sobald Paola den Artikelstamm vollstaendig gepflegt hat. Pruefkriterium: Das Ereignisprotokoll
meldet nach einem TR-IT-Import `Lieferant aus Uebergangsliste ergaenzt` mit
`Zeilen ergaenzt=0`. Dann sind Tabelle, eingebettete Liste und der Anwendungsschritt
ersatzlos entfernbar.

### Das Abbaukriterium war schon am Deploytag erfuellt

Deployed am 2026-08-26 14:08, Funktionscommit `bc46286`. Der Nachweis unmittelbar danach mit
`.tmp_tools/CheckSupplierOverrides` (read-only gegen die Produktivdatenbank) zeigt: die Tabelle
ist angelegt und mit `941` Zeilen befuellt, aber **sie kann nicht mehr greifen**.

| Messung auf der Produktivdatenbank, 2026-08-26 nach dem Deploy | Wert |
| --- | ---: |
| TRIT-Zeilen in `CentralSalesRecords` | 19'968 |
| davon ohne jeden Lieferanten | **12** |
| Umsatzzeilen zu den 941 Materialien der Liste | 5'739 |
| davon ohne Lieferanten | **0** |
| davon mit exakt dem Lieferanten aus Paolas Liste | **5'739** |

Zwischen dem Export vom 2026-08-25 16:15 (`5'088` TRIT-Zeilen ohne Lieferant) und dem Import
vom 2026-08-26 12:10 ist die Luecke verschwunden. Alle `941` Materialien fuehren jetzt genau
den Lieferanten, den Paola gemeldet hat, und der einzige Weg, auf dem dieser Wert in die
Umsatzzeile kommt, ist `OITM.CardCode`. **Der B1-Artikelstamm ist also doch gepflegt worden**,
entgegen der Aussage in Paolas Mail vom selben Vormittag. Die Trefferquote fuer Italien steigt
damit von rund `71 %` auf `99.94 %`.

Konsequenz: Die Uebergangsloesung ist wirkungslos, aber auch harmlos — sie fuellt ausschliesslich
leere Felder und es gibt keine mehr. Sie bleibt vorerst liegen und wird beim naechsten
ohnehin faelligen Deploy ersatzlos entfernt. Ein eigener Deploy nur zum Ausbau lohnt nicht.

**Neue kleine Luecke, die noch offen ist.** Von den `12` verbliebenen Zeilen sind `5` Dienst-
und Bonusbelege ohne Artikelnummer (Weihnachtspraemie, Broschuerenaenderung, `PREMIO PER
RAGGIUNGIMENTO FATTURATO`, eine Periodenabrechnung, ein Entwicklungsanteil fuer BU MAG) — dort
gibt es nichts zu pflegen. Die anderen `5` sind echte Artikel, die in Paolas Liste fehlten:
`54290`, `GC11887`, `GC11902`, `GC11903`, `GC11905`. Die gehoeren bei Gelegenheit nachgemeldet.

## Werkzeuge

- `.tmp_tools/CompareSupplierFallback` — Alt/Neu-Differenz read-only messen
- `.tmp_tools/RefreshChPlantMaterialMaster` — SAP-Bestand validieren; ohne `--apply`
  read-only, mit `--apply` atomarer Cache-Backfill
- `.tmp_tools/MeasureAndreasLocalFallback` — Wirkung der lokalen Standardkostenregel
- `.tmp_tools/CheckItalySupplierImpact` — read-only Wirkung der italienischen Uebergangsliste
- `.tmp_tools/CheckSupplierOverrides` — read-only Nachweis der Uebergangsliste auf der Produktivdatenbank
- `.tmp_tools/CheckSupplierClaims0902` — read-only Nachmessung gegen `Sales_All_*.xlsx`: Lieferantenfuellgrad
  je TSC inklusive Teilzeilen-Gegenprobe, Sales-Type-Verteilung, Konfliktfaelle, echte Stufe-4-Kandidatenmenge
  und Lieferantenland bei Trafag AG. Grundlage der Live-Tabelle in Abschnitt 3
- `.tmp_tools/BuildSupplierWorkflowSvg` — erzeugt
  `docs/FINANCE_LIEFERANT_STANDARDKOSTEN_WORKFLOW_2026-09-02.svg` neu. Das Diagramm wird nicht von Hand
  bearbeitet, sondern hier geaendert und neu erzeugt; Rahmen und Zeilenumbrueche rechnet das Skript

Berichte: `docs/Supplier_Laenderstatus_CH_AT_Pruefung_2026-08-11.docx` und
`docs/Supplier_Laenderstatus_CH_AT_Pruefung_mit_Fallback_2026-08-11.docx`.

## Nachweis und Deploymentstatus

Die beiden getrennten Finance-Schalter fuer Fallback und interne Kostenquelle sind am
2026-08-27 um 16:12 produktiv deployed. Produktiv read-only bestaetigt:
`SupplierFallbackMode = ChPlantMaster` und
`InternalSupplierCostSourceMode = DeliveringEntityCosts`, also beide bisherigen Defaults.
`66'049` MARC-Materialien fuer Werk 1100 und alle `63'550` bisherigen MBEW-Schluessel
blieben erhalten; Server-DLL und lokaler Release-Build waren bitgleich. Technischer
Deploynachweis: `docs/DEPLOYMENT.md`.

## Querverweise

- Kostenbasis und Konzern-Standardkosten: `docs/FINANCE_STANDARDKOSTEN.md`
- Indien-Ersatzklassifikation ueber Sales Type: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`
- Gruppenmarge-Fachlogik: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`
- SAP-Rohwertnachweis WAVWR: `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`
- Klassifikationscode: `Services/GroupMarginSupplierClassifier.cs`, `Services/GroupMarginCalculator.cs`
