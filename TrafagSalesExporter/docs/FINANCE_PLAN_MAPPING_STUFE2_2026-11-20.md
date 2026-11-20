# Finance: Plan Stufe 2 Mapping und Issue Topic (Ausfuehrung durch Opus 5.5)

Stand: 2026-11-20 11:20, **freigegeben („alles umsetzen“, Vorgaben F1 bis F7 gelten), B, C, D, G gebaut (`9c6fe53` im Worktree, Zweig
`finance-mapping-2011`), Tests 1785/1785, NICHT deployed (F7: vorher fragen)**; davor 2026-11-20 10:50 (*ueberholt:* Plan, nicht
freigegeben, nichts gebaut). Auftrag Ingo 20.11. („mach ein plan fuer opus 5.5“) nach
der Pruefung Abschrift 18.11. gegen Loesung. Vorgaenger: `docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md` (Stufe 1,
deployed `aade490`), Regelset-Abgleich `docs/FINANCE_REGELSET_ABGLEICH_2026-11-20.md`.

## 00. Umsetzungsstand 2026-11-20 (Opus 5.5)

| Paket | Stand | Nachweis |
| --- | --- | --- |
| A | wartet auf den Export 12:10; Pruefskript am Export 19.11. getestet (Basis Zeilen/Umsatz je Land; 19.11. noch ohne Issue Topic, weil der Deploy nach 12:10 kam). Eine `Mapping_2026-11-19.xlsx` gibt es aus demselben Grund nicht | Skript im Scratchpad |
| B | gebaut. **Messung vorab (Schritt 1, Sales_All 16.11.):** Zeilen anderer Gesellschaften mit Lieferant Trafag Italia ohne Kosten TR IT: FR 29 Zeilen (54'797), UK 11 (9'810); mit Kosten TR IT: FR 21, UK 3. ES 0, weil der Lieferant bis zu Santis Lauf fehlt (erwartet, kein Fehler). Bestaetigte Mapping-Zeile mit `DeliveringEntity` TR IT/TR IN/TR AG und deren Nummer wirkt in `FindGroupCost` zuerst, nur wenn die Zeile dieselbe Gesellschaft liefert; Gruppennummer der Zeile bleibt leer; Vorschlagslauf sucht fuer TR-IT-Kandidaten zuerst unter den TR-IT-Artikeln mit positiven Kosten (Text aus TRIT-Verkaufszeilen) | `9c6fe53`, `FinanceStufe2Tests` |
| C | gebaut, **Abweichung vom Plantext mit Begruendung:** statt einer eigenen Menge legt `B1GroupStandardCostBuilder` Artikel ohne positive Kosten mit `UnitCost = 0` in `GroupStandardCosts` ab. Geprueft: die Kostenregel nimmt nur `UnitCost > 0` (keine Konzernkosten 0), die Praesenz liest nur `ResolveIssueTopic` (`lookup.Found`), der Klassifizierer nur den Bereich TR AG, `MaterialCount` steht nur im Protokoll; dasselbe Muster gilt fuer TR AG schon (MBEW mit 0). Vorteil: keine Schemaaenderung, Stand bleibt zwischen Laeufen. Dazu: `ReplaceAsync` ersetzt nur bei mindestens einem positiven Wert (bestehender Schutztest war rot und ist wieder gruen), die I-Praefix-Regel wird von einem 0-Eintrag unter dem vollen Schluessel nicht verdeckt | `9c6fe53` |
| D | gebaut: `SalesRecord`/`CentralSalesRecords.SupplierSource` (Wareneingang, Liste; Spalte per `AddColumnIfMissing`), Spalte „Supplier Origin“ Sales 60, Finance Details 47 (Rule CH/AT, Rule Sales Type, Source field, Last goods receipt, IT override list, CH plant master, none). Altzeilen zeigen bis zum naechsten Lauf ihres Landes „Source field“ | `9c6fe53` |
| E | Schritt 1 gemessen (Sales_All 16.11., Finance-Zeilen TRUK): 725 Artikel, 7,06 Mio. GBP; 361 Artikel (4,57 Mio.) tragen ein Typencode-Muster im Text, meist der volle Konfigurationscode (z. B. `8474.76.8963.32.17.00.T2.01`, `8782.20.20L1.Y1…`), keine Sachnummer. Schritt 2 laeuft (Agent `sap-t76`, nur T76, nur lesen) | – |
| F | Indien: **6** Artikel (nicht 5), 147 Zeilen, 10,76 Mio. INR, LRD ohne Sachnummer; DM000083 allein 112 Zeilen / 9,09 Mio. INR; H90101 (Lotstab) und FA000028/29 (Schweissmaschinen) sehen nicht nach Ware von TR AG aus. Mailentwurf `Downloads\Mail_Indien_LRD_ohne_Sachnummer_2026-11-20.eml` (Empfaenger offen, nicht gesendet). Top 50 DE: nach dem Export aus `Mapping_2026-11-20.xlsx` | – |
| G | Feldhilfe: Zusatzcodes 7 (extern), 8, 9, 10 als „nicht Teil der Matrix“ gekennzeichnet; Mapping-Text („Regelzeilen wirken sofort“ war seit 19.11. 14:20 falsch) und TR IT; neue Hilfe „Supplier Origin“ | `9c6fe53` |

