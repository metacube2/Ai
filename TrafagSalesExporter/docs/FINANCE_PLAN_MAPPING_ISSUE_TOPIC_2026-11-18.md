# Finance: Plan Artikel-Mapping, Kostenkette und Attribut „Issue Topic“ in Sales_All

Stand: 2026-11-19 14:01 (TRCH/TRAT aus dem Mapping ausgenommen, Entscheid Ingo, `5fece84`, nicht deployed); davor 2026-11-19 13:57 (Status offen: alle Artikel mit Mapping-Bedarf in Mapping-Excel und Tabelle, `06dd7f0`, nicht deployed; Befund TRAT); davor 2026-11-19 13:29 (Nachtrag: use 0 rechnet mit Kosten 0, Materialtext aus MAKTSet, Commit `1576766`); davor 2026-11-19 13:16 (AP1 bis AP4 gebaut im Zweig `finance-mapping-1911`, `75b320d`/`12efb44`, nicht deployed; Abschnitt 0); davor 2026-11-18 17:55 (Plan, nichts gebaut, nichts deployed). Auftrag Ingo 18.11. („mappingtabelle und so, mach zuerst
ein plan“). Grundlage: Besprechung Ingo mit Andreas Stoller am 18.11.2026 ab 14:37 (automatische Abschrift der Aufnahme,
Whisper large-v3, ohne Sprecherkennung) und das Word `Downloads\Offene_Punkte_2026-11-18.docx`, das in der Besprechung
geteilt wurde.

**Vorbehalt zur Quelle:** Die Abschrift hat Hoerfehler („Saab Schweiz“ = SAP Schweiz) und nennt keine Sprecher. Wer was
gesagt hat, ist hier aus dem Zusammenhang gelesen und mit „(vermutlich …)“ gekennzeichnet. Stellen, die sich widersprechen
oder unverstaendlich sind, stehen als offene Frage in Abschnitt 8 und sind **nicht** entschieden.

Zahlen in diesem Plan stammen aus `docs/FINANCE_SUPPLIER.md` 12/12a und `docs/FINANCE_STANDARDKOSTEN.md` 13/13a (Sales_All
18.11., nur Aggregate). Fuer diesen Plan wurde nichts neu gemessen.

## 0. Umsetzung 2026-11-19 (Zweig `finance-mapping-1911`, Commits `75b320d`/`12efb44`, NICHT deployed)

Auftrag und Entscheide Ingo 2026-11-19: E1 = Lesart A (ohne Lieferant nur Trafag AG, heutiger Stand `ChPlantMaster`), E5 =
Texttreffer wirken nur bestaetigt, Regeltreffer sofort, E6 = Variante B (Tabelle `MaterialMappings` in der Dashboard-DB,
Seite `/finance-cockpit/mapping`), E7 = Go fuer AP1. Gebaut sind AP1 bis AP4; Merge und Deploy erst nach dem Einkauf-Deploy.
Tests 1700/1700 im Worktree `C:\TMP\TrafagSalesExporter_fin1911`.

**Issue-Topic-Matrix (verbindlich, Ingo 2026-11-19), Spalten no Supplier / no Mat Number Match / no Cost:**

| Code | Matrix | Loesung | Gerechnet wird |
| --- | --- | --- | --- |
| leer | 0/0/0 | kein Issue | Konzernkosten der liefernden Gesellschaft, bei externem Lieferanten die lokalen Kosten |
| 1 | 1/1/0 | local standard cost | lokale Standardkosten |
| 2 | 0/1/0 | local standard cost | lokale Standardkosten |
| 3 | 0/0/1 | local standard cost | lokale Standardkosten |
| 4 | 1/0/1 | local standard cost | lokale Standardkosten |
| 5 | 1/0/0 | TR AG standard cost | Standardkosten Trafag AG (MBEW) |
| 6 | 1/1/1 | use 0 | Kosten 0 |
| 7 | 0/1/1 | use 0 | Kosten 0 |

Lesart, wie gebaut (`GroupMarginCalculator.ResolveIssueTopic`, einzige Stelle; Excel, Nachweis, Cockpit und Vorschlagslauf
rufen sie ueber `Evaluate`):

- **no Supplier** = keine CH/AT-Regel (TRCH/TRAT verkaufen als Trafag AG), keine Sales-Type-Rolle (Indien FFM/CM/LRD) und
  alle drei Lieferantenfelder leer. Ein Lieferant aus dem Wareneingang oder der italienischen Liste steht in den Feldern und
  zaehlt als vorhanden.
- **no Mat Number Match** = der Kostenschluessel (nach Mapping und I-Praefix-Regel) steht nicht in der Kostentabelle der
  liefernden Gesellschaft; bei Trafag AG zaehlt zusaetzlich der CH-Werkstamm (MARC 1100) als gefunden.
- **no Cost** = bei gefundenem Artikel: Konzernkosten fehlen oder sind 0. Bei nicht gefundenem Artikel (6/7): die lokalen
  Kosten fehlen oder sind 0.
- **Korrektur Ingo 2026-11-19 (gilt, die Zwischenregel „3/4 werden 6/7“ ist ungueltig):** die Matrix gilt woertlich. Ein gefundener
  Artikel bleibt 3 bzw. 4, auch wenn die lokalen Kosten 0 sind; die Kosten werden ueber den bestehenden lokalen Rueckfall 0,
  Margin Status „Standardpreis fehlt“ wie bisher. 6/7 gibt es nur bei nicht gefundenem Artikel und fehlenden lokalen Kosten.
- **Vorlaeufige Codes (von der Matrix nicht abgedeckt, offene Frage an Andreas/Ingo):** 7 auch bei EXTERNEM Lieferanten ohne
  lokale Kosten (extern mit Kosten = leer); 8 = Indien LRD ohne Konzernkosten, Kosten bleiben offen (Entscheid 05.08.,
  Verhalten unveraendert); 9 = Lieferant unklar (keine Lieferantenangabe, keine Materialnummer); 10 = interner Lieferant ohne
  bekannte Kostenquelle (GFS, Trafag GmbH): lokale Kosten.
- **Grenze der Unterscheidung:** Fuer TR IT und TR IN speichert `B1GroupStandardCostBuilder` nur positive Kosten. Ein dort
  gefundener Artikel ohne Kosten ist deshalb nicht von einem nicht gefundenen zu unterscheiden und erscheint als 2 (bzw. 7).

**Abgleich mit dem heutigen Verhalten (Auftrag: melden, nicht umbauen):**

