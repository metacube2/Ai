# Finance: Supplier-Klassifikation, Laenderstatus und CH-Werkstamm-Fallback

Stand: 2026-11-18 18:05 (Besprechung Andreas 14:37: Plan Mapping/Issue Topic in `docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md`, hier nichts geaendert); davor 2026-11-18 11:52 (12a ergaenzt: Indien-Ware in IT ist „I“ + Nummer, 473 von 539 Zeilen im Programm loesbar; Beleg-Nenner berichtigt); davor 11:41 (Abschnitt 12a: Nachpruefung aller Aussagen vom 18.11., vier Berichtigungen, Entscheid 1 betrifft noch rund 0,85 Mio. CHF; Ursache UK/IN gemessen); davor 10:48 (Abschnitt 12 neu aus Sales_All 18.11., erster Export nach dem Deploy: FR und US gruen; Nachweis in Abschnitt 11); davor 10:00 (Abschnitt 12: Ampel je Land zu Andreas' Feststellungen und weitere Feststellungen); davor 09:35 (Abschnitt 11: B1-Lieferant aus dem letzten Wareneingang deployed 09:31 `8d15d48`, mit Spanien `c9208b5`; Messung bestaetigt die Werkstamm-Regel); davor 2026-11-17 abends (Abschnitt 10 neu: Muster, Antwortvorschlag Andreas, Wirkung Entscheid 1 rund 2,5 Mio. CHF, Pruefzeugnisse, Feldhilfe UK veraltet); davor 2026-11-16 (Abschnitt 9 Nachtrag: Blatt „Lieferantenzuordnung“ in Sales_All, `2da2682`, Nachweis am Export 12:11, Nachmessung fuer Andreas); davor 2026-11-13 (Abschnitt 9 neu: Mail Andreas 13.11., Nachmessung je Gesellschaft, Italien 499 Zeilen ohne Lieferant, UK TR09, DE-Kunden als ID, Sales_All mit Supplier Type / Delivering Entity, deployed `df48301`); davor 2026-11-12 (Doku-Pruefung: Widerspruch Abschnitt 2 gegen Abschnitt 3 zur DE-Pruefung aufgeloest, Filterliste des Schemaauszugs in Abschnitt 7); davor 2026-11-12 11:40 (Spanien: Lieferant des letzten Wareneingangs umgesetzt, `c9208b5`, nicht deployed, Santi Gang 1 offen, ISS-003.3); davor 2026-11-12 (Andreas einverstanden); davor 2026-11-11 (Spanien: Weg ueber den Wareneingang belegt); davor 2026-10-29 (Abschnitte 3 und 7 mit Nachtraegen vom 2026-09-02 und 2026-09-29; Grundstand 2026-08-31). Zusammengefuehrt aus vier Vorgaengerdateien (Lueckenanalyse
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
  Import ganz, bei DE haengt es an den Exportspalten von Alphaplan. *(Nachtrag 2026-11-11: Fuer ES liefert der Sage-Wareneingang `CabeceraAlbaranProveedor`/`LineasAlbaranProveedor` den Lieferanten je Artikel; die Herleitung wartet auf den Entscheid von Andreas, ISS-003.3. Nachtrag 2026-11-12: Andreas ist einverstanden, Umsetzung offen.)*
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
~~steht fuer Deutschland noch aus, siehe die offene Frage zu Deutschland in Abschnitt 3~~ ist fuer Deutschland am
2026-09-29 gemacht (berichtigt 2026-11-12, siehe Abschnitt 3, `ISS-003.3`): Die Export-Query liest keine
Lieferantenspalte; offen ist der Schemaauszug, ohne den Tabelle und Spalte nicht bekannt sind.

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
es bleiben nur noch `17` leere Zeilen *(ueberholt am 2026-11-13: 499 Zeilen ohne Lieferant, Abschnitt 9)*. Dadurch steigt die Gesamtquote von 18,7 % auf
26,8 %. Der Auftrag „verbleibende TRIT-Zeilen nach Ursache segmentieren" ist damit
erledigt bis auf diese 17 Zeilen.

Ebenfalls live gemessen und fuer die Klassifikation wichtig: **Sales Type fuehrt
ausschliesslich Indien** (`7'012` von `7'476` Zeilen: `FFM` 6'213, `LRD` 776, `CM` 23).
Alle acht anderen Standorte stehen auf `0`, Italien bei allen `24'095` Zeilen.

**Nicht in `Sales_All` enthalten** *(ueberholt am 2026-11-13, Abschnitt 9: beides steht im Blatt Finance Details, die Zuordnung seit dem 13.11. auch im Blatt Sales)*: eine Spalte `Cost Source` und der Margen-Status. Wer
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
| Spanien | TRES | 5'697 | 0 | 0,0 % | *(11.11.2026)* Lieferant des letzten Wareneingangs je Artikel aus Sage herleiten (Spalten belegt); *12.11.: Andreas einverstanden; umgesetzt `c9208b5`, nicht deployed, wirkt nach Gang 2 bei Santi*, ISS-003.3 |
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
  ueber `ArtikelID` und Import erweitern. Keine Bitte um Datenpflege. *(Ergaenzt 2026-11-12: Fuer die Kundenfelder
  nennen `docs/FINANCE_FELDLUECKEN.md` Abschnitt 6 und `docs/STANDORT_DE_ALPHAPLAN.md` Abschnitt 6 zusaetzlich
  `%Adress%` und `%Kunde%`; ein Auszug mit allen fuenf Filtern deckt Kundenfelder und Lieferant.)*
- ES: Die aktive Export-SQL liest nur `CabeceraAlbaranCliente`/`LineasAlbaranCliente`; die dort
  vorhandenen `CodigoFabricanteLc`/`CodigoFamiliaFabricanteLc` sind leer. Einen Artikel-Lieferanten
  zeigen unsere Schema-Auszuege nicht; Lieferantennummern stehen nur auf der Einkaufsseite
  (`LineasAlbaranProveedor` u. a. mit `CodigoProveedor` und `CodigoArticulo`). Naechster Schritt:
  Spaltenauszug `%Proveedor%`/`%Articulo%`; eine Herleitung aus der Einkaufshistorie waere ein
  Fachentscheid. *(Ueberholt 2026-11-12: Andreas hat entschieden, der Lieferant des letzten Wareneingangs gilt als Supplier;
  umgesetzt in `c9208b5`, nicht deployed, `docs/STANDORT_ES_SAGE.md` Abschnitt 2.)*
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
*Nachgeprueft am 2026-10-29: trotz vieler Deploys seither noch nicht entfernt, der Anwendungsschritt
steht weiter in `Services/DataSources/HanaDataSourceAdapter.cs` (Tests
`SupplierMaterialOverrideStoreTests.cs`). Ob er jetzt ausgebaut wird, ist offen.*

**Neue kleine Luecke, die noch offen ist.** Von den `12` verbliebenen Zeilen sind `5` Dienst-
und Bonusbelege ohne Artikelnummer (Weihnachtspraemie, Broschuerenaenderung, `PREMIO PER
RAGGIUNGIMENTO FATTURATO`, eine Periodenabrechnung, ein Entwicklungsanteil fuer BU MAG) — dort
gibt es nichts zu pflegen. Die anderen `5` sind echte Artikel, die in Paolas Liste fehlten:
`54290`, `GC11887`, `GC11902`, `GC11903`, `GC11905`. Die gehoeren bei Gelegenheit nachgemeldet.

## 9. Mail Andreas 13.11.: Nachmessung, Befunde, wirksame Zuordnung in Sales_All (Stand 2026-11-13)

Andreas Stoller hat am 13.11. die Lieferantenzuordnung in `Sales_All` pivotiert (AT, CH, DE, ES ohne Lieferant; FR, IN, US
groesstenteils leer; IT mit Trafag Italia als Lieferant und einer grossen Zeile ohne Lieferant; UK Nummer TR09 mit zwei Namen;
TR DE Kunden seit Herbst als ID) und nach dem Stand der Group HK gefragt. Auftrag Ingo: alles pruefen und beheben, was ohne
ihn geht; Messung auf der Produktivdatenbank freigegeben.

**Messung, nur lesend, Stand der Daten 12.11.2026 16:31, Rechnungsdatum ab 2025** (ein Durchgang in eine lokale Arbeitskopie,
danach geloescht; `112'027` Verkaufszeilen):

| TSC | Zeilen | mit Lieferant | Quote Zeilen | Quote Umsatz | Sales Type gefuellt |
| --- | ---: | ---: | ---: | ---: | ---: |
| TRAT | 2'089 | 0 | 0,0 % | 0,0 % | 0 |
| TRCH | 55'678 | 0 | 0,0 % | 0,0 % | 0 |
| TRDE | 8'373 | 0 | 0,0 % | 0,0 % | 0 |
| TRES | 8'643 | 0 | 0,0 % | 0,0 % | 0 |
| TRFR | 2'926 | 133 | 4,5 % | 5,8 % | 0 |
| TRIN | 8'380 | 952 | 11,4 % | 21,5 % | 7'892 |
| TRIT | 20'730 | 20'231 | 97,6 % | 92,9 % | 0 |
| TRUK | 3'404 | 3'404 | 100,0 % | 100,0 % | 0 |
| TRUS | 1'804 | 6 | 0,3 % | 0,1 % | 0 |

Weiterhin gilt: keine Zeile hat nur eines oder zwei der Lieferantenfelder. **Produktive Einstellungen** (aus `ExportSettings`
gelesen): `SupplierFallbackMode = ChPlantMaster`, `InternalSupplierCostSourceMode = DeliveringEntityCosts`. Ohne Lieferant
entscheidet also weiter der CH-Werkstamm. **Das ist bewusst so:** Entscheidungsrichtung Ingo vom 27.08.
(`docs/FINANCE_STANDARDKOSTEN.md` 7a), der Werkstamm-Fallback bleibt Standard, Andreas' einfachere Variante B1 („kein Lieferant
= Standardkosten der verkaufenden Gesellschaft“) ist als Schalter waehlbar. Welche Variante gelten soll, steht weiter als
offener Punkt bei Andreas (`docs/FINANCE_STANDARDKOSTEN_SCHNITT_ANDREAS_2026-09-09.md`, `ISS-007.3`).
*(Berichtigt am 2026-11-13 nach Gegenpruefung: die erste Fassung dieses Absatzes nannte den Beschluss „nicht eingestellt“, als
waere das ein Versaeumnis.)*

**Italien, Zeile ohne Lieferant (neu, `ISS-003.6`):** `499` Zeilen mit `1'068'053` EUR ohne Lieferant, davon 2025 `186` Zeilen
mit `500'912` EUR (genau die Zeile in Andreas' Bild) und 2026 `313` Zeilen mit `567'141` EUR. *Damit ueberholt: „es bleiben nur
noch 17 leere Zeilen“ (Abschnitt 3, Stand 02.09.) und „12 Zeilen“ (Abschnitt 8, Stand 26.08.).* Ursache: `405` der Zeilen
(`923'501` EUR, 86 %) gehoeren zu den sechs Kunden, die der Export bis zum 29.09. ausschloss (`f77840a`, `ISS-021`:
Faiveley Transport Italia, Metal Work Service, Fincantieri Nextech, System Ceramics, Elemaster, Wabtec MZT); ihre Artikel
standen deshalb nicht auf Paolas Liste vom August. Der Rest sind neue Artikel (erste Rechnung gehaeuft ab 08/2026). Es sind
`162` Artikel, keiner hat in irgendeiner Zeile einen Lieferanten: `106` numerische Trafag-Sachnummern (`921'598` EUR, z. B. 9R5
Picostat Railway, 8254 Transmitter), `49` sonstige, `7` Hausnummern; dazu `7` Zeilen ohne Artikelnummer (`80'442` EUR,
Dienstleistungsrechnungen an Trafag AG). Liste als Excel im Mailentwurf an Paola.

**Italien, Trafag Italia als Lieferant:** `1'189` Zeilen, `225'095` EUR, `127` Artikel, **ausschliesslich Hausnummern
`M_IT01_*`**. Trafag Italia als interner Lieferant ist nach Beschluss B2 vom 27.08. zulaessig, und vieles ist erkennbar lokal
(`LAVORAZIONE`, `CERTIFICATI`, `RT - P - BASE`). **Offen und nicht belegt:** die zwei groessten Artikel `M_IT01_006802` und
`M_IT01_006340` („8472.79.52.7004.0200.SCHENKER…“, zusammen rund `112'700` EUR, die Haelfte des Werts) sind dem Namen nach
Trafag-Transmitter unter italienischer Hausnummer. Kommen sie von Trafag AG, rechnet die Zeile mit Italiens Einkaufspreis
statt mit den Konzernkosten. Frage steht im Entwurf an Paola. Der Kontenfilter
(`47005%` ohne `4700504%`) waehlt nur die Erloeskonten und ist seit dem 30.09. gegen das Hauptbuch abgestimmt (`ISS-021`); mit
dem Lieferanten hat er nichts zu tun.

**UK, Nummer TR09 mit zwei Namen (neu, `ISS-003.7`):** `TR09` ist Trafag Controls India (`113` Zeilen). Genau **eine** Zeile
traegt `TR09` mit Name `Trafag AG`: Rechnung `0000043677` vom 19.06.2026, Material `44073`, Menge 10, `1'535.50` GBP. Dasselbe
Material laeuft sonst unter `TR01 Trafag AG`. Die Klassifikation liest den Namen (`ResolveDeliveringEntity`), die Zeile rechnet
also mit TR-AG-Kosten; vermutlich ist die Nummer falsch erfasst. Daneben: Trafag AG hat in UK zwei Nummern (`TR08` 3'019
Zeilen, `TR01` 82 Zeilen), `MO01` zwei Namen (Motion 29 Limited, OEM Automatic Limited). Frage an Cornell als Entwurf.

**TR DE, Kunden als ID (`ISS-016`):** seit 09/2026 `751` Zeilen mit `734'524` EUR und `144` internen IDs als
`ALPHAPLAN-ID:<ID>` ohne Namen (09/2026 329 von 339 Zeilen, 10/2026 391 von 391, 11/2026 31 von 31). Gegen die eingebettete
Belegbruecke gehalten: `108` IDs (`618` Zeilen, 88,8 % des Umsatzes) zeigen dort eindeutig auf genau einen Kunden, `35` IDs
(`131` Zeilen) fehlen (neuer Kunde oder neue Adresse), `1` ID ist mehrdeutig. Eine Uebergangsregel „ID eindeutig = Kunde“ wuerde
also den groessten Teil loesen, **hebt aber die Sperre vom 09.09. auf** (`GermanCustomerInvoiceMapping`: zwei interne IDs haben
ihre fachliche Nummer schon einmal gewechselt). Entscheid Ingo offen. Ohne Sperre zu heben: aktuelle Rechnungsliste von Rohail
(verlaengert die Bruecke) und Schemaauszug fuer den dauerhaften Join; beides im Mailentwurf an Rohail, Abfrage
`AlphaplanExportPackage/scripte/schema_auszug_kunde_lieferant_2026-11-13.sql`.

**Umgesetzt und deployed 13.11. 10:31 (`df48301`, gemergt `15105ed`): wirksame Zuordnung in Sales_All.** Blatt `Sales` bekommt
am Ende `Supplier Type` (Spalte 55), `Delivering Entity` (56) und `Cost Source` (57); Blatt `Finance Details` `Supplier Type`
(43) und `Delivering Entity` (44). `Delivering Entity` = `TR AG` / `TR IT` / `TR IN` aus demselben Aufruf wie der Kostenpfad
(`GroupMarginEvaluation.DeliveringEntity`), leer bei Extern, Lokal, Unklar. Nur im zentralen Sales_All, die Standortdateien
bleiben. Zeilen, die die Finance-Regeln ausschliessen, bleiben leer. Hilfeblatt: `Supplier Type` auf die heutige
Regelreihenfolge berichtigt. Tests `CentralExcelSupplierAssignmentTests`, Suite 1559/1559. *Damit ueberholt: „Nicht in
`Sales_All` enthalten: eine Spalte `Cost Source` und der Margen-Status“ (Abschnitt 3); `Cost Source` und `Margin Status` stehen
seit dem 06.10. im Blatt Finance Details, die Zuordnung seit dem 13.11. auch im Blatt Sales.*
**Nachweis am ersten echten Export (`Sales_All_2026-11-13.xlsx`, geschrieben 12:10, 59.8 MB, geprueft 12:12 gegen
`Sales_All_2026-11-12.xlsx`):** Blatt Sales 57 statt 54 Spalten, 1 bis 54 unveraendert, 55 bis 57 `Supplier Type`, `Delivering
Entity`, `Cost Source`; Finance Details (Kopf Zeile 4) 44 statt 42, 1 bis 42 unveraendert, 43/44 die beiden Zuordnungsspalten.
Keine Gesellschaft mit weniger Zeilen (gesamt 113'321 gegen 112'798, Zuwachs nur neue Rechnungen). TRCH und TRAT durchgehend
Intern / TR AG; leer bleibt die Zuordnung **nur** bei Zeilen, die die Finance-Regeln ausschliessen (TRCH 3'319, TRES 1'383,
TRIT 874, TRDE 639, TRUK 351, TRFR 85, TRUS 19, TRAT 4). Wohin die Zeilen **ohne Lieferantenfeld** (Finance Include) heute
laufen: TRIT 503 Intern / TR AG (1.01 Mio. EUR), 1 Lokal, 6 Unklar (80'442 EUR); TRDE 7'598 Lokal, 137 TR AG; TRES 5'125 TR AG,
2'896 Lokal; TRFR 1'293 TR AG, 1'416 Lokal, 12 Unklar; TRUS 1'545 TR AG, 159 Lokal, 76 Unklar; TRIN 6'963 TR IN, 367 TR AG, 75
Lokal, 36 Unklar. Zwei TRUK-Zeilen zeigen `Intern` ohne Entity (Fall `ISS-018.7`, im Hilfeblatt beschrieben). Pruefskript im
Scratchpad der Sitzung (`pruef_export.py`), nur Summen.

**Nachtrag 2026-11-16: Blatt „Lieferantenzuordnung“ in Sales_All (`2da2682`, deployed 09:33).** Andreas pivotiert weiter nach den
rohen Feldern `Supplier number` / `Supplier name` (Spalten Q bis S); bei der Schweiz kommt dort nur eine leere Zeile, obwohl die
wirksame Zuordnung 100 % Intern / TR AG ist. Gemessen an `Sales_All_2026-11-16.xlsx` (Export 08:40, nur Finance Include): TRCH und
TRAT je 100 % Intern / TR AG; TRDE 98,2 % Lokal, 1,8 % TR AG; TRES 63,8 % TR AG, 36,2 % Lokal; TRFR 46,3 % TR AG, 1,8 % TR IT, 49,8 %
Lokal; TRIN 83,9 % TR IN, 13,6 % TR AG; TRIT 49,9 % TR AG, 45,4 % Extern, je rund 2 % TR IN und TR IT; TRUK 91,2 % TR AG; TRUS
86,6 % TR AG, 8,9 % Lokal, 4,3 % Unklar. Cost Source bei CH: 51'269 Zeilen „Konzernkosten TR AG (MBEW-STPRS)“, 1'215 „Interner
Standardpreis“ (AT 1'877 / 215); welche Materialien die 1'215 sind, ist nicht aufgeschluesselt. Neu deshalb ein fertiges Blatt
`Lieferantenzuordnung` (nur zentrales Sales_All, nach `Gruppenmarge Details`): Kopf in Zeile 4, je Zeile `TSC`, `Currency`,
`Supplier Type`, `Delivering Entity`, `Cost Source`, `Rows`, `Share of TSC rows`, `Net Sales Actual`, danach je Monat (`JJJJ-MM`,
Finance | Date, lueckenlos vom ersten bis zum letzten Monat) das Net Sales Actual; feste Werte, keine Formeln; Basis dieselben
Gruppenmargen-Zeilen wie die Spalten im Blatt Sales (nur Finance | Include = TRUE). `GroupMarginProofRow` hat dafuer additiv
`Month`. Tests `CentralExcelSupplierAssignmentTests` (zwei neu), Suite 1595/1595. *(Der Nachweis am ersten echten Export stand bis 16.11. 16:10 aus; er ist erbracht, siehe uebernaechster Absatz.)* „Group HK“ in Andreas' Mail = Konzern-Herstellkosten,
also die Kostenbasis der Gruppenmarge; die Regeln dazu sind seit 27.08. und 09.09. entschieden, er fragt nach dem Stand.

**Nachweis am ersten echten Export (`Sales_All_2026-11-16.xlsx`, geschrieben 12:11, 59.9 MB, geprueft 16:10):** Blatt
`Lieferantenzuordnung` vorhanden (nach `Gruppenmarge Details`), Kopf in Zeile 4 wie beschrieben, Monate 2025-01 bis 2026-11 (23
Spalten), 44 Zeilen. Abgleich gegen das Blatt Sales derselben Datei (Finance | Include = TRUE): je Gesellschaft Zeilen und Net Sales
Actual gleich (9 von 9), je Gesellschaft x Supplier Type x Delivering Entity x Cost Source 44 von 44 gleich (Zeilen, Anteil, Wert),
737 Monatswerte gleich. TRCH 52'554 Zeilen, 100 % Intern / TR AG. Die Hintergrundpruefung von 09:35 hatte den Export verpasst und
„kein neuer Export“ gemeldet, obwohl die Datei seit 12:11 auf dem Server lag (Ursache nicht untersucht, vermutlich Netzunterbruch);
deshalb erst 16:10 nachgeholt. Pruefskript im Scratchpad der Sitzung (`pruef_blatt.py`), nur Summen.

**Fuer die Abstimmung mit Andreas am 16.11. (Auftraege Ingo „mach ein Excel“, „auf neusten Stand bringen“):** Excel
`Group_HK_Regeln_Beispiele_2026-11-16.xlsx` in Ingos Downloads (Uebersicht, zwei entschiedene Regeln und zwei offene Themen je mit
Beispiel als Ablauf; Beispielwerte frei gewaehlt, Tabellen „Groessenordnung“ gemessen) und der Mailentwurf an Andreas auf den Stand
16.11. gebracht (Excel angehaengt, **nicht gesendet**). Dafuer an `Sales_All_2026-11-16.xlsx` (Export 08:40, nur Finance Include)
nachgemessen:

- **Lieferant Trafag Controls India (`ISS-007.4`):** TRIT 453 Zeilen, davon 448 ohne Konzernkosten TR IN („Interner Standardpreis“,
  537'501 EUR) und 5 mit; ersetzt 414 von 420 vom 07.10. TRUK 112 Zeilen, davon 87 ohne (76'722 GBP) und 25 mit; bei TRUK ist die
  Ursache nicht untersucht und es gibt kein eigenes Issue.
- **Zeilen ohne Lieferantenfeld** (Nummer und Name leer; ohne CH/AT und ohne 7'270 TRIN-Zeilen mit Sales Type FFM/CM/LRD): 20'982.
  Davon 8'685 ueber den Werkstamm auf Intern / TR AG (8'569 mit Konzernkosten TR AG, 116 „Interner Standardpreis“), 12'167 Lokal,
  130 Unklar. Je Gesellschaft Intern / TR AG: TRDE 137 von 7'735, TRES 5'145 von 8'063, TRFR 1'293 von 2'721, TRIN 62 von 173,
  TRIT 503 von 510, TRUS 1'545 von 1'780. Mit `SupplierFallbackMode = LocalStandardCosts` (Beschluss Andreas 27.08.) bekaemen die
  8'569 Zeilen eine andere Kostenbasis; das ist aus der Rechenregel abgeleitet, **nicht** mit dem Schalter gemessen. Die Angabe
  10'817 von 22'950 Kandidatenzeilen (27.08.) ist ein aelterer Datenstand.
- **Berichtigung:** Der Faktor drei bis acht aus `docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 10 gilt fuer Trafag-Sachnummern in
  Italiens Kostentabelle, nicht fuer Ware aus Indien. Wie weit Italiens Einkaufspreis ueber den Kosten von TR IN liegt, ist nicht
  gemessen.

**Mailentwuerfe (`.eml` in Ingos Downloads, nicht gesendet):** Antwort an Andreas mit allen Punkten und dem Stand Group HK
(deckt auch Wochen_Todo 84), Rohail (Rechnungsliste, Schemaauszug), Cornell (UK-Nummern), Paola (162 Artikel, nur das Thema
Preferred Vendor).

**Nicht gemacht, braucht Ingo:** DE-Uebergangsregel, Spanien-Deploy (`c9208b5`, jetzt nur aus dem Arbeitszweig ab `15105ed`; *seit 13.11. abends ab `a207923`, produktiv `9100537`; ueberholt am 16.11.: produktiv `2da2682`, Spanien nur ab `70507fc` oder juenger; seit 16.11. 17:06 produktiv `7cc1a62` (Shopfloor-Filter), Spanien nur ab `69ed8a0` oder juenger; seit 17.11. 11:28 produktiv `6a7de50` (Handbestellwesen AD-Abgleich), Spanien nur ab `0197661` oder juenger; seit 17.11. 12:43 produktiv `2e62163` (Handbestellwesen und Shopfloor CO-PA/M*), Spanien nur ab `fd49894` oder juenger*),
Umstellen des Fallback-Schalters, Lesezugriff B1 TR IT fuer `ISS-007.4`.

## 10. Stand 17.11.: Muster fuer Andreas, Antwortvorschlag, Wirkung Entscheid 1 (Stand 2026-11-17 abends)

Kurzauskuenfte und Hilfsmittel aus der Sitzung vom 17.11., nichts am Code geaendert, kein Deploy.

- **Muster-Arbeitsmappe** (Wunsch Ingo, Fragebogen 17.11.: erst Muster, Zusammenzuege als Formeln, Zuordnung je Gesellschaft,
  Lieferant roh, Herstellkosten je Jahr und Monat, HK = Konzern-HK `Cost Basis CHF`):
  `Downloads\Sales_All_Lieferantenzuordnung_HK_Muster_2026-11-16_mit_Ablauf.xlsx` (Stand Sales_All 16.11., Finance Include, 2025/2026;
  Blaetter Uebersicht, Ablauf, Lieferantenzuordnung, Lieferant roh, Herstellkosten, Herstellkosten Monat, Daten; 8'239 Formeln ohne
  Fehler, gegen Python und das Blatt Lieferantenzuordnung des Exports geprueft). Ein taeglicher Export davon ist nicht gebaut
  (spaeter moeglich, Entscheid Ingo offen). Skripte in `.tmp_tools/finance_schlank_2026-11-17/` (nicht versioniert).
- **Erklaerung der Regel:** Artefakt „Group-HK Lieferregel“ (privat in Ingos claude.ai, Beispiele und Zeile je Land) und zwei
  Videos in den Downloads (`Group-HK_Lieferregel_2026-11-17.mp4`, `Group-HK_Lieferregel_mit_Excelzeile_2026-11-17.mp4`, Windows-Stimme
  de-CH, mit echten Zeilen, deshalb nur intern). Inhalt ohne Zahlen fuer Ingos eigenes Erklaervideo an Sitzung 9a geliefert.
- **Cost Source** stammt aus keinem Quellsystem; der Export setzt das Feld (`GroupMarginCalculator`), Werte siehe Feldhilfe in
  Sales_All. *Widerspruch gefunden:* Die Feldhilfe sagt fuer UK „keine Kostenquelle, Kostenbasis fehlt durchgaengig“
  (`ExcelExportService.cs` Kostenquellen-Tabelle), die Daten haben bei 3'074 von 3'115 UK-Zeilen Kosten. Text vermutlich veraltet,
  nicht korrigiert (Code und Deploy, Entscheid Ingo).
- **Wirkung Entscheid 1 (Werkstamm oder immer lokal), aus Sales_All 16.11. nachgerechnet, nicht mit dem Schalter gemessen:**
  8'703 Zeilen ueber den Werkstamm, 14,7 Mio. CHF Umsatz; HK heute 6,10 Mio., mit „immer lokal“ (lokaler Standardpreis x Kurs x Menge)
  8,59 Mio. CHF, Gruppenmarge also rund **2,5 Mio. CHF tiefer** (US 1,21 Mio., ES 0,54, FR 0,54, IT 0,20, DE/IN nahe 0); 181 Zeilen
  ohne lokalen Preis nicht eingerechnet. Die Mehrheit der Werkstamm-Zeilen hat Konzern-HK zwischen 50 und 150 % der lokalen Kosten;
  der lokale Preis enthaelt bei Ware von TR AG in der Regel den IC-Aufschlag (Lesart, nicht belegt).
- **Nebenbefund Pruefzeugnisse:** Die Zertifikat-Artikel `H703xx` haben bei TR AG einen Standardpreis von 0.01 CHF; ueber den
  Werkstamm bekommen sie fast keine HK (rund 280 Zeilen mit Konzern-HK unter 10 % der lokalen Kosten, kleiner Umsatz). Nicht entschieden.
- **Antwort an Andreas:** Der Entwurf vom 13./16.11. (`.eml` in den Downloads) mischt Datenstaende und verweist auf ein Blatt, das es im
  Muster nicht gibt; im Chat vom 17.11. steht ein kuerzerer Vorschlag, geordnet nach Zustaendigkeit (nichts zu tun: CH/AT, IN, IT
  Trafag Italia, UK TR09; Quelle muss liefern: DE Rohail, ES Deploy + Santi, FR/US/IT B1-Stammdaten; Andreas entscheidet: Zeilen ohne
  Lieferant und Ware aus Indien ueber IT/UK). Idee, nicht gebaut und nicht gemessen: Lieferant fuer FR/US/IT aus dem letzten
  Wareneingang in B1 lesen (wie Spanien). Vorher muessen die Entwuerfe an Rohail, Paola und Cornell raus. Gesendet hat Claude nichts.

## 11. B1: Lieferant aus dem letzten Wareneingang (Stand 2026-11-18, deployed 09:31 `8d15d48`)

Auftrag Ingo 18.11. („die fehlenden Felder, die Andreas anmahnt, erweitern, wo sie noch fehlen“). Machbar ist das nur fuer die
B1-Gesellschaften (FR, US, IT, IN): DE braucht Rohail (Alphaplan), ES ist mit `c9208b5` gebaut und wartet auf Deploy und Santi, CH/AT
brauchen per Regel keinen Lieferanten.

**Messung (nur lesend, HanaQ gegen B1, Rechnungen ab 2025, alle Zeilen):**

| TSC | Zeilen ohne Standardlieferant | davon letzter WE Trafag | WE extern | kein WE | Stamm gepflegt: WE = Stamm |
| --- | ---: | ---: | ---: | ---: | --- |
| TRFR | 2'751 | 1'406 (2,54 Mio. EUR) | 953 (65k) | 392 | 80 von 130 |
| TRUS | 1'742 | 1'076 (7,27 Mio. USD) | 56 | 610 | 6 von 6 |
| TRIT | 4'049 | 479 (0,98 Mio. EUR) | 73 | 3'497 | 17'555 von 20'369 |

**Gegen die heutige Zuordnung (Sales_All 16.11., Finance Include, nur Zeilen ohne Lieferant):** Der letzte Wareneingang bestaetigt die
Werkstamm-Regel fast ganz: FR 1'188 von 1'295 Werkstamm-Zeilen (2,34 Mio. CHF) haben als letzten Wareneingang Trafag AG, US 1'039 von
1'548 (5,93 Mio. CHF; 498 ohne WE), IT 444 von 514 (0,90 Mio. CHF). Umklassiert wuerden wenige: FR 15 Werkstamm-Zeilen nach Extern
(32k CHF) und 70 nach TR IT/TR IN, IT 67 nach Extern (66k CHF). Lokal nach Extern (FR 928, US 56) aendert die Kosten nicht. **Folgerung
fuer Entscheid 1 (Werkstamm oder immer lokal): Die Wareneingaenge sprechen fuer den Werkstamm.**

**Gebaut (Worktree `C:\TMP\TrafagSalesExporter_b1we1811`, Zweig `b1-we-lieferant-1811` ab Produktivstand `f2c0267`, Commit `6e4d44f`):**
`HanaQueryService`: ist `OITM.CardCode` leer, gilt der Lieferant des letzten nicht stornierten Wareneingangs (`OPDN`/`PDN1`, Menge > 0)
desselben Artikels am oder vor dem Belegdatum (wie Spanien, Regel V1); Nummer, Name und Land haengen am selben Ausdruck; hoechstens eine
Zeile je Position (`ROW_NUMBER`). Rangfolge: Artikelstamm vor Paolas Uebergangsliste (`SupplierMaterialOverrideStore`, ersetzt jetzt auch
einen Wert aus dem Wareneingang) vor Wareneingang. Scheitert die Abfrage, laeuft sie ohne Wareneingang wie bisher. Live gegen B1 geprueft
(FR, US, IT, Rechnungen und Gutschriften ab 2025): Zeilenzahl und Summe vorher und nachher gleich, keine Position doppelt, Laufzeit
hoechstens 2,1 s; mit Lieferant FR 130 -> 2'489 von 2'881, US 6 -> 1'138 von 1'748, IT 20'369 -> 20'921 von 20'931. TRIN nicht lokal
pruefbar (nur ueber den Server erreichbar). Tests 1624/1624.

*Nachtrag 18.11. 09:31:* **deployed** zusammen mit Spanien `c9208b5` (Merge `8d15d48`, Auftrag Ingo „mach mal den Deploy“). Neu geladen wird ohne eigenen Lauf: der Export liest jedes Mal ab `ExportSettings.DateFilter` 2025-01-01, die Zahlen aendern sich mit dem naechsten Exportlauf je Land. *Damit ueberholt:* „Deploy und Neuladen offen“. **Weiter offen:**
ob in Sales_All sichtbar werden soll, dass ein Lieferant aus dem Wareneingang stammt (heute nur intern, Spanien hat das auch nicht).
Mit dem Deploy werden die Saetze „Standardlieferant im B1-Artikelstamm pflegen“ fuer FR/US im Antwortentwurf an Andreas und im
Regelset-Dokument ueberholt; Paolas Liste mit 162 Artikeln bleibt sinnvoll (ausdrueckliche Zuordnung geht vor).

*Nachweis 18.11. 10:48 am echten Export:* Ingo startete den Lauf 10:06 ueber die Webseite, `Sales_All_2026-11-18.xlsx` 10:15.
Gegen den 17.11.: Zeilen mit Lieferant FR 120 -> 2'437 von 2'846, US 6 -> 1'129 von 1'790, IT 19'696 -> 20'214 von 20'220, IN
960 -> 1'380 von 8'453; Zeilen und Umsatz in FR, US, DE und UK rappengenau gleich. Die Messung oben (FR 2'489, US 1'138 auf B1) liegt
leicht hoeher, weil der Export Finance-Ausschluesse und Gutschriften anders zaehlt. Ampel je Land in Abschnitt 12.

## 12. Stand der Feststellungen Andreas je Land, Ampel (Stand 2026-11-18 10:48)

Feststellungen aus Andreas' Mail vom 13.11. **Zahlen neu aus `Sales_All_2026-11-18.xlsx`** (Exportlauf 10:06-10:15, erster Lauf
nach dem Deploy 09:31 `8d15d48`), Blatt Finance Details, Rechnungen 2025/2026; Vergleich mit `Sales_All_2026-11-17.xlsx`. Die Fassung
von 10:00 stand auf Sales_All 16.11. und der Messung vom 18.11. (Abschnitt 11) und ist damit ueberholt.
Gruen = erledigt, Gelb = gebaut/deployed, wartet auf Datenlauf, Orange = wartet auf andere, Rot = Entscheid offen.

| Land | Feststellung | Ampel | Stand 18.11. 10:48 (Zeilen mit Lieferant 17.11. -> 18.11.) | offen bei |
| --- | --- | --- | --- | --- |
| AT, CH | kein Lieferant | gruen | Regel 1: immer TR AG, die Verkaufsfaktura kennt keinen Vorlieferanten; Supplier Type 100 % Intern | - |
| DE | kein Lieferant | orange | 0 von 7'800; Alphaplan-Export ohne Lieferantenspalte; Werkstamm findet DE-eigene Nummern nicht (`N10.0`) | Rohail (Entwurf 18.11., Abwesenheitsnotiz) |
| ES | kein Lieferant | gelb | 0 von 8'105; Lieferant aus letztem Sage-Wareneingang seit 09:31 produktiv (`c9208b5`), braucht den neuen Sage-Export | Santi: neuer Exportlauf |
| FR | grosser Teil ohne | gruen (war gelb) | **120 -> 2'437 von 2'846** (86 %); Supplier Type Lokal 1'418 -> 391 Zeilen, Extern 48 -> 1'001, Intern 1'368 -> 1'442 | Rest 409 Zeilen: 391 lokal (nicht im Werkstamm), 12 unklar, 6 Werkstamm *(berichtigt 12a; vorher „Rest: Entscheid 1“)* |
| US | Grossteil ohne | gruen (war gelb) | **6 -> 1'129 von 1'790** (63 %); Lokal 159 -> 89, Extern 4 -> 60, Intern 1'550 -> 1'564 | Rest 661 Zeilen: 495 Werkstamm (Entscheid 1), 89 lokal, 77 unklar *(berichtigt 12a)* |
| IN | Grossteil ohne | gruen | Sales Type entscheidet; Lieferant zusaetzlich aus Wareneingang 960 -> 1'380 von 8'453, Lokal 76 -> 43 | - |
| IT | Trafag Italia als Lieferant, Accountfilter | gruen | zulaessig (Beschluss 27.08.); Kontenfilter 47005 ohne 4700504 seit 30.09. gegen Hauptbuch abgestimmt; 19'696 -> 20'214 von 20'220 mit Lieferant | - |
| IT | zwei Transmitter-Artikel unter Hausnummer (`M_IT01_006802`, `M_IT01_006340`, im Namen „…SCHENKER“, Abschnitt 9), 192 Trafag-Italia-Zeilen ohne Standardpreis (13.11.: 191), 162 Artikel ohne Standardlieferant | orange | Wareneingang fuellt jetzt die leeren; 67 Zeilen von Intern auf Extern; Paolas Antwort steht aus | Paola (Entwurf 18.11.) |
| UK | TR09 mit zwei Namen | orange | unveraendert (3'115 von 3'115 mit Lieferant); eine Zeile falsch erfasst, rechnet richtig | Cornell (Entwurf 18.11.) |
| alle | Entscheid 1: ohne Lieferant Werkstamm oder lokal | rot | Betrifft nach dem Deploy noch ES 5'163, US 495, DE 139, FR 6 Zeilen (ohne Lieferant, Werkstamm = TR AG); „immer lokal“ = rund 0,85 Mio. CHF weniger Gruppenmarge (vor dem Deploy 2,5 Mio.). Wareneingaenge bestaetigen den Werkstamm ueberwiegend (Export, alle Werkstamm-Zeilen vom 17.11.: FR 1'199 von 1'296, US 1'042 von 1'548, IT 445 von 514; ohne Wareneingang FR 6, US 495). *(Berichtigt 12a; vorher „FR 409, US 661 Zeilen laufen ueber den Werkstamm“, das war falsch.)* | Andreas (Termin 18.11. 13:00) |
| IT, UK | Entscheid 2: Ware aus Indien | rot | IT 448/453, UK 87/112, US 4/19 Zeilen mit lokalem Preis statt Kosten TR IN. Ursache gemessen (12a): Kostenschluessel passt nicht, bei 473 von 539 Zeilen im Programm loesbar (IT „I“ + Nummer, UK/US indische Artikelnummer); damit voraussichtlich kein Entscheid noetig | Claude nach Go Ingo; Andreas zur Kenntnis |

Zeilen und Umsatz in FR, US, DE und UK sind gegenueber dem 17.11. rappengenau gleich; der Deploy hat also nur Felder gefuellt, keine
Zeile verloren oder verdoppelt.

### 12a. Nachpruefung vom 18.11. 11:41 (Auftrag Ingo: alles von Finance pruefen)

Alle Aussagen des Vormittags unabhaengig gegen `Sales_All_2026-11-17.xlsx` und `Sales_All_2026-11-18.xlsx` nachgerechnet (Blatt
Finance Details, Zeilen ueber Gesellschaft, Belegart, Belegnummer, Position und Material verbunden, kein Schluessel doppelt).
Richtig waren: die Ampelzahlen je Land (Abschnitt 12 und `docs/FINANCE_STANDARDKOSTEN.md` 13), Zeilen mit Lieferant je Land,
gleicher Umsatz und gleiche Zeilenzahl, Entscheid 2 (IT 448/453, UK 87/112), UK TR09 (eine Zeile). **Falsch oder ungenau waren:**

| Aussage vom Vormittag | Richtig |
| --- | --- |
| „Rest FR 409, US 661 Zeilen ohne Lieferant laufen ueber den Werkstamm auf TR AG“ | FR: 391 lokal, 12 unklar, nur 6 Werkstamm. US: 495 Werkstamm, 89 lokal, 77 unklar |
| „Immer lokal“ = rund 2,5 Mio. CHF weniger Gruppenmarge | Galt am 16.11. (8'703 Zeilen). Nach dem Deploy noch 5'803 Zeilen: ES 5'163 (4,59 Mio. CHF Umsatz), US 495 (0,83 Mio.), DE 139, FR 6; Wirkung rund **0,85 Mio. CHF** (ES 0,55, US 0,30, DE unter 0,01), dazu 121 Zeilen ohne lokalen Preis (ES 41, US 67, DE 13). Gleiche Rechnung wie in Abschnitt 10 (lokaler Standardpreis x Kurs x Menge). TRIN (286 Zeilen ohne Lieferant mit TR AG) zaehlt nicht dazu: dort entscheidet der Sales Type, nicht der Werkstamm |
| Wareneingang bestaetigt den Werkstamm bei „FR 863, US 522 Zeilen“ (nur im Word „Offene Punkte“) | Die Zahlen stammten aus einer Beispielsuche mit Betragsfilter. Richtig: von allen Werkstamm-Zeilen des 17.11. nennt der Wareneingang Trafag AG bei FR 1'199 von 1'296, US 1'042 von 1'548, IT 445 von 514, IN 86 von 86; einen anderen Lieferanten bei FR 91, US 11, IT 69; ohne Wareneingang FR 6, US 495 (dort kein Beleg). Die Nenner 1'290/1'053 der ersten Fassung von 12a zaehlten nur Zeilen mit Wareneingang |
| „44 US-Zeilen wechseln von lokal auf Konzernkosten“ (nur im Word) | 70 bisher lokale US-Zeilen bekamen einen Lieferanten: 56 extern, 14 intern, davon 7 mit Konzernkosten TR IN. FR: 1'027 Zeilen, 938 extern, 89 intern |
| „SCHENKER ist der Spediteur, echter Lieferant zu klaeren“ (nur im Word) | Falsch gelesen. „SCHENKER“ steht im Artikelnamen der zwei italienischen Hausnummern aus Abschnitt 9; die Frage an Paola ist, ob diese Transmitter von Trafag AG kommen. Als Lieferant kommt SCHENKER in Sales_All nicht vor |
| UK: „30 % mit Schweizer Typencode im Text, 70 % ohne“ (nur im Word) | Das Suchmuster erkannte nur Transmitter. 58 % des Betrags tragen einen Trafag-Typencode (Transmitter 30 %, Druckschalter 21 %, Thermostate 7 %), 28 % PTM/ATM-Bezeichnungen, 14 % Zertifikate und Uebriges |

**Ursache Entscheid 2, Ware aus Indien (neu gemessen, nur gelesen):** `B1GroupStandardCostBuilder` legt die Kosten von TR IN je
Artikel unter der Trafag-Sachnummer ab (`GroupMaterialNumber`), nur ohne Sachnummer unter der indischen Artikelnummer.
- **UK 70 und US 2 Zeilen:** verkaufen unter der indischen Artikelnummer (z. B. `TS000095`); der Artikel hat in Indien die
  Sachnummer `35560` und einen Preis, die Kosten liegen also unter `35560` und werden unter `TS000095` nicht gefunden.
  **Programmluecke, nicht fehlende Daten.** Wo UK die Sachnummer selbst fuehrt (23 Zeilen), stimmen die Kosten. Moegliche Loesung:
  Kosten zusaetzlich unter der indischen Artikelnummer ablegen (nicht gebaut, Entscheid Ingo).
- **UK 17 und US 2 Zeilen:** Nummern, die Indien nicht kennt.
- **IT 448 Zeilen** *(berichtigt am selben Tag; zuerst als „in Indien unbekannt, Zuordnung Paola“ notiert)*: Italien fuehrt die
  Ware unter „I“ plus Nummer. 285 Zeilen (78 % des Betrags) sind „I“ + Trafag-Sachnummer (`I53513` = `53513`), 116 Zeilen (18 %)
  „I“ + indische Artikelnummer (`ITS000713` = `TS000713`); fuer alle 401 hat Indien einen Preis. 47 Zeilen (5 %) laufen unter
  Hausnummern `M_IT01_…`, die Indien nicht kennt. 5 Zeilen mit indischer Nummer (`IR…`) finden die Kosten schon heute.
- **Zusammen:** bei 473 von 539 Zeilen (IT 401, UK 70, US 2) passt nur der Schluessel nicht. Moegliche Loesung im Programm:
  Kosten von TR IN zusaetzlich unter der indischen Artikelnummer suchen und bei TRIT das vorangestellte „I“ abschneiden.
  **Nicht gebaut, Entscheid Ingo.** Abgeleitet aus Export und Code (`B1GroupStandardCostBuilder`, `ResolveGroupCostKey`), in der
  Tabelle `GroupStandardCosts` nicht nachgelesen. Damit ist „Entscheid 2“ voraussichtlich kein Entscheid fuer Andreas mehr.

**Nebenwirkung des Deploys in FR (neu gemessen):** Von 1'290 Zeilen, die der Werkstamm TR AG zuordnete, bleiben 1'199 bei TR AG;
66 wechseln weg (45 interner Standardpreis, 15 extern, 3 TR IN, 3 TR IT). Das erklaert die Marge FR 70,4 -> 69,4 %.

**Ursache UK und IN gemessen** (Detail in `docs/FINANCE_STANDARDKOSTEN.md` 13a): UK liefert keine Schweizer Sachnummer (0 von
3'115 Zeilen), 295 UK-eigene Artikelnummern; IN fehlt bei 5 Artikeln die Trafag-Sachnummer.

**Mailentwurf an Andreas (Vorab-Info, Outlook):** nannte die Messwerte vor dem Export (FR 2'489 von 2'881, US 1'138 von 1'748,
„wirksam mit dem naechsten Exportlauf“, 2,5 Mio.). Stand der Berichtigung siehe `lastchange.md`.

**Weitere Feststellungen 17./18.11. (nur hier festgehalten):**
- `TSC` ist die verkaufende Gesellschaft (Sales_All Finance Details Spalte G), fuer CH und AT kommt sie aus derselben Quelle (SAP, Netzwerkanzeige `ZSCHWEIZ`).
- `Cost Source` stammt aus keinem Quellsystem, der Export setzt es (`GroupMarginCalculator`); die Feldhilfe fuer UK („keine Kostenquelle“) ist veraltet (Abschnitt 10).
- Der Werkstamm-Abgleich verfehlt Schweizer Produkte, die eine Gesellschaft unter eigener Nummer fuehrt (DE `N10.0`); fuer UK siehe `docs/FINANCE_STANDARDKOSTEN.md` 13 (1'586 interne Zeilen ohne Konzernkosten).
- Regelset mit Excel-Beispielen und Antworten: `Downloads\Regelset_Lieferantenzuordnung_Group_HK_2026-11-18.docx` (fuer Andreas), Ingos Arbeitskopie `Regelset_Lieferantenzuordnung_mit_Beispielen.docx` im Repo-Ordner (nicht versioniert).

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