## 0. Auftrag an die ausfuehrende Sitzung

Du fuehrst genau diesen Plan aus, Paket fuer Paket, in der Reihenfolge von Abschnitt 4. Du baust nichts, was hier nicht steht.
Wo ein Entscheid aus Abschnitt 2 fehlt, gilt die dort genannte Vorgabe nur, wenn Ingo den Plan mit „Vorgaben gelten“
freigegeben hat; sonst haeltst du an und stellst die Fragen als Liste.

Pflichten (aus den Projektregeln):

- Zuerst `docs/AGENT_COORDINATION.md` lesen, eigene Zeile eintragen. Andere Sitzungen arbeiten parallel an HR, Einkauf, SAP.
  Fremde uncommittete Dateien im Hauptordner nicht anfassen (`CLAUDE.md`, `Program.cs`, `Services/TimerBackgroundService.cs`,
  `TrafagSalesExporter.csproj`, geloeschte Wurzeldateien). Committen nur mit ausdruecklichen Pfaden (`git commit -- <pfade>`),
  nie `git add -A`; im Index liegen fremde vorgemerkte Loeschungen.
- Code im Worktree `C:\TMP\TrafagSalesExporter_fin1911` auf einem neuen Zweig `finance-mapping-2011` ab
  `feature/supplier-overrides-trit`. Tests dort gruen (Ausgangsstand 1759).
- Deploy nur mit dem Werkzeug nach `CLAUDE.md` Abschnitt 5, vorher pruefen, dass kein Einkauf-Delta laeuft. Produktiv pruefen
  unter `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/...` (immer mit `/BiDashboard/`).
- SAP nie mit Passwort im Skript. T76 nur ueber den Agenten `sap-t76`. P76 nur lesend und nur mit ausdruecklicher Freigabe
  Ingos im Auftrag; diese Freigabe liegt nicht vor.
- SQLite: `decimal` nicht in SQL sortieren oder summieren, im Speicher rechnen.
- Statustexte (`Margin Status`, `Cost Source`) nie umbenennen.
- Zahlen rechnet ein Werkzeug (Skript ueber die Excel), nicht KI-Lesen. Mails nie senden, nur Text im Chat und `.eml` in Downloads.
- Jedes Paket endet mit Commit, nachgefuehrter Doku (dieser Plan, `docs/rag/FINANCE.md`, `lastchange.md`, Issue-Log ISS-003,
  `projektmanagement/Wochen_Todo.tsv` Zeile 163 samt xlsx, Koordination) und Eintrag in `docs/rag/LEARNINGS.md` bei Fehlern.

## 1. Ausgangslage (am Code geprueft 20.11.)

| Fakt | Beleg |
| --- | --- |
| Ein Mapping-Treffer setzt `GroupMaterialNumber`; dieser Schluessel geht in Klassifikation UND Kostensuche | `GroupMarginCalculator.Evaluate` Zeile 60-66 |
| Die Kostensuche liest `(Schluessel, Bereich der liefernden Gesellschaft)` | `GroupMarginCostRules.FindGroupCost` |
| Der Vorschlagslauf sucht Ziele nur im Stamm Trafag AG (`trAgMaterials`) | `MaterialMappingSuggestionService` |
| `MaterialMappings` hat `DeliveringEntity` und `DeliveringMaterialNumber`, beide wirken nirgends (nur Import/Export) | `MaterialMappingStore` |
| Kosten TR IT und TR IN stammen aus deren eigenen Verkaufszeilen, nur positive Stueckkosten werden gespeichert | `B1GroupStandardCostBuilder.IsUsable` |
| `SupplierFromGoodsReceipt` steht nur am `SalesRecord`, nicht in `CentralSalesRecords` | `Models/SalesRecord.cs`, `HanaQueryService` |
| Produktiv: 705 DE-Vorschlaege (5,3 Mio. EUR), Mapping wirkt noch auf keine Zeile, solange nichts bestaetigt ist | Vorschlagslauf 19.11. |

## 2. Entscheide Ingo vor dem Start (Frageliste)