- Die Spalte Issue Topic aendert keine Kosten, keine Kostenquelle und keinen Status (Tests je Matrixfall pruefen Kosten,
  Cost Source und Status mit). Topic 4 und 5 entsprechen dem heutigen Verhalten.
- **Entscheid Ingo 2026-11-19, gebaut in `1576766`: „use 0“ heisst mit Kosten 0 rechnen.** „Standardpreis fehlt“ (Topics 3, 4, 6, 7)
  ist nicht mehr offen und gilt als bekannte Kostenbasis 0 (`GroupMarginStatuses.Open`, `CostBasisUnknown`, neu `ShowsMargin`):
  Marge = Umsatz, Cost Basis CHF und Margin CHF in Finance Details gefuellt, keine offene Kostenzeile, Summary nicht mehr leer;
  Excel-Formel Margin Value/% in Gruppenmarge Details, Cockpit-Detail und Pruefbuch gleich. Statustext zeichengleich, Farbe
  bleibt gelb. Indien LRD (8) bleibt offen. Interner Lieferant ohne Kostenquelle und ohne lokale Kosten ist jetzt 7 statt 10.
  Unveraendert: „saubere Kostenbasis %“ im Cockpit zaehlt nur OK; die Datenqualitaets-Zeile „StandardCost = 0“ zaehlt weiter.
  Der folgende Absatz ist Historie. *(ueberholt am 2026-11-19)* „use 0“ hiess bis dahin „Kosten 0, Marge offen“, nicht „Marge mit Kosten 0 gerechnet“: Status „Standardpreis fehlt“ steht in
  `GroupMarginStatuses.CostBasisUnknown`; `IsCostBasisKnown` ist dafuer falsch. Folge: in Finance Details bleiben Cost Basis
  CHF und Margin CHF leer, die Zeile zaehlt als Open Cost Row, und im Blatt Gruppenmarge Summary setzt die Formel
  `IF(G>0,"",E-F)` Margin Value und Margin % der ganzen Gruppe (Jahr/Land/TSC/Waehrung) auf leer. Soll wirklich mit 0
  gerechnet werden, ist das eine Aenderung der Margenregel (Entscheid Andreas/Ingo), nicht Teil dieses Zweigs.
- **Kosten aendern sich bewusst durch AP1** (erwartet IT rund 401, UK 70, US 2 Zeilen von lokalem Preis auf Kosten TR IN) und
  durch AP4, sobald Mapping-Zeilen wirksam sind.

**AP1, Indien-Schluesselregel:** `B1GroupStandardCostBuilder` legt TR-IN-Kosten zusaetzlich unter der indischen Artikelnummer
ab (Sachnummer gewinnt bei Kollision). `GroupMarginCostRules.FindGroupCost`: TRIT mit liefernder Gesellschaft TR IN schneidet
genau ein vorangestelltes „I“ ab, wenn der volle Schluessel nicht trifft (volle Nummer zuerst, weil echte indische Nummern mit
I beginnen koennen). Sichtbar in der Spalte Mapping („… (Regel I-Praefix TRIT)“).

**AP2, Spalten:** Sales Spalten 58 „Issue Topic“, 59 „Mapping“; Finance Details 45/46; Blatt Lieferantenzuordnung: Issue Topic
als weitere Gruppierung in der LETZTEN Spalte hinter den Monaten (bestehende Spalten bleiben); Feldhilfe im Blatt Finance
Filter Hilfe. Herkunft des Lieferanten (Stamm/Wareneingang/Liste) ist **nicht** gebaut: `SupplierFromGoodsReceipt` wird nicht
in `CentralSalesRecords` gespeichert und kommt in Sales_All nicht an.

**AP3, Vorschlagslauf (Knopf auf der Seite, nie im Export):** Kandidaten = Artikel mit Topic 1, 2, 6, 7 je TSC, mit Gewicht.
Regel „Trafag-Nummer im Text“: genau eine Nummer im lokalen Text, die im CH-Werkstamm steht, Sicherheit 100, Methode Regel
(wirkt nach E5 sofort). Text: Trigramm-Dice zwischen lokalem Text und deutschem Materialtext Trafag AG, ab Sicherheit 40, nur
Vorschlag. Kein Sprachmodell, kein Netz, wiederholgenau. Bestaetigte, abgelehnte und Handzeilen bleiben beim Lauf unangetastet.

**AP4, Mapping-Tabelle:** `MaterialMappings` (eindeutig je TSC und normalisierter Nummer), Import alles oder nichts (doppelte
Schluessel, fremder TSC, fehlende Nummer, unbekannter Status, Sicherheit ausserhalb 0 bis 100), leere Zeilen uebersprungen.
Wirkt nur, wo die Quelle keine Gruppennummer liefert, nur fuer den eigenen TSC, ueberschreibt nie; in Sales_All, Nachweis und
Cockpit gleich. Bei jedem zentralen Export entsteht `Mapping_JJJJ-MM-TT.xlsx` neben Sales_All (auch SharePoint). Nicht
angewendet wird das Mapping in der Spartenanalyse (`ResolveDivision` liest `SalesRecord.GroupMaterialNumber`, das unveraendert
bleibt); bewusst, Produktgruppen kommen nach den Standardkosten (E11).

**Status offen (2026-11-19 13:57, `06dd7f0`, nicht deployed; produktiv laut Hauptsitzung bis `b2a0152`):** Auftrag Ingo: die
Mapping-Excel enthaelt ALLE Artikel mit Mapping-Bedarf (Issue Topic 1, 2, 6, 7), auch ohne Treffer. Neuer Status „offen“
(Gruppennummer und Verfahren leer, Sicherheit 0, wirkt nie), mit Gewicht Zeilen und Finance Net Sales Actual. **Entscheid
Speicherung:** beides, getrennt nach Ausloeser. Der Vorschlagslauf (Knopf) speichert offene Zeilen in `MaterialMappings`, damit
sie auf der Seite sichtbar und filterbar sind und Andreas sie per Import ergaenzen kann; er fuehrt das Gewicht nach und entfernt
offene Zeilen ohne Bedarf. Der taegliche Export ergaenzt in `Mapping_*.xlsx` fehlende offene Zeilen aus demselben Datenstand,
schreibt aber nicht in die Datenbank (ein Export soll keine Stammdaten veraendern, und die Datei ist so auch vor dem ersten
Knopfdruck vollstaendig). Vorschlaege ersetzen offene Zeilen; bestaetigte, abgelehnte und Handzeilen bleiben. Import: offen
ohne Nummer bleibt offen, offen mit nachgetragener Nummer wird Vorschlag (Methode Hand), Status bestaetigt wirkt sofort.
Kandidaten und Gewicht kommen seit diesem Stand nur aus Zeilen mit Finance | Include = TRUE, wie das Issue Topic in Sales_All.

