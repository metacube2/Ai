# Finance: Supplier-Klassifikation, Laenderstatus und CH-Werkstamm-Fallback

Stand: 2026-08-17. Zusammengefuehrt aus vier Vorgaengerdateien (Lueckenanalyse
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
| explizit gepflegter Supplier | hat immer Vorrang |
| Materialnummer fehlt oder Cache leer | `Unklar` |

CH/AT selbst bleiben unberuehrt, dort gilt die vorhandene TSC-Regel.

**Warum `MARC` Werk 1100 und nicht die mandantenweite `MARA`:** MARC belegt, dass das
Material im CH-Werkstamm gefuehrt wird. Der Treffer ist ein Konzern-Stammdaten-Fallback,
aber ausdruecklich kein Produktions- oder Warenbewegungsnachweis.

**Warum die Tabelle `GroupMaterialMasters` von `GroupStandardCosts` getrennt ist:** Ein
MARC-Treffer darf intern klassifizieren, aber keine erfundene Kostenbasis erzeugen. Echte
Konzernkosten kommen weiterhin ausschliesslich aus MBEW/`GroupStandardCosts`.

### Umschalter

`Admin Bereich > Settings > Export Einstellungen`, Feld
`Supplier-Fallback ohne Lieferantenangabe`:

- `Neu: CH-Werkstamm (MARC 1100)` — Default, produktiv
- `Alt: CH-Kostentabelle (MBEW 1100)` — historisches Verhalten

Gespeichert in `ExportSettings.SupplierFallbackMode`, im Konfigurationsexport mitgefuehrt.
Dashboard, Finance-Pruefbuch, zentrale Excel und Nachweis-Excel nutzen denselben Modus.
Ist nach einer Migration noch kein MARC-Cache vorhanden, faellt der neue Modus
voruebergehend automatisch auf den alten MBEW-Fallback zurueck.

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

- **Strukturell 100 % leer:** CH, AT, DE, ES, UK — die Quelle liefert kein
  Lieferantenfeld beziehungsweise es existiert kein Mapping.
- **Teilweise gefuellt, B1-Laender:** IT am besten, dann IN, FR, US — dort kommt der Wert
  aus `OITM.CardCode`, dem Standardlieferanten im Artikelstamm, der oft ungepflegt ist.

Diese Unterscheidung ist der Grund, warum eine pauschale Bitte um Feldpflege an die
Standorte falsch waere: bei fuenf Laendern gibt es schlicht nichts zu pflegen.

## 3. Laenderstatus

Basis `neu.xlsx` vom 2026-08-11 mit 96'233 Sales-Zeilen. Aktuellere Gesamtquote:
`18'263` von `97'537` Zeilen (18,7 %) mit allen drei Feldern.

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
oder nur ein Unterschied zweier Datenbestaende, ist **nicht geklaert**. Zu pruefen ist
zuerst die eigene Export-Query, nicht der Standort.

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

## 7. Weitere offene Punkte

- DE-Supplier-Spalten pruefen: 7'332 Zeilen komplett ohne Lieferant, zuerst die eigene
  Export-Query, nicht den Standort fragen.
- ES und US: Quellfeld und Mapping bestimmen.
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

## Werkzeuge

- `.tmp_tools/CompareSupplierFallback` — Alt/Neu-Differenz read-only messen
- `.tmp_tools/RefreshChPlantMaterialMaster` — SAP-Bestand validieren; ohne `--apply`
  read-only, mit `--apply` atomarer Cache-Backfill
- `.tmp_tools/MeasureAndreasLocalFallback` — Wirkung der lokalen Standardkostenregel
- `.tmp_tools/CheckItalySupplierImpact` — read-only Wirkung der italienischen Uebergangsliste

Berichte: `docs/Supplier_Laenderstatus_CH_AT_Pruefung_2026-08-11.docx` und
`docs/Supplier_Laenderstatus_CH_AT_Pruefung_mit_Fallback_2026-08-11.docx`.

## Nachweis und Deploymentstatus

Produktiv deployed am 2026-08-11 (Fallback) und 2026-08-12 (lokale Standardkosten).
`471/471` beziehungsweise `478/478` Tests gruen. Produktiv read-only bestaetigt:
`SupplierFallbackMode = ChPlantMaster`, `66'049` MARC-Materialien fuer Werk 1100, alle
`63'550` bisherigen MBEW-Schluessel enthalten, Server-DLL und lokaler Release-Build
bitgleich. Technischer Deploynachweis: `docs/DEPLOYMENT.md`.

## Querverweise

- Kostenbasis und Konzern-Standardkosten: `docs/FINANCE_STANDARDKOSTEN.md`
- Indien-Ersatzklassifikation ueber Sales Type: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`
- Gruppenmarge-Fachlogik: `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`
- SAP-Rohwertnachweis WAVWR: `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md`
- Klassifikationscode: `Services/GroupMarginSupplierClassifier.cs`, `Services/GroupMarginCalculator.cs`