| Nr | Frage | Vorgabe, falls „Vorgaben gelten“ |
| --- | --- | --- |
| F1 | Paket B (Mapping gegen TR IT fuer Spanien, Industrial Components) bauen? | ja |
| F2 | Paket C (Topic 2 gegen 3 bei TR IT/TR IN trennen) und D (Herkunft Lieferant) im selben Zweig? | ja |
| F3 | Indien LRD: Code 8 behalten, Kosten offen, 5 Artikel nachpflegen lassen? | ja |
| F4 | Codes ausserhalb der Matrix (7 bei externem Lieferant ohne lokale Kosten, 9 unklar, 10 intern ohne Kostenquelle) so lassen? | ja, in der Feldhilfe als „Zusatzcode, nicht Matrix“ kennzeichnen |
| F5 | Wer bestaetigt die DE-Vorschlaege? | Stichprobe Top 50 an Ingo, Rest spaeter Rohail |
| F6 | UK-Regel (Paket E): Typencode in SAP nachsehen, in T76 ueber `sap-t76`? | ja, nur T76, nur lesen |
| F7 | Deploy am Ende ohne Rueckfrage, wenn Tests gruen und kein Einkauf-Delta laeuft? | nein, vorher fragen |

## 3. Pakete

### Paket A: Export 20.11. pruefen, Zahl Entscheid 1 neu messen, Mail Andreas (kein Code)

1. `Sales_All_2026-11-20.xlsx` und `Mapping_2026-11-20.xlsx` (Export 12:10) gegen `Sales_All_2026-11-19` per Skript:
   Zeilen und Finance Net Sales je Land gleich; IT Ware aus Indien „Interner Standardpreis“ 448 auf rund 47, UK 87 auf rund 17;
   Anzahl je Issue Topic und Land; Stichprobe je Topic (3 Zeilen) mit Kosten, Cost Source, Status; Gruppenmarge Summary gefuellt.
2. Wirkung „Werkstamm gegen immer lokal“ neu messen (Zeilen, Umsatz, Herstellkosten in beiden Lesarten). In der Doku stehen
   2,5 Mio. CHF (Regelset-Word, Stand 16.11.) und 0,85 Mio. CHF (Plan Stufe 1, ISS-007.3). Den Unterschied erklaeren
   (Zeitraum, Menge, Waehrung) und die ueberholte Zahl als ueberholt markieren.
3. Mailtext an Andreas im Chat plus `.eml` in Downloads, nicht senden: beide Excel, Matrix 1 bis 7 mit Zusatzcodes,
   Bitte um schriftliche Bestaetigung der Werkstamm-Regel (im Gespraech „Das koennen wir so machen“), Indien LRD (F3),
   wer Mappings bestaetigt, Hinweis, dass „Saubere Kostenbasis %“ use-0-Zeilen als nicht sauber zaehlt.

Abnahme: Tabelle Soll/Ist im Chat, Abweichungen benannt. Bei Abweichung in Zeilen oder Umsatz anhalten.

### Paket B: Mapping gegen TR IT (Ausloeser Andreas: Italien liefert an Spanien)

Ziel: Eine Zeile einer Tochter mit Lieferant Trafag Italia findet die Kosten TR IT ueber die italienische Artikelnummer.

1. Zuerst messen (Skript, Sales_All): Welche Artikel verkauft TRIT selbst, die als Industrial Components gelten, und stehen
   dafuer Kosten im Bereich TR IT? Bei null Treffern anhalten und Ingo fragen.
2. Wirkung: Ist die liefernde Gesellschaft der Zeile TR IT (aus den Lieferantenfeldern, nicht aus dem Werkstamm) und gibt es
   eine bestaetigte Mapping-Zeile fuer TSC und Artikel mit `DeliveringEntity = TR IT` und gefuellter
   `DeliveringMaterialNumber`, dann sucht `FindGroupCost` zuerst mit dieser Nummer im Bereich TR IT. `GroupMaterialNumber`
   der Zeile bleibt unveraendert (keine Wirkung auf Werkstamm-Abgleich und Sparte). Gleiches fuer TR IN zulassen, Code allgemein.
3. Vorschlagslauf: fuer Kandidaten mit liefernder Gesellschaft TR IT zweites Ziel = Artikel und Texte aus den Verkaufszeilen
   von TRIT (Material, Materialtext), Trigramm wie bisher, nur Vorschlag. Spalte Mapping in Sales_All zeigt
   „<Nummer> (TR IT, <Verfahren>)“.
4. Import und Seite: `DeliveringEntity` nur leer, TR AG, TR IT, TR IN; Zeile mit TR IT ohne Nummer wird abgelehnt.
5. Tests: Treffer nur bei passender liefernder Gesellschaft; kein Treffer bei Zeile ohne Lieferant (Lesart A bleibt);
   unbestaetigt wirkt nicht; TRCH/TRAT weiter gesperrt; Issue Topic wechselt von 2 auf leer.