**Befund TRAT (2026-11-19, nur gelesen, nicht umgebaut):** Vorschlaege fuer TRAT (z. B. 901130 -> 37705) entstehen, weil die Kandidatenauswahl jedes Issue Topic 1, 2, 6, 7 nimmt. TRAT ist per CH/AT-Regel Intern mit liefernder Gesellschaft TR AG; gesucht wird im Kostenbereich 1100 und im CH-Werkstamm (MARC Werk 1100). Die AT-Zeilen bekommen ihren lokalen Preis aber aus Bewertungskreis 1200 (`StandardCostEnricher.ValuationAreaByCountry`: AT = 1200), und `SapGatewayPlantMaterialReader` behaelt nur Werk 1100. Ein Material, das nur im AT-Werk gefuehrt wird, ist damit „bei Trafag AG nicht gefunden“ (Topic 2) und rechnet mit dem AT-Standardpreis. Es ist also ein echtes Topic nach heutiger Regel, aber kein Fall fuer ein Text-Mapping: AT verkaufte Materialnummern desselben SAP-Systems, ein Textvorschlag auf eine andere CH-Nummer waere fachlich falsch. *Vermutung, nicht gemessen:* 901130 ist ein Material nur in Werk 1200. Vorschlag zur Entscheidung: TRCH und TRAT aus der Kandidatenauswahl nehmen (die Spalte Issue Topic bleibt) oder fuer TRAT den AT-Werkstamm als „gefunden“ werten. Die bestehenden TRAT-Vorschlaege sollten abgelehnt werden, bis das entschieden ist. *(entschieden, siehe naechster Absatz)*

**Entscheid Ingo 2026-11-19, gebaut in `5fece84` (nicht deployed):** TRCH und TRAT sind aus dem Mapping ausgenommen: keine Vorschlaege, keine offenen Zeilen, keine Regeltreffer (auch nicht „Trafag-Nummer im Text“), weder im Vorschlagslauf noch in `Mapping_*.xlsx`. Zeilen fuer TRCH/TRAT wirken nie (Sperre in `MaterialMappingStore.IsEffective` und im Lookup, auch nach einem Import). Der Vorschlagslauf raeumt alte CH/AT-Zeilen weg, ausser bestaetigten und Handzeilen (die bleiben stehen und wirken nie). Die Spalte Issue Topic bleibt fuer CH/AT unveraendert. Damit sind die Vorschlaege oben zu TRAT erledigt.

**Materialtext Trafag AG:** `GroupMaterialMasters.MaterialText` (neue Spalte) wird aus `MARCSet` gefuellt, sobald das Set die
Property `Maktx` fuehrt (Platzhalter `SapGatewayPlantMaterialReader.MaterialTextProperty`; abgefragt nur, wenn `$metadata` sie
zeigt, dann mit `sap-language=DE`). Heute fuehrt `MARCSet` sie nicht, der Text bleibt leer und der Textabgleich liefert nichts.
**Befund gegen die Annahme „MAKT nur fuer Einkaufsmaterial“:** laut `saptasks/zeichnungsaenderung/NOTES.md` (02.11.) liefert
`MAKTSet` im Dienst `ZPOWERBI_EINKAUF_SRV` den ganzen MAKT-Bestand (rund 68'000 Materialien, alle Sprachen, `$select=Matnr`
44,7 MB in T76). Ein Weg ohne SAP-Aenderung waere damit: `MAKTSet` im Vorschlagslauf lesen und Deutsch waehlen. *(Entscheid Ingo 2026-11-19, gebaut
in `1576766`:)* Der Vorschlagslauf liest `MAKTSet` ueber die Gateway-Site (TRCH bevorzugt, Service-URL und Zugang wie der Export)
mit `$select=Matnr,Spras,Maktx`, `$filter=Spras eq 'D'` (Rueckfall ohne Filter, falls leer oder abgelehnt) und
`sap-language=DE`, parst die Antwort als Strom und schreibt nur deutsche Texte der Werk-1100-Materialien nach
`GroupMaterialMasters.MaterialText`. Faellt MAKTSet aus, zeigt die Seite die Meldung und der Lauf rechnet mit dem gespeicherten
Stand. Der taegliche Werkstamm-Abzug behaelt vorhandene Texte. Live nicht geprueft (kein Skript-Login), erst nach dem Deploy.

## 1. Was besprochen und was zugesagt wurde

**Anlass (Andreas):** Er wollte die Gruppenmarge fuer Industrial Components rechnen, die Italien lokal einkauft und zum Teil
an Spanien weiterliefert (rund 100'000 pro Jahr). Dabei fehlte in Spanien der Lieferant (Daten kamen separat von Borja), und
Italien und Spanien fuehren fuer dasselbe Produkt verschiedene Artikelnummern. Er hat die Nummern ueber die Beschreibung mit
einem Sprachmodell verbunden und die Top 10 von Hand geprueft; das nennt er selbst „kein robustes Verfahren“. Das Geschaeft
ausserhalb Italiens ist heute klein (200'000 bis 300'000, rund 0,3 % des Gesamtumsatzes), soll aber wachsen (Italien beschafft
lokal oder europaweit und liefert an die Toechter; „wird noch diskutiert“).

**Gemeinsame Linie des Gespraechs:**

1. Ziel ist, zu jedem Artikel den **letzten Lieferanten** zu ziehen, „egal, welcher das ist“ (Andreas).
2. Die Kette bricht **nach einem Schritt** ab (Beispiel aus dem Gespraech: Spanien nennt Trafag AG, Trafag AG haette Indien
   als Lieferant, dort wird geschnitten).
3. Wo die Nummern nicht zusammenpassen, hilft eine **Mapping-Tabelle** ueber den Artikeltext, mit sichtbarer Sicherheit und
   der Moeglichkeit, von Hand nachzupruefen.
4. Was nicht zugeordnet werden kann, rechnet mit den **lokalen Standardkosten** und bekommt ein Kennzeichen, **„Issue
   Topic“**, an dem man ablesen kann, an welcher Stelle die Kette abgezweigt ist.
5. Langfristig sollen die Toechter die Gruppen-Artikelnummer in einem Feld ihres ERP fuehren.
6. Die Zuordnung zu Produktgruppen kommt **nach** den Standardkosten.

**Zugesagt von Ingo:** zwei Excel-Dateien an Andreas: (a) Sales_All mit dem Matching und dem Issue Topic, (b) das Mapping
als separate Excel-Tabelle („alte Nummer, neue Nummer und die beiden Texte“). Dazu: bei Italien nachfragen, was es mit den
SCHENKER-Artikeln auf sich hat.

**Zugesagt von Andreas:** nichts Ausdrueckliches. Er schaut die Produktgruppen-Zuordnung „separat“ an (Sprecher unsicher).

## 2. Abgleich mit dem heutigen Stand: was es schon gibt

| Nr | Regel aus dem Gespraech | Stand | Wo |
| --- | --- | --- | --- |
| R1 | Letzter Lieferant je Artikel | **teilweise.** B1 (FR, US, IT, IN): Artikelstamm, sonst letzter Wareneingang, seit 18.11. 09:31 produktiv. ES: letzter Sage-Wareneingang gebaut und deployed, wartet auf Santis Exportlauf. UK: Lieferant in 100 % der Zeilen. DE: fehlt (Alphaplan-Export ohne Lieferant, Rohail). CH/AT: per Regel Trafag AG | `HanaQueryService` (Wareneingang), `docs/FINANCE_SUPPLIER.md` 11, 12 |
| R2 | Abbruch nach einem Schritt | **gibt es.** Genau eine Stufe, keine Rekursion (Entscheid Andreas 09.09.) | `GroupMarginCostRules.GroupStandardCost`, `ResolveDeliveringEntity`; `docs/FINANCE_STANDARDKOSTEN.md` 12 |
| R3 | Kein Lieferant: bei Trafag AG nachsehen, sonst lokale Kosten | **gibt es**, als produktive Einstellung `SupplierFallbackMode = ChPlantMaster`: Material im CH-Werkstamm (MARC Werk 1100) ergibt Intern / TR AG mit Kosten aus MBEW; nicht im Werkstamm ergibt „Lokal“ mit lokalem Standardpreis; ohne Materialschluessel „Unklar“. Gesucht wird **nur** bei Trafag AG, nicht bei TR IN und TR IT | `GroupMarginSupplierClassifier.Resolve` / `HasMaterialFallbackMatch` |
| R4 | Kennzeichen „ohne Lieferant“ | **teilweise.** „Lokal“ und „Unklar“ gibt es nur ohne Lieferant. Dass ein Intern / TR AG aus dem Werkstamm stammt und nicht aus einem Lieferantenfeld, sieht man in Sales_All nur daran, dass die rohen Lieferantenspalten leer sind. Dass ein Lieferant aus dem Wareneingang stammt, ist intern bekannt (`SupplierFromGoodsReceipt`), aber nicht im Export | `ExcelExportService`, `docs/FINANCE_SUPPLIER.md` 11 („weiter offen“) |
| R5 | Lieferant intern, Artikel dort nicht gefunden: lokale Kosten | **gibt es als Rechnung, nicht als Kennzeichen.** Die Zeile faellt auf den lokalen Standardpreis, Cost Source „Interner Standardpreis“, Status „OK“. Ob der Artikel fehlte oder nur der Preis null war, ist nicht unterscheidbar | Regelkette `CostRules`, `ResolveCostSource` |
| R6 | Lokale Kosten null: als Problem ausweisen | **gibt es.** Margin Status „Standardpreis fehlt“ | `GroupMarginCalculator.ResolveStatus` |
| R7 | „Loesung ist immer lokale Kosten“ | **gibt es mit einer bewussten Ausnahme.** Indien, Sales Type LRD ohne Konzernkosten: Kosten bleiben offen („Konzernkosten fehlen“), weil der lokale Wert der IC-Preis ist (Entscheid 05.08.). 143 Zeilen | `GroupMarginCostRules.GroupDistributionWithoutGroupCost` |
| R8 | Mapping-Tabelle lokale Nummer auf Gruppennummer | **fehlt.** Es gibt drei Bausteine, an die sie anschliessen kann: das Feld `GroupMaterialNumber` (heute nur aus B1 `OITM.U_TASC_OMN`, praktisch nur Indien), das der Kostenschluessel schon bevorzugt (`ResolveGroupCostKey`); die italienische Lieferanten-Uebergangsliste (`SupplierMaterialOverrideStore`, Artikel auf Lieferant, nicht Artikel auf Artikel); die Sicht „Spartenanalyse > Zentrale Zuordnung“ mit dem Status „Nicht im TR-AG-Stamm“ | `GroupMarginCalculator.ResolveGroupCostKey`, `B1GroupStandardCostBuilder`, `Data/supplier_overrides_TRIT_2026-08-26.csv` |
| R9 | Issue Topic als Spalte | **fehlt.** Naechste Verwandte sind Cost Source und Margin Status (Abschnitt 4) | – |
| R10 | Produktgruppe ueber Trafag AG | **gibt es** fuer Artikel im TR-AG-Stamm (`ResolveDivision`: eigene Sparte, sonst Material, sonst `GroupMaterialNumber`); **offen** fuer Artikel, die Trafag AG nicht kennt | `Services/Sales/SalesAnalytics.cs`, `docs/rag/FINANCE.md` „Spartenanalyse Kurzlogik“ |

**Die drei wichtigsten Befunde daraus:**

1. Die Kostenkette, die im Gespraech beschrieben wurde (Lieferant, ein Schritt, ohne Lieferant Trafag AG, sonst lokal), **ist
   der heutige produktive Stand**. Zu bauen ist nicht die Kette, sondern ihre Sichtbarkeit (Issue Topic) und die Zuordnung der
   Nummern (Mapping).
2. Ein Mapping muss die Rechenregeln nicht aendern: Es genuegt, `GroupMaterialNumber` zu fuellen, wo die Quelle es leer
   laesst. Kostenschluessel, Werkstamm-Abgleich und Spartenzuordnung lesen dieses Feld schon.
3. Der Satz „Loesung ist immer lokale Kosten“ widerspricht dem Entscheid vom 05.08. fuer Indien LRD (Kosten bleiben offen).
   Das ist ein Entscheid (E4), kein Programmierdetail.

## 3. Regelwerk Kostenkette, wie es im Gespraech steht

| Schritt | Regel | Heute |
| --- | --- | --- |
| 1 | Lieferant der Zeile = letzter Lieferant des Artikels | R1 |
| 2 | Lieferant extern: lokale Standardkosten der verkaufenden Gesellschaft | gibt es („Kosten aus Verkaufszeile“) |
| 3 | Lieferant intern (Trafag AG, Indien, Italien): Kosten dieser Gesellschaft, **ein** Schritt | R2 |
| 4 | Artikel bei der liefernden Gesellschaft nicht gefunden, auch nicht ueber das Mapping: lokale Standardkosten, Issue Topic setzen | Rechnung R5, Kennzeichen fehlt |
| 5 | Artikel gefunden, Kosten fehlen oder null: lokale Standardkosten, Issue Topic setzen | wie 4 |
| 6 | Kein Lieferant: bei Trafag AG suchen; gefunden: Kosten Trafag AG; nicht gefunden: lokale Standardkosten; in beiden Faellen kenntlich machen | R3, Kennzeichen teilweise (R4) |
| 7 | Lokale Standardkosten fehlen oder null: ausweisen und der Ursache nachgehen | R6 |

**Offener Entscheid E1: Wo wird gesucht, wenn kein Lieferant steht?** Das Gespraech hat zwei Stellen, die sich widersprechen.

- *Lesart A, nur Trafag AG (frueh im Gespraech, bei der Durchsicht von Teil 2 des Word):* Vorschlag „vielleicht in dieser
  Reihenfolge durchgehen. Trafag AG, Indien, Italien, oder?“, Antwort „Ne, wuerde ich gar nicht machen“. Danach (vermutlich
  Ingo): „Wir schauen, ob Trafag drinsteht. Und wenn es leer ist, … ist es im SAP Schweiz drin? Und wenn es dann auch nicht
  drin ist, nehmen wir die Kosten vom lokalen Land.“ Rueckfrage (vermutlich Andreas): „Geht er dann nur Trafag AG durch? Oder
  geht er alle drei durch?“, Antwort: „Nein, da geht er Trafag.“, Schluss: „Alles gut. Das koennen wir so machen, ja.“ mit
  dem Zusatz, es brauche ein Kennzeichen „ohne Lieferant“.
- *Lesart B, alle drei Tabellen (spaeter, beim Beispiel Indien / PCB):* „wenn kein Lieferant steht, dann muss er eigentlich
  die Tabelle Trafag AG durchgehen, dann die Tabelle Indien und eine Tabelle Italien; wenn [er] dann nirgends was findet, dann
  haben wir ein Problem“. Die Stelle wird im selben Atemzug relativiert („das sind ja dort bei 195 Artikeln so gewesen von
  diesen Tausenden, aber das wird gar nicht so sein“); wer spricht und worauf sich 195 bezieht, ist nicht erkennbar.

Empfehlung: Lesart A beibehalten (heutiger Stand). Begruendung: Die Kostentabellen von TR IN und TR IT sind zum Teil unter
eigenen Nummern abgelegt; eine Suche ueber alle drei nur nach der Nummer kann zufaellige Treffer liefern. Mit dem letzten
Lieferanten (R1) und dem Mapping schrumpft die Menge ohne Lieferant ohnehin. Lesart B waere eine eigene Regel mit eigener
Messung.

**Folge fuer „Entscheid 1“ (Werkstamm oder immer lokal, `ISS-007.3`, rund 0,85 Mio. CHF Gruppenmarge):** Lesart A ist die
Werkstamm-Regel. Das Gespraech liest sich als Zustimmung („Das koennen wir so machen“), eine ausdrueckliche Bestaetigung
fehlt. Als E2 gefuehrt.

## 4. Attribut „Issue Topic“ in Sales_All

**Im Gespraech genannt:** Bezeichnung „Issue Topic“ (auch „Topic 1, 2, 3“); Zweck: an einer Liste je Topic ablesen, was zu
tun ist („wo es abgezweigt ist“). Die Nummern wurden nicht abschliessend festgelegt: Frueh heisst es „Topic 3 waere zum
Beispiel Lieferant da, Artikel kann im Lieferanten nicht gemappt werden“, spaeter ist das der zweite Fall. Als Stichworte
fielen „no supplier“, „wrong number“, „no cost“ („no cost geht eigentlich gar nicht, irgendeine Kost [wird] immer gezogen“).

**Vorschlag (Nummern und Texte sind Vorschlag, zu bestaetigen in E3):** *(ueberholt am 2026-11-19: Ingo hat die Matrix 1 bis 7 festgelegt, gebaut nach Abschnitt 0; diese Tabelle ist nur noch Historie)*

| Vorschlag Code | Bedingung | Verwendete Kosten | Heute in Sales_All | Was zu tun ist |
| --- | --- | --- | --- | --- |
| leer | Lieferant vorhanden; extern, oder intern mit Kostentreffer | lokal (extern) bzw. Kosten der liefernden Gesellschaft | Cost Source „Kosten aus Verkaufszeile“ bzw. „Konzernkosten TR AG / TR IT / TR IN“ | nichts |
| 1 „No supplier, not found at Trafag AG“ | alle drei Lieferantenfelder leer, Artikel nicht im CH-Werkstamm | lokale Standardkosten | Supplier Type „Lokal“, Cost Source „Standardkosten der lokalen Gesellschaft“ | Lieferant in der Quelle pflegen oder Artikel mappen |
| 1a „No supplier, found at Trafag AG“ | Lieferantenfelder leer, Artikel im CH-Werkstamm | Kosten Trafag AG | Intern / TR AG, **nicht** von einem echten Lieferanten Trafag AG unterscheidbar | Lieferant pflegen; im Gespraech: „waere wahrscheinlich auch ein Issue … muessen [wir] ausweisen“ |
| 2 „Supplier, article not matched“ | Lieferant intern, Artikel bei der liefernden Gesellschaft nicht gefunden (auch nicht ueber das Mapping) | lokale Standardkosten | Cost Source „Interner Standardpreis“, Status „OK“ | Mapping ergaenzen |
| 3 „Supplier, article matched, no cost“ | Lieferant intern, Artikel gefunden, Kosten fehlen oder sind null | lokale Standardkosten | wie 2, nicht unterscheidbar | Stammdaten der liefernden Gesellschaft pruefen |
| 4 „No local cost“ | nach dem Rueckfall sind auch die lokalen Kosten null oder fehlen | keine | Margin Status „Standardpreis fehlt“ | Ursache in der Quelle klaeren |
| 9 „Supplier unclear“ | Lieferantenfelder leer und keine Pruefgrundlage (keine Materialnummer) | lokale Standardkosten | Supplier Type „Unklar“, Status „Lieferant unklar“ | Einzelfall (heute rund 130 Zeilen) |

Nicht im Gespraech, aber im Bestand und deshalb zu regeln: Indien LRD ohne Konzernkosten (Status „Konzernkosten fehlen“, Kosten
bleiben offen). Je nach E4 ist das Topic 2/3 mit lokalem Rueckfall oder ein eigener Code ohne Rueckfall.

**Abgleich mit Cost Source:** Cost Source sagt, **woher die Kosten kommen**; Issue Topic soll sagen, **warum die Kette nicht
bis zu den Konzernkosten kam**. Die Topics 1, 4 und 9 lassen sich heute schon aus Supplier Type, Cost Source und Margin
Status ableiten. Neu waeren: die Trennung 1 gegen 1a, die Trennung 2 gegen 3 (heute beide „Interner Standardpreis“), und dass
ein Lieferant aus dem Wareneingang oder ein Artikel aus dem Mapping stammt.

**Empfehlung: eigene Spalte, Cost Source und Margin Status bleiben zeichengleich.** Grund: Die Statustexte stehen als
Zeichenketten in Excel-Formeln des Nachweises und in der Cockpit-Faerbung (`docs/router/finance.md`, Falle „Statustexte nie
umbenennen“). Die neue Spalte kaeme ans Ende der Blaetter Sales und Finance Details und als weitere Gruppierung ins Blatt
Lieferantenzuordnung; berechnet wird sie an einer Stelle (`GroupMarginCalculator.Evaluate`), damit Export und Cockpit nicht
auseinanderlaufen. Die Kostenwerte aendern sich durch die Spalte nicht.

**Erwartete Groessenordnung nach heutigem Stand (aus 12a und 13a abgeleitet, nicht mit der neuen Spalte gemessen):** Topic 2
oder 3 vor allem UK (1'586 Zeilen), dazu IT Ware aus Indien (448), IT Trafag Italia ohne Standardpreis (192), UK/US Ware aus
Indien (91); Topic 1 vor allem DE (rund 7'800 Zeilen, 99,7 % lokal), ES (rund 35 % des Umsatzes lokal), FR (391), US (89);
Topic 1a ES 5'163, US 495, DE 139, FR 6.

## 5. Mapping-Tabelle Artikelnummern

**Zweck:** Zu einer Artikelnummer einer Gesellschaft die Gruppen-Artikelnummer (Sachnummer Trafag AG) liefern, damit Schritt 3
und Schritt 6 der Kette die Kosten finden. Sie ersetzt keine Lieferantenangabe.

**Spalten (Vorschlag; im Gespraech genannt sind alte Nummer, neue Nummer, beide Texte und eine Sicherheit, dazu der Wunsch,
die Artikelnummer des Lieferanten sichtbar zu machen):**

| Spalte | Inhalt |
| --- | --- |
| Gesellschaft (TSC) | verkaufende Gesellschaft, fuer die die Zeile gilt; wirkt nie auf einen anderen TSC |
| Lokale Artikelnummer, lokaler Text | wie in Sales_All |
| Gruppen-Artikelnummer, Text Trafag AG | Ziel der Zuordnung |
| Liefernde Gesellschaft, deren Artikelnummer | nur wo sie abweicht (Indien fuehrt eigene Nummern) |
| Verfahren | Stufe 1 bis 4 (unten) |
| Sicherheit | 100 bei Regel, sonst Wert der Textaehnlichkeit („100 ist gut, bei anderen ist es unsicher“) |
| Status | Vorschlag / bestaetigt / abgelehnt |
| Geprueft von, am | Name und Datum |
| Zeilen, Umsatz (2025/2026) | Gewicht, damit zuerst das Grosse geprueft wird |

**Stufen des Matchings:**

| Stufe | Verfahren | Wirkt automatisch? |
| --- | --- | --- |
| 1 | Nummer stimmt nach Normalisierung ueberein | ja, heute schon, braucht keine Tabellenzeile |
| 2 | feste Regel: Praefix („I“ + Nummer in Italien), indische Artikelnummer als zweiter Schluessel, Typencode im Text | ja, sobald die Regel gebaut oder die Regelzeile erzeugt ist |
| 3 | Textaehnlichkeit (unscharfe Suche oder Sprachmodell ueber die Beschreibung) | nur als Vorschlag; wirkt erst nach Bestaetigung (Empfehlung, E5) |
| 4 | von Hand durch die Gesellschaft oder Finance | ja |

Stufe 3 laeuft **nicht** im taeglichen Export: Sie ist nicht wiederholgenau, und ein falscher Treffer ergaebe unbemerkt falsche
Konzernkosten. Vorschlaege entstehen in einem getrennten Lauf und landen als Excel bei den Pruefern; die Tabelle, die der
Export liest, ist fest.

**Quellen je Land, mit dem heute gemessenen Stand:**

| Land | Lage | Was Regel ist | Was Mapping braucht |
| --- | --- | --- | --- |
| IT | Ware aus Indien 448 Zeilen unter „I“ + Nummer oder Hausnummer; Trafag Italia als Lieferant ausschliesslich Hausnummern `M_IT01_…` (13.11.: 1'189 Zeilen, 127 Artikel) | 401 Zeilen: „I“ abschneiden bzw. indische Artikelnummer (Stufe 2) | 47 Zeilen Hausnummern aus Indien; die zwei SCHENKER-Artikel, falls sie von Trafag AG stammen; Industrial Components gegen die Nummern der Toechter |
| UK | Lieferant vorhanden, aber 0 von 3'115 Zeilen mit Trafag-Sachnummer; 295 UK-eigene Artikel; 1'586 Zeilen ohne Konzernkosten; Top 50 Artikel = 74 % des Betrags | 70 Zeilen Ware aus Indien unter indischer Artikelnummer | 295 Artikel; 58 % des Betrags tragen einen Trafag-Typencode im Text (Kandidat fuer Stufe 2 oder 3), 28 % PTM/ATM-Bezeichnungen, 14 % Uebriges |
| DE | kein Lieferant, 99,7 % lokale Kosten, eigene Nummern (`N10.0`); laut Gespraech steht die Trafag-Nummer im Materialtext (Stand „Anfang des Jahres“) | – | Abgleich ueber den Materialtext; hier nicht nachgemessen |
| ES | kein Lieferant im Export bis zu Santis neuem Lauf; 65 % des Umsatzes ueber den Werkstamm bei Trafag AG gefunden | – | der lokale Rest und die Industrial Components aus Italien; Borjas Auswertung (liegt nur Andreas vor) zeigt, dass Spanien eine Zuordnung hat |
| IN | eigene Nummern (im Gespraech: `DM` und `PS` plus Laufnummer); Trafag-Sachnummer in `U_TASC_OMN`; 5 Artikel ohne Sachnummer (143 LRD-Zeilen) | – | **kein** Mapping fuer lokal Gefertigtes (Kosten sind die eigenen); die 5 Artikel in B1 nachpflegen lassen |
| FR, US | 391 bzw. 89 Zeilen lokal ohne Lieferant, 77 US-Zeilen unklar | – | erst nach dem Issue Topic beurteilen |
| CH, AT | Quelle ist Trafag AG | – | nichts |

**Zusammenhang mit der gemessenen Programmluecke „Ware aus Indien“ (473 von 539 Zeilen):** Das ist **Regel, kein Mapping**.
Die Kosten von TR IN zusaetzlich unter der indischen Artikelnummer ablegen (UK 70, US 2 Zeilen) und bei TRIT das
vorangestellte „I“ abschneiden (401 Zeilen). Es bleibt als eigenes kleines Paket (AP1) vor dem Mapping; fuer die
Mapping-Tabelle bleiben davon 66 Zeilen (IT 47, UK 17, US 2).

**Wo die Tabelle liegt (Varianten):**

| Variante | Vorteil | Nachteil |
| --- | --- | --- |
| A. Excel/CSV im Programm eingebettet (wie die italienische Lieferantenliste) | vom Deploy-Nachweis mit abgedeckt, kein neuer Bildschirm | jede Aenderung braucht einen Deploy; fuer einige hundert bis tausend Zeilen mit laufender Pflege ungeeignet |
| B. Tabelle in der Dashboard-Datenbank, Import und Export als Excel auf einer Einstellungsseite | Pflege ohne Deploy; die Excel fuer Andreas ist der Export derselben Tabelle; Status und Pruefer je Zeile | Seite und Import sind zu bauen |
| C. Excel auf einem Laufwerk, beim Export gelesen | schnell | kein Schutz gegen halbe Staende, keine Historie |

Empfehlung: **B**, mit einer ersten Excel aus dem Vorschlagslauf, bevor die Seite gebaut ist. Regeln aus der
Lieferanten-Uebergangsliste uebernehmen: nie ueberschreiben (wirkt nur, wo die Quelle keine Gruppennummer liefert), und nur
fuer den eingetragenen TSC.

## 6. Langfristig: Gruppennummer im ERP der Toechter

Vorschlag aus dem Gespraech: Die Toechter fuehren die Gruppen-Artikelnummer („Standardnummer von der Trafag“) in einem Feld
ihres Artikelstamms, „dann hast du diesen Zauber nicht“. Vorgehen laut Gespraech: Mapping-Tabelle an die Gesellschaft schicken,
bestaetigen lassen, dann einpflegen; der Export zieht das Feld mit. Genannt wurden UK und Spanien; Deutschland steht vor einem
neuen ERP (welches, ist laut Gespraech „noch nicht klar“). Indien fuehrt das Feld schon (`U_TASC_OMN`).

Wer bestaetigt, ist nicht festgelegt (E9). Aus den bisherigen Kontakten naheliegend: UK Cornell, IT Paola, ES Santi/Borja, DE
Rohail; das ist eine Annahme.

## 7. Einzelpunkte aus dem Gespraech

| Punkt | Stand | Naechster Schritt |
| --- | --- | --- |
| SCHENKER-Artikel Italien (zwei Hausnummern, im Namen ein Trafag-Typencode) | Im Gespraech nur Vermutungen (lokal weiterverarbeitet, umgepackt, Produktionsauftrag). Frage steht seit 13.11. im Entwurf an Paola | Ingo fragt nach (zugesagt) |
| Indien, 143 Zeilen / 5 Artikel ohne Trafag-Sachnummer | Gespraech: „Bescheid geben, wo es fehlt, sollen sie nachpflegen“ | Liste der 5 Artikel an Indien; Ansprechperson offen |
| UK, eine Zeile TR09 mit Name Trafag AG | Im Gespraech nur gestreift; rechnet richtig | Entwurf an Cornell liegt vor |
| Spanien, Lieferant | Gespraech: „er muss noch das hinzufuegen“ (Santi) | Santis Exportlauf abwarten |
| Lokal Gefertigtes in Indien | braucht kein Mapping, nur eine Produktgruppe | zusammen mit den Produktgruppen |
| Produktgruppen-Zuordnung | bewusst nach den Standardkosten; offen ist der Fall „bei Trafag AG nicht gefunden“ (lokale Produktgruppen, Industrial Components nur in Italien, im Gespraech rund 1,5 Mio. Umsatz) | E11, kein Paket in diesem Plan |

## 8. Entscheide, bevor gebaut wird

*Stand 2026-11-19: E1 (A), E3 (Matrix Ingo), E5 (nur bestaetigt), E6 (B) und E7 (Go) sind entschieden, siehe Abschnitt 0. E4 bleibt offen (vorlaeufig Code 8, Verhalten unveraendert).*

| Nr | Frage | Wer | Empfehlung |
| --- | --- | --- | --- |
| E1 | Ohne Lieferant nur bei Trafag AG suchen (Lesart A) oder bei Trafag AG, Indien, Italien (Lesart B)? | Andreas | A, heutiger Stand |
| E2 | Ist damit „Entscheid 1“ (Werkstamm oder immer lokal) zugunsten des Werkstamms entschieden? | Andreas, schriftlich | ja, so liest sich das Gespraech |
| E3 | Issue Topic: Faelle, Nummern, Texte (Deutsch oder Englisch), eigener Code fuer „ohne Lieferant, bei Trafag AG gefunden“? | Andreas | Tabelle in Abschnitt 4 |
| E4 | Indien LRD ohne Konzernkosten: Kosten weiter offen lassen (Entscheid 05.08.) oder wie alle anderen auf lokale Kosten fallen? | Andreas / Ingo | offen lassen, eigener Code; der lokale Wert ist dort der IC-Preis |
| E5 | Duerfen Treffer aus der Textaehnlichkeit ohne Bestaetigung wirken (ab welcher Sicherheit)? | Andreas | nein, nur bestaetigte und Regeltreffer |
| E6 | Ablage der Mapping-Tabelle: Variante A, B oder C | Ingo | B |
| E7 | Go fuer die Schluesselregel „Ware aus Indien“ (AP1), offen seit dem Vormittag | Ingo | ja |
| E8 | Reihenfolge der Laender im Mapping | Ingo / Andreas | UK Top 50 zuerst, dann IT Hausnummern, dann DE |
| E9 | Wer bestaetigt das Mapping je Land, und geht die Bitte um ein Gruppennummer-Feld an die Toechter? | Andreas | – |
| E10 | Die zwei zugesagten Excel: aus dem regulaeren Export nach dem Deploy, oder vorab einmalig von Hand erzeugt? | Ingo | regulaerer Export fuer Sales_All; Mapping-Excel als Vorschlagsliste vorab |
| E11 | Produktgruppe fuer Artikel, die Trafag AG nicht kennt | Andreas | spaeter |

## 9. Arbeitspakete

| Paket | Inhalt | Haengt ab von | Aufwand | Tests | Produktiv zu pruefen |
| --- | --- | --- | --- | --- | --- |
| AP0 | Entscheide E1 bis E10 einholen; Borjas Auswertung von Andreas erbitten | – | klein | – | – |
| AP1 | Schluesselregel Ware aus Indien: TR-IN-Kosten auch unter der indischen Artikelnummer, TRIT „I“ abschneiden | E7 | klein | `B1GroupStandardCostBuilderTests`, `GroupMarginCalculatorTests`: beide Schluessel, kein Treffer bei fremder Nummer, „I“ nur bei TRIT und nur bei Lieferant Indien | erster Export: IT 448 auf rund 47 Zeilen „Interner Standardpreis“ bei Lieferant Indien, UK 87 auf rund 17; Zeilen und Umsatz je Land gleich |
| AP2 | Issue Topic rechnen (eine Stelle) und in Sales_All ausgeben (Sales, Finance Details, Lieferantenzuordnung), Feldhilfe; Herkunft des Lieferanten (Stamm, Wareneingang, Liste) mit ausgeben | E3, E4 | mittel | `GroupMarginCalculatorTests` je Fall der Entscheidungstabelle; `CentralExcelSupplierAssignmentTests` (Spalten am Ende, bestehende Spalten unveraendert) | erster Export: Kostenwerte und Margen gegen den Vortag gleich; Summe der Topics = Zeilen ohne Konzernkosten; Stichprobe je Topic |
| AP3 | Vorschlagslauf Mapping ausserhalb des Exports: je Gesellschaft die Artikel mit Topic 1, 2 oder 3, Regelstufe 2 und Textaehnlichkeit, Ausgabe als Excel mit Gewicht und Sicherheit | AP2 (liefert die Menge), E5, E8; Quelle fuer Texte und Typencodes von Trafag AG | mittel | Stichprobe Top 10 je Land von Hand, wie Andreas es getan hat | – (keine Wirkung auf Zahlen) |
| AP4 | Mapping-Tabelle im Dashboard: Tabelle, Excel-Import und -Export, Anwendung beim Export (fuellt die Gruppennummer, ueberschreibt nie, nur eigener TSC), Spalte „Mapping“ in Sales_All | E6, bestaetigte Zeilen aus AP3 | mittel bis gross | Importregeln (doppelte Schluessel, leere Zeilen, fremder TSC), Wirkung auf Kostenschluessel und Sparte, Schema-Migration | Export vor und nach dem Import: nur gemappte Artikel aendern Kostenquelle; Zeilen und Umsatz gleich |
| AP5 | Rueckmeldungen der Laender einarbeiten (UK Cornell, IT Paola inkl. SCHENKER, IN 5 Artikel, ES Santi/Borja, DE Rohail) | Antworten | laufend | – | – |
| AP6 | Spaeter: Gruppennummer-Feld je ERP mitziehen; Produktgruppen fuer nicht gefundene Artikel | E9, E11 | offen | – | – |

Reihenfolge: AP1 und AP2 brauchen kein Mapping und liefern die erste zugesagte Excel (Sales_All mit Issue Topic). AP3 liefert
die zweite (Mapping als Vorschlag). Das Matching wirkt in Sales_All erst mit AP4. Jedes Paket mit Code endet nach `CLAUDE.md`
Abschnitt 3 mit Commit, Doku und Deploy ueber das Werkzeug; Tests im Release-Worktree.

## 10. Nicht geprueft, Annahmen

- Die Sprecherzuordnung in der Abschrift ist geschlossen, nicht belegt. Unverstaendlich blieben unter anderem: die Stelle zu
  Oesterreich („im Oktober eine groessere Menge an Produkten von Indien verkauft, an Demos“), „Standardkurs fehlt … 162“ (im
  Bestand gibt es 162 italienische Artikel ohne Standardlieferant und 192 Trafag-Italia-Zeilen ohne Standardpreis; welches
  gemeint ist, ist offen), „195 Artikel“, der Satz zu den Dateien („wahrscheinlich sind es drei …“) und fast alles zum
  kuenftigen ERP in Deutschland.
- Borjas Auswertung fuer Spanien liegt nicht vor.
- Ob SAP den Typencode am Material fuehrt und ob die Texte von Trafag AG fuer einen Abgleich taugen, ist nicht geprueft
  (`docs/FINANCE_STANDARDKOSTEN.md` 13a: die Schweizer Zeilen in Sales_All fuehren den Typencode nicht im Materialtext).
- Ob die deutschen Materialtexte die Trafag-Nummer wirklich tragen, ist die Aussage aus dem Gespraech, hier nicht gemessen.
- Die Schluesselregel „Ware aus Indien“ ist aus Export und Code abgeleitet; die Tabelle `GroupStandardCosts` wurde nicht
  gelesen (`docs/FINANCE_SUPPLIER.md` 12a).
- Die Groessenordnungen in Abschnitt 4 sind aus bestehenden Messungen abgeleitet, nicht mit der neuen Spalte gezaehlt.
- Ob die B1-Gesellschaften FR, US und IT das Feld `U_TASC_OMN` ueberhaupt haben, ist nicht nachgesehen.
- Aufwandsangaben sind Schaetzungen ohne Detailentwurf.

## Querverweise

- Lieferantenregeln und Stand je Land: `docs/FINANCE_SUPPLIER.md` 11, 12, 12a
- Kostenquellen, Schnitt nach einer Stufe, Ampel: `docs/FINANCE_STANDARDKOSTEN.md` 12, 13, 13a
- Indien, Sales Type und offene Kosten bei LRD: `docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md`
- Statustexte nicht umbenennen: `docs/FINANCE_ANZEIGE_PRUEFUNG_2026-08-06.md` 5a
- Code: `Services/GroupMarginCalculator.cs`, `Services/GroupMarginSupplierClassifier.cs`,
  `Services/B1GroupStandardCostBuilder.cs`, `Services/SupplierMaterialOverrideStore.cs`, `Services/ExcelExportService.cs`