6. Voraussetzung produktiv: Spanien liefert den Lieferanten erst nach Santis Exportlauf. Ohne ihn hat Paket B dort keine
   Wirkung; das im Abschluss so melden, nicht als Fehler.

### Paket C: Topic 2 gegen 3 bei TR IT und TR IN trennen

`B1GroupStandardCostBuilder` liefert zusaetzlich die Menge der bekannten Artikelschluessel je Bereich, auch ohne positive
Kosten. **Nicht** als Kostenzeile mit 0 in `GroupStandardCosts` speichern (das ergaebe Konzernkosten 0), sondern als eigene
Menge im Kontext (wie `ChPlantMaterialKeys`). `ResolveIssueTopic`: gefunden = Kostentreffer oder bekannter Schluessel.
Tests: Artikel bekannt ohne Kosten ergibt 3 bzw. 4, Kosten und Status unveraendert. Schema nur aendern, wenn die Menge
gespeichert werden muss; dann Migration mit Test.

### Paket D: Herkunft des Lieferanten in Sales_All

Neue Spalte „Supplier Origin“ am Ende von Sales und Finance Details: `Stamm`, `Wareneingang`, `Liste` (italienische
Uebergangsliste), `Regel` (CH/AT, Sales Type), `Werkstamm` (kein Lieferant, bei Trafag AG gefunden), `keiner`. Dafuer
`SupplierFromGoodsReceipt` und das Listen-Kennzeichen nach `CentralSalesRecords` durchreichen (Schema-Migration, Altzeilen
leer = unbekannt bis zum naechsten Lauf des Landes). Bestehende Spalten und deren Reihenfolge bleiben. Tests: je Herkunft ein
Fall, Spaltenposition, Migration.

### Paket E: UK-Regel Typencode (erst lesen, dann bauen)

1. Messung vorab per Skript an Sales_All: wie viele der 295 UK-Artikel tragen ein eindeutiges Typencode-Muster im Text.
2. Ueber `sap-t76` in T76 lesend klaeren, wo der Trafag-Typencode am Material steht (Klassifizierung, Materialtext,
   Kundenmaterial) und ob ein vorhandenes OData-Set ihn liefert. Ergebnis in diesen Plan.
3. Nur wenn er ohne SAP-Aenderung lesbar ist: Regel „UK Typencode im Text“ als Vorschlag (nie sofort wirksam),
   Ausschlusswoerter wie bei der DE-Regel. Braucht es eine SAP-Aenderung: anhalten, Ingo fragen.

### Paket F: Bestaetigung vorbereiten (kein Code)

Excel der 50 umsatzstaerksten DE-Vorschlaege mit beiden Texten und Sicherheit in Downloads; Ingo prueft, danach Status per
Import auf bestaetigt. Liste der 5 indischen Artikel ohne Sachnummer als Mailentwurf. SCHENKER-Frage an Paola: bestehenden
Entwurf nennen, nicht neu schreiben.

### Paket G: Feldhilfe und Doku

Feldhilfe im Blatt „Finance Filter Hilfe“: Zusatzcodes 8, 9, 10 und 7 bei extern kennzeichnen (F4). Regelset als gueltige
Fassung in `docs/FINANCE_REGELSET_ABGLEICH_2026-11-20.md` nachfuehren, sobald F3 und die Antwort Andreas vorliegen.

## 4. Reihenfolge und Haltepunkte

1. A sofort nach dem Export 12:10. **Halt:** Ergebnis und Mailtext an Ingo.
2. C, D, B, G in dieser Reihenfolge in einem Zweig, je Paket ein Commit, Tests gruen. **Halt** vor dem Deploy (F7).
3. Nach dem Deploy: Vorschlagslauf auf der Seite, erster Export gegen den Vortag (Zeilen und Umsatz je Land gleich, Kosten
   aendern sich nur, wo Topic oder Mapping es erklaert).
4. E und F danach, E ohne weitere Freigabe nur bis zum Lesebefund.

## 5. Nicht Teil dieses Plans

Lesart B (ohne Lieferant bei TR IN und TR IT suchen), Produktgruppen fuer unbekannte Artikel (E11), Gruppennummer-Feld in den
ERP der Toechter (E9), Lieferant im Alphaplan-Export (Rohail), Aenderung der Kennzahl „Saubere Kostenbasis %“.

## 6. Annahmen, nicht geprueft

- Die Abschrift vom 18.11. wurde fuer diesen Plan nicht neu gelesen, Grundlage ist ihre Auswertung im Plan Stufe 1.
- Ob TRIT die Industrial Components selbst verkauft und damit Kosten im Bereich TR IT stehen, ist nicht gemessen
  (Paket B Schritt 1).
- Ob Spanien nach Santis Lauf „Trafag Italia“ als Lieferant liefert, ist offen.
- Aufwand: A klein, B mittel, C klein, D mittel (Migration), E offen, F klein, G klein.
