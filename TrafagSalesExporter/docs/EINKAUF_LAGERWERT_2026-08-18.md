# Lagerwert der Einkaufsteile im Einkauf-Cockpit

Stand: 2026-08-18, Nachtrag Verlauf je Woche 2026-09-28 (Abschnitt 13). Zurueck: `docs/router/einkauf.md`.

## 0. Stand auf einen Blick

| | |
| --- | --- |
| **Auftrag** | Armin will den Lagerwert der Einkaufsteile als KPI-Kachel, „per «bis Monat»", Werte wie MB5L, abgegrenzt auf die Disponenten `001`–`005`. |
| **Umgesetzt?** | **Teilweise.** Die KPI-Kachel fuer den **aktuellen** Lagerwert ist seit 2026-08-19 produktiv und seit 2026-08-24 in der Datenbank gespeichert; am 2026-08-24 zeigte sie CHF 9'205'959 aus einem echten SAP-Read (Abschnitt 12.7). *Ueberholt ist die fruehere Aussage „zeigt wartet auf Einkauf-Lauf, noch nie gegen echtes SAP gelaufen".* Der Stichtag „per bis Monat" fehlt weiterhin, siehe Abschnitt 10. |
| **Verlauf je Woche** | Seit 2026-09-28 **gebaut, nicht deployed** (Abschnitt 13, Weg A): jeder Lauf schreibt einen Tagesstand, die Uebersicht zeigt Stand Ende Kalenderwoche als Grafik und Liste. Beginnt ab Deploy, kein Rueckblick. |
| **Die Zahl** | Einkaufsteile heute: **CHF 8'982'938.78** ueber 7'261 Materialien, das sind 82 % des gesamten Lagerwerts von CHF 10'937'376.40. |
| **Machbar?** | Ja. Alle fuenf Disponenten existieren, `MBEWH` reicht bis 2000 zurueck, die Stichtagsrechnung ist gebaut und in sich geprueft. |
| **Groesster offener Punkt** | Der MB5L-Abgleich. Ohne ihn ist die Zahl nicht freigegeben. |
| **Groesste technische Huerde** | `MBEWH` ist mit 5,4 Mio Zeilen nicht ueber OData ladbar. Es braucht ein serverseitig aggregierendes SAP-Set. |

**Hier geht es weiter:** Abschnitt 13.4 (Deploy des Verlaufs), danach Abschnitt 7 (MB5L-Abgleich).

## 1. Die Anforderung

Armin am 2026-08-18 per Mail an Ingo:

> wie heute vor der Pause kurz angesprochen, waere der Lagerwert von den Einkaufsteilen eine
> interessante Groesse

Die beigelegte Grafik zeigt mit einem Pfeil auf die KPI-Zeile des Einkauf-Cockpits (neben
`Spend total`, `Bestellungen im Zeitraum`, `Verpflichtungen`, `Lieferantenperformance`) und
nennt zwei Vorgaben:

- **„Lagerwert per «bis Monat» (Werte dito Transaktion MB5L)"**
- **„Abgrenzung: Einkaufsteile, d.h. Disponenten 001, 002, 003, 004, 005"**

Damit ist es keine vage Idee, sondern eine Anforderung mit Stichtagsbezug, Referenztransaktion
und Mengenabgrenzung.

## 2. Ist-Stand

| Frage | Antwort |
| --- | --- |
| Lagerwert im Einkauf-Cockpit? | **Nein.** Keine Kachel, keine Query, kein Feld. |
| Wird `MBEW` schon gelesen? | Ja, aber **nur als Stueckpreis** (`STPRS`) fuer die Gruppenmarge (`Services/SapGatewayStandardCostReader.cs`). Bestandsmenge `LBKUM` und Bestandswert `SALK3` werden nirgends verwendet. |
| Bestandswerte woanders? | Im Logistik-Stuecklisten-Dashboard gibt es Bestands**mengen**. Eine CHF-Bewertung wurde dort **bewusst weggelassen**, weil eine mehrfach verwendete Komponente ueber Stuecklistenbeziehungen doppelt gezaehlt wuerde (`docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md`). Andere Fragestellung, kein Gegenargument. |

## 3. Die Messung (T76/100, Bewertungskreis 1100)

Gelaufen am 2026-08-18 mit `docs/abap/Z_PURCHASING_LAGERWERT_ANALYSE.abap`.

### 3.1 Armins Abgrenzung ist stimmig

Alle fuenf Disponenten existieren, und ihre SAP-Bezeichnungen bestaetigen die Zuordnung zum
Einkauf:

| Disponent | Bezeichnung | Materialien | Lagerwert CHF |
| --- | --- | --- | --- |
| `001` | rot / Einkauf | 2'810 | 4'041'124.80 |
| `002` | gun / Einkauf | 30 | 302'246.37 |
| `003` | mso / Einkauf | 3'724 | 4'301'097.56 |
| `004` | Betriebsmat/Einkau | 686 | 322'931.33 |
| `005` | wid / Einkauf | 11 | 15'538.72 |
| **Summe** | | **7'261** | **8'982'938.78** |

Zum Vergleich der gesamte Bewertungskreis: 65'498 Materialien, **CHF 10'937'376.40**. Die
Einkaufsteile sind damit rund **82 %** des Lagerwerts.

### 3.2 Weitere Messwerte

- 65'549 `MARC`-Saetze im Werk 1100, 89 Disponenten insgesamt. Die grosse Restmenge liegt in
  Produktion und Sensorik (`A99 PPD / Status 99` allein 26'750 Materialien), 2'172 Materialien
  haben keinen Disponenten.
- **Kein bewertetes Material ohne `MARC`-Satz** (0 Zeilen, 0 CHF) — der Join ist lueckenlos.
- Preissteuerung: 65'493 mal `S`, **5 mal `V`** (gleitender Durchschnitt).
- `MBEWH` (Bewertungshistorie) vorhanden: **5'371'798 Saetze**, zurueck bis Periode 2000/12.

### 3.3 Stichtagsrechnung, zwei Laeufe

| | Stichtag 2026/06 | Stichtag 2024/06 |
| --- | --- | --- |
| **Lagerwert (korrekt)** | **CHF 8'973'694.30** | **CHF 10'648'966.26** |
| Menge | 14'215'807.724 | 15'480'608.268 |
| aus `MBEWH` (Historie) | 687 (9 %) | 6'893 (95 %) |
| aus `MBEW` (Fallback) | 6'574 (91 %) | 368 (5 %) |
| naive Summe nur ueber `MBEWH` | CHF 3'070'209.07 | CHF 10'640'616.98 |
| Abweichung der naiven Methode | **−66 %** | **−0,08 %** |

Beide Rechenpfade sind damit geprueft, und der Bestandsverlauf ist stimmig: von 10,65 Mio Mitte
2024 auf 8,98 Mio heute.

**Vorbehalt:** `T76` ist eine mehrere Monate alte Kopie (Hinweis Ingo), in 2026 bewegt sich dort
kaum etwas. Auf Produktiv ist die Verteilung anders. Die Laeufe zeigen zudem nur **innere
Stimmigkeit, nicht Korrektheit** — das beweist erst MB5L.

## 4. Die drei Fallstricke

### 4.1 `MBEWH` darf nicht einfach summiert werden

`MBEW` fuehrt nur den **aktuellen** Bestand. Fuer einen Stichtag braucht man `MBEWH` — aber
**`MBEWH` enthaelt fuer eine Periode nur die Materialien, bei denen es danach eine
bewertungsrelevante Bewegung gab.** Ein seit Maerz unbewegtes Material hat fuer 2026/04 keinen
Historiensatz; sein Wert steht unveraendert in `MBEW`.

Die erste Fassung des Reports summierte naiv und lieferte fuer 2026 sichtbar Unsinn: 2026/02
noch 8'355'679 CHF, 2026/04 nur 383'904 CHF.

**Korrekte Logik, wie MB5L sie anwendet:** je Material den `MBEWH`-Satz der **kleinsten Periode
>= Stichtag** nehmen; existiert keiner, gilt der aktuelle `MBEW`-Wert. So rechnet der Report
jetzt.

### 4.2 Die falsche Methode faellt bei alten Stichtagen nicht auf

Das ist der gefaehrlichere Punkt. Die naive Summe weicht ab

- bei `2024/06` um **0,08 %** — voellig unauffaellig,
- bei `2026/06` um **66 %** — offensichtlich kaputt.

Je aelter die Periode, desto dichter die Historie. **Wer die Umsetzung an einem
zurueckliegenden Stichtag testet, sieht den Fehler nicht** und baut ihn produktiv ein, wo er
ausgerechnet den aktuellen Monat trifft — den die Kachel am haeufigsten zeigt.

**Regel fuer die Abnahme: immer mit einem jungen Stichtag pruefen.**

### 4.3 `MBEWH` ist nicht ladbar wie `MARA` oder `MAKT`

Der bisherige Weg fuer Stammdaten ist „ein ungepagter Request, alles in den Cache" — so laufen
`MARA001Set`, `MAKTSet` und `mbewSet`. Fuer `MBEWH` scheitert das:

- `mbewSet` kostet mit 68'543 Zeilen bereits **124 MB und 28 Sekunden** je Aufruf (gemessen
  2026-07-28, dokumentiert in `Services/SapGatewayStandardCostReader.cs`).
- `MBEWH` hat im Bewertungskreis 1100 **5'371'798 Zeilen**, rund das **78-fache**. Allein fuer
  die 7'261 Zielmaterialien sind es 1'132'402 Saetze.

Ein Full Load ist damit ausgeschlossen. Richtig ist ein **serverseitig aggregierendes
EntitySet**: SAP rechnet den Wert je Bewertungskreis, Periode und Disponentengruppe und liefert
wenige Zeilen. Die Rueckrechnung aus 4.1 gehoert dann ebenfalls nach ABAP, neben die Daten.
Muster: `docs/abap/ZSTR_MAT_XYZ_GET_ENTITYSET.abap`.

## 5. MB5L als Referenz — was sie kann und was nicht

`MB5L` laeuft auf Programm **`RM07MBST`** (aus `TSTC` ermittelt, nicht angenommen), 16
Selektionsfelder.

### 5.1 Belegt: MB5L kennt keinen freien Stichtag

Die Saldoschalter heissen `AKSALDO`, `VMSALDO`, `VJSALDO` — aktueller Saldo, Vormonat, Vorjahr.
**Damit ist aus dem Selektionsbild bewiesen, dass MB5L genau diese drei Zeitpunkte kennt.**

Fuer Armins Wunsch heisst das eine Praezisierung, keinen Widerspruch: „Werte dito MB5L" ist als
**Bewertungslogik** uebernehmbar (`SALK3`, aktueller Saldo), „per «bis Monat»" geht darueber
hinaus. Referenz fuer beliebige Stichtage waere `MB5B` (Bestand zum Buchungsdatum). Die Kachel
kann also mehr als MB5L; MB5L taugt zur Pruefung des **aktuellen** Stands.

### 5.2 Welche Feldeinstellungen das Ergebnis verfaelschen

| Feld | Wirkung auf den Summenvergleich |
| --- | --- |
| `MATLINES` | **kritisch, live bestaetigt 2026-08-19**: Text lautet „Materialeinzelzeilen anzeigen". Ohne dieses Kennzeichen liefert MB5L die FI-Kontenabgleichsansicht (Felder `BUPER`/`BUKRS`/`BUTXT`/`KONTS`/`TXT50`), keine Materialzeile und kein `SALK3` — ein Vergleich ist dann unmoeglich. Die fruehere Annahme „begrenzt die Materialzeilen" war falsch. |
| `KEINZEL` | Text lautet „nur Bewertungskreisebene", **nicht** „unterdrueckt Einzelposten" wie zuvor angenommen. Wirkung auf den Summenvergleich noch ungeklaert, wird nach dem ersten Lauf mit `MATLINES = X` beurteilt. |
| `SUMMEN` | **hoch** — Summenzeilen zusaetzlich in der Ausgabe; wer alle Zeilen addiert, zaehlt doppelt. |
| `NEGATIV` | **hoch** — schraenkt auf negative Bestaende ein. |
| `VMSALDO` / `VJSALDO` | **hoch** — zusaetzliche Wertspalte Vormonat bzw. Vorjahr. |
| `BWTAR` | **besonders** — bei getrennter Bewertung fuehrt SAP je Material einen Kopfsatz UND Teilsaetze; beide summiert ergibt den doppelten Wert. |
| `AKSALDO` | **muss an sein**, sonst wird ein anderer Zeitpunkt verglichen als `MBEW` fuehrt. |
| `BUKRS`, `BKLAS`, `MATNR`, `SKONT` | mittel — schraenken die Materialmenge ein. |
| `NULLB` | bewusst an, damit ein Material mit Wert ohne Menge nicht fehlt. |

Der Abgleichsreport setzt deshalb gezielt `BWKEY`, `AKSALDO = X`, `NULLB = X` und leert
`VMSALDO`/`VJSALDO`. Alles andere bleibt auf der Vorbelegung des Programms.

**NACHTRAG 2026-08-19, live beobachtet:** Beim `SUBMIT` auf `RM07MBST` mit `BWKEY`
eingegrenzt erscheint SAP-Meldung **M7375**: „Eingrenzungen dieses Feldes führen zu
fehlerhaften Ergebnissen." Diagnosetext: Die Salden auf den Bestandskonten liegen auf
**Buchungskreisebene**; eine Eingrenzung darunter — genannt werden Material,
Bewertungskreis, Bewertungsart, Bewertungsklasse und Negative Bestände — liefert korrekte
Materialdetails, aber der Abgleich gegen das **Bestandskonto der Finanzbuchhaltung** kann
dabei falsch werden. Das betrifft die Ruecksteuerung gegen FI, nicht unseren Vergleich: wir
stellen die eigene `MBEW`-Summe gegen die `MB5L`-Summe, beide mit derselben
`BWKEY`-Eingrenzung, kein FI-Bestandskonto im Spiel. Für unseren Zweck ist die Meldung
deshalb keine Fehlerquelle, nur bestaetigen und weiterlaufen lassen. Neu entdecktes,
bisher nicht dokumentiertes Feld auf dem MB5L-Selektionsbild: **„nur
Bewertungskreisebene"** (Checkbox, im beobachteten Lauf nicht angehakt) — Zusammenhang mit
`BWTAR`/getrennter Bewertung noch ungeklaert, vor der naechsten Fachfrage nicht selbst
anhaken.

### 5.3 Welche Materialien eine Differenz erzeugen koennen

Der Report misst beides, damit man bei einer Abweichung nicht raet:

1. **Getrennte Bewertung:** `MBEW`-Saetze mit und ohne `BWTAR`. Gibt es Teilsaetze, ist die
   eigene Summe moeglicherweise zu hoch und muss auf `BWTAR = leer` beschraenkt werden.
2. **Sonderbestaende:** `MSKA` (Kundenauftrag), `MSPR` (Projekt), `MSLB` (beim Lieferanten).
   `MBEW` fuehrt diese nicht; zeigt MB5L sie mit, liegt MB5L hoeher.

Zusaetzlich vergleicht der Report **die Zeilenzahl von MB5L mit der `MBEW`-Menge, bevor er
summiert**. Mehr Zeilen deuten auf Summenzeilen oder Teilsaetze, weniger auf eine zusaetzliche
Einschraenkung.

## 6. Was schon anderswo dokumentiert war

| Punkt | Stand | Quelle |
| --- | --- | --- |
| **Bewertungskreis** | **geklaert**: `1100` = CH, `1200` = AT, per `T001K` bestaetigt. Nicht ueber beide summieren (Hauswaehrung). | `docs/FINANCE_STANDARDKOSTEN.md` |
| **`mbewSet`** | im Einsatz, mit `$filter=Bwkey`, Kosten gemessen (124 MB / 28 s) | `Services/SapGatewayStandardCostReader.cs`, `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md` |
| **`PEINH`-Falle** | dokumentiert als Warnung fuer `STPRS`. Entfaellt beim Lagerwert, weil `SALK3` ein absoluter Wert ohne Preiseinheit ist — ein weiteres Argument gegen `LBKUM * STPRS`. | ebenda |
| **`MBEWH`** | nirgends erwaehnt | — |
| **Sonderbestaende** | nirgends erwaehnt | — |
| **Disponent `004`** | nicht dokumentiert; SAP-Text ist `Betriebsmat/Einkau` | — |

## 6a. Live-Lauf 2026-08-19: Ursache fuer den fehlgeschlagenen MB5L-Vergleich gefunden

Erster Lauf von `Z_PURCHASING_MB5L_ABGLEICH` mit `p_run = X` auf T76/100 lieferte nur
**2 Zeilen statt der erwarteten rund 65'498** und keine Materialdaten:

- Ergebnisfelder waren `BUPER`, `BUKRS`, `BUTXT`, `KONTS`, `TXT50` — das ist MB5L's
  **FI-Kontenabgleichsansicht** (Saldo je Sachkonto), nicht die Materialliste.
- Ursache: das Kennzeichen `MATLINES` („Materialeinzelzeilen anzeigen") wurde vom Report
  nicht gesetzt. Ohne dieses Kennzeichen zeigt MB5L standardmaessig die Kontenansicht.
  Die fruehere Einschaetzung, `MATLINES` wuerde nur die Zeilenzahl begrenzen, war falsch.
  **Behoben**: der Report setzt jetzt zusaetzlich `MATLINES = X`.
- Nebenbefund beim manuellen Test der MB5L-Maske direkt (ausserhalb des Reports): Meldung
  **M7375** „Eingrenzungen dieses Feldes fuehren zu fehlerhaften Ergebnissen", wenn
  `Bewertungskreis` eingeschraenkt wird. Diagnosetext: Bestandskonten-Saldi liegen auf
  **Buchungskreisebene**; Eingrenzungen darunter (Material, Bewertungskreis, Bewertungsart,
  Bewertungsklasse, Negative Bestaende) verfaelschen den Abgleich gegen das **FI-Bestandskonto**.
  Das betrifft unseren Vergleich NICHT, weil wir gegen die eigene `MBEW`-Summe abgleichen,
  nicht gegen ein FI-Konto. Reine Warnung, kein Blocker fuer diesen Report.
- Die eigene `MBEW`-Rechnung im selben Lauf zeigt den Stand von heute: **CHF 10'947'696.40**
  gesamt (65'498 Materialien), **CHF 8'993'258.78** fuer die Disponenten 001-005 (7'261
  Materialien) — rund CHF 10'320 hoeher als der Messwert vom 18.08., normale Tagesbewegung.
  Keine getrennte Bewertung (`BWTAR`) im Bestand. Sonderbestaende: `MSKA` **320'848** Saetze,
  `MSLB` 760, `MSPR` 0 — die hohe `MSKA`-Zeilenzahl ist neu und noch nicht bewertet, weil der
  Report nur Saetze zaehlt, nicht deren Wert.

### 6a.1 Zweiter Lauf: `MATLINES = X` hat nichts geaendert

Der Lauf um 09:14 mit gesetztem `MATLINES = X` lieferte **exakt dasselbe Ergebnis**: 2 Zeilen,
Felder `BUPER`/`BUKRS`/`BUTXT`/`KONTS`/`TXT50`, kein `SALK3`. Damit ist die Erklaerung aus
6a **widerlegt**. `MATLINES` steuert die Bildschirmausgabe, aber nicht, welche Tabelle
`cl_salv_bs_runtime_info` abgreift.

### 6a.1a Ursache am Quelltext BELEGT (Lauf 09:22)

Die Quelltextsuche ueber alle sechs Includes von `RM07MBST` liefert den Beweis:

| Fundstelle | Aufruf |
| --- | --- |
| **`RM07MBST` Zeile 2956** | **`CALL FUNCTION 'REUSE_ALV_HIERSEQ_LIST_DISPLAY'`** |
| `RM07MBST` Zeile 3029 | `CALL FUNCTION 'REUSE_ALV_LIST_DISPLAY'` |
| `RM07ALVI` Zeilen 81/83/103 | `alv_detail_func = 'REUSE_ALV_GRID_DISPLAY'` bzw. `…LIST_DISPLAY` |

`RM07MBST` gibt eine **hierarchisch-sequenzielle Liste** aus. `cl_salv_bs_runtime_info`
greift davon nur die **Kopftabelle** ab, nie die Positionen. Der automatische Abgriff der
Materialebene ist damit **ausgeschlossen**, nicht nur unwahrscheinlich.

Bestaetigt wird das durch den Inhalt der zwei Zeilen: sie tragen `BUKRS 1100` („Trafag AG")
und `BUKRS 1200` („Trafag Ges.m.b.H."), `KONTS` und `TXT50` sind **leer**. Wir sehen also die
oberste Hierarchieebene (Buchungskreis) — auf der die gesetzte `BWKEY`-Selektion noch gar
nicht greift, weshalb auch der oesterreichische Buchungskreis mit erscheint.

### 6a.2 Konsequenz: der Abgleich wird von Hand gemacht

**Der MB5L-Abgleich ist eine einmalige Validierung, keine laufende Funktion.** Die spaetere
KPI-Kachel rechnet aus `MBEW`/`MBEWH` und ruft MB5L nie auf. Weitere Runden, um den
ALV-Abgriff zu automatisieren, zahlen deshalb auf nichts ein — der Aufwand steht nicht im
Verhaeltnis zum einmaligen Nutzen.

Der Report gibt jetzt in Abschnitt 6 eine vollstaendige Anleitung fuer den manuellen Lauf
aus, inklusive der eigenen Vergleichszahl. Zusaetzlich liest er in Abschnitt 3c den
**Buchungskreis** zum Bewertungskreis aus `T001K`, weil Meldung M7375 genau auf diese Ebene
zielt, und gibt die zwei gefundenen **Bestandskonten** (`KONTS`) im Klartext aus.

### 6a.3 Die Selektionsmaske blockiert beim Bewertungskreis — Loesung ueber den Buchungskreis

Ingo meldet am 2026-08-19: In der MB5L-Maske laesst sich der **Bewertungskreis nicht setzen
und ausfuehren**, Meldung M7375 blockiert.

Die Loesung steht im Text von M7375 selbst: Eingrenzungen **auf** Buchungskreisebene sind
erlaubt, nur **darunter** nicht. Und `T001K` liefert die Bruecke:

| Bewertungskreis | Buchungskreis |
| --- | --- |
| `1100` | `1100` (Trafag AG) |

**Also in MB5L den Buchungskreis `1100` eingrenzen statt des Bewertungskreises.** Damit
entfaellt die Meldung.

Eine Bedingung muss dafuer erfuellt sein, sonst vergleicht man verschiedene Materialmengen:
am Buchungskreis `1100` darf **genau ein** Bewertungskreis haengen. Der Report prueft das
jetzt in Abschnitt 3c in der Gegenrichtung (`SELECT bwkey FROM t001k WHERE bukrs = …`) und
meldet entweder `GLEICHWERTIG` oder rechnet die Vergleichssumme ueber alle betroffenen
Bewertungskreise neu. **Diese Pruefung steht noch aus.**

**Naechster Schritt:** Report einmal laufen lassen (Abschnitt 3c zeigt jetzt die
Gegenrichtung), danach MB5L von Hand mit **Buchungskreis 1100** gegen die eigene Summe
stellen.

## 7. Naechste Schritte

1. **Abgleichsreport Stufe 1** erneut laufen lassen (`p_run` leer). Er zeigt jetzt zusaetzlich
   die Selektionstexte und misst getrennte Bewertung sowie Sonderbestaende. **Das kann die
   Vergleichszahl schon vor dem MB5L-Lauf korrigieren**, falls es `BWTAR`-Teilsaetze gibt.
2. **Abgleichsreport Stufe 2** (`p_run = X`): MB5L laeuft und wird gegen die eigene Summe
   gestellt. Erwartet wird **CHF 10'937'376.40** fuer den gesamten Bewertungskreis.
3. **Mit Armin klaeren:**
   - Gehoert Disponent `004` (Betriebsmaterial) in den Lagerwert?
   - Sollen Sonderbestaende mitzaehlen?
   - Ist „per «bis Monat»" wirklich fuer jeden Monat gewuenscht, oder genuegen aktueller Stand
     und Vormonat? Das entscheidet ueber den ganzen `MBEWH`-Aufwand aus 4.3.
4. **Erst danach umsetzen**, in dieser Reihenfolge: SAP-seitiges aggregierendes EntitySet ->
   Cache -> Kachel. Nach Marcos Leitplanke „ein Punkt nach dem anderen" und erst nach Abnahme des
   laufenden Spend-Themas.

**Bei der Abnahme:** mit einem **jungen** Stichtag pruefen, nie nur mit einem alten (Grund in
4.2).

## 10. Umsetzung vom 2026-08-19

Gebaut wurde die Kachel fuer den **aktuellen** Lagerwert. Der Stichtag bleibt bewusst aussen
vor, solange Armins Antwort auf Frage 3 (braucht es wirklich jeden Monat?) fehlt — davon
haengt der ganze `MBEWH`-Aufwand ab.

### 10.1 Was gebaut wurde

| Datei | Rolle |
| --- | --- |
| `Services/SapGatewayStockValueReader.cs` (neu) | Liest `mbewSet` (`Salk3`, `Lbkum`) und `MARCSet` (`Dispo`), aggregiert je Disponent, haelt den Stand im Speicher |
| `Services/IPurchasingDashboardService.cs` | Additive Felder `StockValue*` plus Record `PurchasingStockValueRow` |
| `Services/PurchasingDashboardService.cs` | Konstanten `PurchasingValuationArea` / `PurchasingPlanners`, Befuellung aus dem Cache |
| `Services/PurchasingDataRefreshService.cs` | `RefreshStockValueSafeAsync`, aufgerufen nach dem Commit in Full **und** Delta |
| `Components/Pages/PurchasingDashboard.razor` | Fuenfte KPI-Kachel „Lagerwert Einkaufsteile" |
| `Services/PurchasingUiTextGeneratedTranslations.cs` | Zwei neue Texte in sechs Sprachen |
| `Program.cs` | Eine `AddSingleton`-Zeile |
| `TrafagSalesExporter.Tests/StockValueReaderTests.cs` (neu) | 9 Tests der Rechenlogik |

### 10.2 Drei Entwurfsentscheidungen, die den Fallstricken aus Abschnitt 4 folgen

1. **`SALK3` statt `LBKUM * STPRS`.** `SALK3` ist ein absoluter Betrag ohne Preiseinheit. Die
   Multiplikation liefe in die `PEINH`-Falle und laege bei `PEINH = 100` um Faktor 100 daneben
   — derselbe Fehler wie einst bei der Gruppenmarge.
2. **Nicht am Seitenaufruf.** Ein Read kostet ueber 100 MB und rund 28 Sekunden. Die Kachel
   liest deshalb **nur aus dem Cache**; gefuellt wird er vom Einkauf-Lauf. Liegt nichts vor,
   zeigt die Kachel „wartet auf Einkauf-Lauf" statt einer `0`, die wie ein leeres Lager
   aussaehe.
3. **Je Disponent, nicht vorsummiert.** SAP liefert die Aufschluesselung, die Anwendung
   summiert. Armins offene Frage zu Disponent `004` ist damit eine Zeilenaenderung in
   `PurchasingPlanners` — ohne SAP-Aenderung und ohne Transport.

Ausfallsicherheit: `RefreshStockValueSafeAsync` wirft nicht. Begruendung ist der Vorfall vom
2026-07-02, als ein `404` auf `MARA001Set` den Full Load abbrach und der Datenstand bis zum
2026-07-17 einfror.

### 10.3 NICHT geprueft — vor dem Deploy zwingend

| Punkt | Warum offen |
| --- | --- |
| **Liefert `MARCSet` das Feld `Dispo`?** | Der bestehende Loader liest `MARCSet` nur mit `$select=Matnr,Werks,Maabc`. Ob `Dispo` exponiert ist, wurde **nicht** geprueft und nach Vorrangregel 5 auch nicht angenommen. Fehlt es, wirft der Reader mit klarer Meldung und dem Hinweis auf SEGW. |
| **Liefert `mbewSet` das Feld `Salk3`?** | Der Standardpreis-Reader nutzt nur `Stprs`/`Peinh`. Der neue Reader prueft das Feld zur Laufzeit und wirft mit der Liste der tatsaechlich vorhandenen Felder, statt still `0` zu zeigen. |
| **Stimmt die Zahl?** | Der MB5L-Abgleich steht weiterhin aus (Abschnitt 6a.3). Die Kachel zeigt bis dahin einen **unbestaetigten** Wert. |
| **Lauf gegen echtes SAP** | Der Code ist gebaut und getestet, aber **nie gegen ein SAP-System gelaufen**. |

Der Cache liegt im Speicher und ist nach einem App-Neustart leer, bis der naechste
Einkauf-Lauf durchlaeuft. Eine Persistierung in eine Cache-Tabelle waere der naechste
Schritt; sie wurde bewusst zurueckgestellt, weil parallel am Finance-Dashboard gearbeitet
wird und eine Schemaaenderung dort kollidieren koennte.

### 10.4 Der Stichtag bleibt offen

Fuer „per «bis Monat»" liegt der Entwurf eines serverseitig aggregierenden EntitySets bereit:
`docs/abap/ZSTR_PURCH_STOCKVAL_GET_ENTITYSET.abap`. Er enthaelt die Rueckrechnung aus 4.1
(kleinste Historienperiode >= Stichtag, sonst `MBEW`) und liefert je Bewertungskreis, Periode
und Disponent eine Zeile. Anzulegen sind dafuer eine Struktur in `SE11` und ein EntityType in
`SEGW`; beides ist im Kopf der Datei beschrieben. **Erst starten, wenn Armin bestaetigt, dass
er den Monatsverlauf wirklich braucht.**

## 8. Werkzeuge

| Datei | Zweck |
| --- | --- |
| `docs/abap/Z_PURCHASING_LAGERWERT_ANALYSE.abap` | Disponenten, Lagerwert je Disponent, `VPRSV`-Verteilung, `MBEWH`-Verfuegbarkeit, Stichtagsrechnung mit waehlbarem Jahr/Periode. Read-only. |
| `docs/abap/Z_PURCHASING_MB5L_ABGLEICH.abap` | Ruft MB5L per `SUBMIT` auf und stellt die Summen gegenueber. Zweistufig: ohne `p_run` nur Analyse, mit `p_run` der echte Lauf. Read-only. |

Beide ermitteln Programm- und Feldnamen aus dem System, statt sie anzunehmen.

## 9. Status

Analyse und Dokumentation. **Kein Anwendungscode, keine Datenbank, kein Deploy.** Die Umsetzung
ist bewusst nicht begonnen, weil der MB5L-Abgleich und drei fachliche Entscheidungen ausstehen.

## 11. Nachtrag 2026-08-21: Endlosschleife im Lagerwert-Read, alle Einkauf-Laeufe blockiert

Ingo meldete, die Kachel haenge dauerhaft auf `wartet auf Einkauf-Lauf` und ein manueller
Full Load bewirke nichts. Die Kachel hatte recht: es war seit ihrem Deploy tatsaechlich kein
Einkauf-Lauf mehr fertig geworden.

### 11.1 Messung auf dem Produktivstand

Read-only gegen die produktive `trafag_exporter.db` (nur `SELECT`):

| Lauf | Start | Status |
| --- | --- | --- |
| `Delta` | 2026-08-18 13:15 | **Success** — der letzte erfolgreiche Lauf ueberhaupt |
| `Full` | 2026-08-19 10:10 | `Running`, nie beendet |
| `Delta` | 2026-08-19 12:24 | `Running`, nie beendet |
| `Full` | 2026-08-20 07:34 | `Running`, nie beendet |

In allen drei haengenden Laeufen ist `Lagerwert-Read gestartet` der **letzte** Logeintrag.
Weder `Lagerwert-Read beendet` noch eine Fehlerwarnung folgt. Die Kachel wurde am
2026-08-19 um 10:07 deployed, der erste haengende Lauf startete um 10:10.

### 11.2 Ursache, am eigenen Code belegt

`LoadPlannerMapAsync` paginierte `MARCSet` in einer `while (true)`-Schleife mit Abbruch bei
`page.Count < 1000`. **`MARCSet` ignoriert `$top`, `$skip` und `$filter`** und liefert bei
jeder Anfrage alle rund 68'559 Zeilen. Die Abbruchbedingung wurde damit nie wahr.

Das war keine neue Erkenntnis: das Verhalten ist seit dem 2026-07-23 live verifiziert und an
zwei Stellen dokumentiert, in `PurchasingDataRefreshService` (Grossbuchstaben-Warnung mit
Verifikationsdatum) und in `SapGatewayPlantMaterialReader`. Der Kommentar im Lagerwert-Reader
behauptete das Gegenteil und war in beiden Halbsaetzen falsch. In
`docs/AGENT_COORDINATION.md` stand die Pruefung dieses Punktes ausdruecklich als **vor dem
Deploy zwingend** und wurde nicht durchgefuehrt.

Warum es nie einen Fehler gab: jede einzelne Anfrage war erfolgreich und blieb unter dem
Fuenf-Minuten-Timeout des `HttpClient`. Der Timeout gilt je Anfrage, nicht fuer die Schleife.

### 11.3 Zweiter, dabei gefundener Fehler

Weil `MARCSet` auch `$filter` ignoriert, war die Einschraenkung auf den Bewertungskreis
wirkungslos. Der Reader bekam alle Werke geliefert und schrieb `map[material] = Dispo` ohne
Werkspruefung, also **Last-Wins**: ein Material in mehreren Werken erhielt den Disponenten des
zuletzt gelesenen Werks. Die Abgrenzung auf die Einkaufsdisponenten waere damit still falsch
gewesen, auch wenn die Schleife je gelaufen waere.

### 11.4 Behoben

| Aenderung | Datei |
| --- | --- |
| `MARCSet` mit EINEM ungepagten Request, Schleife entfernt; neue testbare `ParsePlannerMap` mit clientseitigem Werks-Filter | `Services/SapGatewayStockValueReader.cs` |
| Notbremse: liefert ein Set mehr Zeilen als per `$top` angefordert, lauter Abbruch statt stiller Endlosschleife | `Services/PurchasingDataRefreshService.cs` |
| Harte Zeitgrenze von 10 Minuten um den Lagerwert-Read, weil der bestehende `catch` nur Ausnahmen faengt und gegen einen Haenger wirkungslos ist | `Services/PurchasingDataRefreshService.cs` |
| Vier Regressionstests | `TrafagSalesExporter.Tests/StockValueReaderTests.cs` |

`547/547` Tests gruen (vorher 543), `dotnet build -c Release` ohne Fehler.

### 11.5 Was offen bleibt

- **Deploy steht aus.** Ohne ihn wirkt der Fix produktiv nicht.
- Die drei Zombie-Zeilen `Running` in `PurchasingSyncState` raeumt nichts auf. Sie
  blockieren keinen neuen Lauf (es gibt keinen Nebenlaeufigkeitsschutz), zeigen in der
  Oberflaeche aber einen laufenden Stand, den es nicht gibt.
- `MaterialUsageDataRefreshService.ReadAllRowsAsync` hat dieselbe strukturelle Schwaeche
  ohne Notbremse. Dort wird sie nur mit paginierfaehigen Sets aufgerufen, das ist Glueck in
  der Aufrufliste und keine Sicherung im Code.
- Der MB5L-Abgleich aus Abschnitt 6a.3 steht weiterhin aus. Die Kachel zeigt auch nach dem
  Fix einen fachlich **unbestaetigten** Wert.
- Die Schleife hat ueber zwei Tage hinweg stundenlang Volltabellen-Abfragen gegen das
  produktive SAP `travp762` gefeuert. Ob das dort aufgefallen ist, ist nicht geprueft.

## 12. Nachtrag 2026-08-24: Warum die Kachel trotz Fix wieder leer war

Ingo meldete drei Tage nach dem Deploy des Fixes erneut `wartet auf Einkauf-Lauf` und dazu:
"habe schon 2 mal full load angestossen, was ist los?". Die Kachel hatte wieder recht, aber
aus zwei ganz anderen Gruenden.

### 12.1 Messung auf dem Produktivstand

Read-only gegen die produktive `trafag_exporter.db` sowie die Protokolle auf der Freigabe:

| Befund | Beleg |
| --- | --- |
| Letzter Eintrag in `PurchasingSyncState`: Id 34, `Full`, `Running`, gestartet 2026-08-21 08:32 UTC. **Danach nichts.** | `SELECT ... ORDER BY Id DESC` |
| Die Ids 31 bis 34 stehen alle auf `Running` und sind alle abgebrochen | dieselbe Abfrage |
| Letzter Eintrag in `AppEventLogs`: 2026-08-21 13:04 | `SELECT MAX(Timestamp)` |
| Der Fix liegt korrekt auf dem Server | `ParsePlannerMap`, `ignoriert $top` und `Zeitgrenze` in `BiDashboard.dll` vom 2026-08-21 14:13 nachgewiesen |
| Die Anwendung laeuft, `/einkauf` antwortet mit HTTP 200 | Abruf am 2026-08-24 |
| Der Worker ist am 2026-08-24 um 06:39 neu gestartet | `logs/stdout_20260824043919_19860.log` |

Lauf 34 startete um 10:32 Ortszeit, also **vor** dem Ende des Fix-Deploys (Sicherung ab 10:12,
mit der damals noch seitenweisen Kopie deutlich spaeter fertig). Er lief auf dem alten,
fehlerhaften Code und wurde vom `app_offline` des Deploys erschlagen. Er beweist zum Fix
nichts. **Seit dem Fix ist ueberhaupt kein Einkauf-Lauf mehr gestartet worden.**

### 12.2 Erste Ursache: der Lagerwert lag nur im Arbeitsspeicher

`SapGatewayStockValueReader` hielt den Stand in einem privaten `Dictionary`-Feld mit 20 Stunden
Haltbarkeit. Eine Tabelle dafuer gab es nicht. Damit gilt:

- Jeder Neustart des IIS-Workers loescht den Wert. Am 2026-08-24 um 06:39 ist genau das
  passiert, und ueber das Wochenende lag der Prozess ohnehin still.
- Der naechtliche Nachschub greift kaum: das Einkauf-Delta laeuft **nur im planmaessigen Slot**
  (12:00) und bewusst nicht im Nachhol-Lauf. Lebt der Worker in dieser Minute nicht, faellt es
  aus. Am 20. und 21.08. fiel der Slot in einen Neustart; der Nachhol-Lauf um 12:41 machte den
  Verkaufsexport und liess den Einkauf liegen, wie vorgesehen.

Die Kachel konnte deshalb nur in einem schmalen Zeitfenster gefuellt sein und war jeden
Morgen wieder leer. Das ist keine Fehlfunktion des Fixes, sondern eine Luecke im Entwurf vom
2026-08-19, dort in Abschnitt 10.3 als "naechster Schritt" auch so benannt.

### 12.3 Zweite Ursache: der Lauf hing am Blazor-Circuit des Anwenders

`RunPurchasingFullLoadAsync` in der Seite wartete den Lauf im Klick-Handler ab
(`await ...RunFullLoadAsync()`). Ein Lauf dauert rund fuenfzig Minuten. Reisst die
SignalR-Verbindung in dieser Zeit ab, wechselt der Anwender die Seite oder startet IIS den
Worker neu, dann stirbt der Lauf mitten im Ablauf — und weil sein Statuseintrag zu diesem
Zeitpunkt auf `Running` steht, bleibt er dort fuer immer stehen. Genau so sind die vier
Zombie-Zeilen 31 bis 34 entstanden. Aufgeraeumt hat sie nichts.

### 12.4 Behoben

| Aenderung | Datei |
| --- | --- |
| Neue Tabelle `PurchasingStockValueCache` (Bewertungskreis, Disponent, Wert, Menge, Materialzahl, Lesezeitpunkt) | `Services/DatabaseInitializationService.SchemaSql.cs`, `Services/DatabaseSchemaMaintenanceService.cs` |
| Neuer Speicher `IPurchasingStockValueStore` mit `SaveAsync`/`LoadAsync`, Ersetzen in einer Transaktion | `Services/PurchasingStockValueStore.cs` (neu) |
| Der Einkauf-Lauf schreibt den gelesenen Stand jetzt zusaetzlich in die Datenbank | `Services/PurchasingDataRefreshService.cs` |
| Die Kachel liest zwei Quellen in dieser Reihenfolge: Arbeitsspeicher, dann Datenbank | `Services/PurchasingDashboardService.cs` |
| Neuer `PurchasingRefreshRunner`: der Lauf haengt an der Anwendung, nicht am Circuit; zwei gleichzeitige Laeufe werden abgewiesen | `Services/PurchasingRefreshRunner.cs` (neu), `Program.cs` |
| Beim Start werden liegengebliebene `Running`-Eintraege auf `Abgebrochen` gesetzt | `Services/PurchasingRefreshRunner.cs` |
| Die Seite loest nur aus und fragt im 15-Sekunden-Takt nach; der Hinweis nennt ausdruecklich, dass der Lauf im Hintergrund weiterlaeuft | `Components/Pages/PurchasingDashboard.razor` |
| Drei neue Texte in sechs Sprachen | `Services/UiTextGeneratedTranslations.cs` |
| 16 neue Tests | `TrafagSalesExporter.Tests/PurchasingStockValueStoreTests.cs`, `TrafagSalesExporter.Tests/PurchasingRefreshRunnerTests.cs` (beide neu) |

`580/580` Tests gruen (vorher 564), `dotnet build -c Release` ohne Fehler.

Ein Test hat dabei einen echten Fehler gefunden: `DateTimeStyles.RoundtripKind` laesst sich
nicht mit `AdjustToUniversal` kombinieren, die Kombination wirft. Ohne den Test waere jedes
Zurueckschreiben des Lesezeitpunkts produktiv gescheitert und die Kachel trotz Tabelle leer
geblieben.

### 12.5 Zwei bewusste Entscheidungen

1. **Der gespeicherte Stand verfaellt nicht.** Die Kachel zeigt ihn immer mit Lesezeitpunkt
   daneben. Ein sichtbar alter Wert ist fachlich brauchbar, eine leere Kachel ist es nicht. Die
   20 Stunden Haltbarkeit gelten weiter fuer den Arbeitsspeicher, der der schnellere Weg bleibt.
2. **Aufraeumen beim Start ist zulaessig**, weil ein Lauf im Prozess lebt: ueberlebt der Prozess
   nicht, hat auch kein Lauf ueberlebt. Ein Eintrag, der etwas anderes behauptet, ist eine
   Falschaussage. Deshalb wird beim Start und nur dort aufgeraeumt, bevor ein neuer Lauf
   beginnen kann.

### 12.6 Was offen bleibt

- **Der Beweis fuer den Fix der Endlosschleife fehlt weiterhin**, weil seit dem 2026-08-21
  kein Lauf mehr gestartet wurde. Erst ein durchgelaufener Full Load mit
  `Lagerwert-Read beendet` im Protokoll belegt ihn.
- Der naechtliche Einkauf-Lauf faellt weiterhin aus, wenn der Worker um 12:00 nicht lebt. Das
  ist unveraendert und bewusst so gebaut (kein SAP-Lauf bei beliebigem Tages-Restart). Mit dem
  gespeicherten Stand ist es aber nicht mehr sichtbar in der Kachel.
- Der MB5L-Abgleich aus Abschnitt 6a.3 steht weiterhin aus. Die Kachel zeigt einen fachlich
  **unbestaetigten** Wert.
- `MaterialUsageDataRefreshService.ReadAllRowsAsync` hat die strukturelle Schwaeche aus 11.5
  weiterhin ohne Notbremse.

### 12.7 Deploy und Ergebnis, 2026-08-24

| Punkt | Ergebnis |
| --- | --- |
| Deploy | 2026-08-24 08:01, Commits `45c3aa4` und `29a0f44`, `BiDashboard.dll` bitgleich mit dem lokalen Release-Build (SHA256 `49514D54F8CE1ABEFE6ECD9B3DA8B2AB8D985CF625C94B858CEE936C66EF520A`) |
| Wirknachweis in der DLL | `PurchasingStockValueCache`, `PurchasingStockValueStore`, `PurchasingRefreshRunner`, `Abgebrochen` |
| Routen | `/`, `/einkauf`, `/einkauf/verbindungen` je HTTP 200 |
| Sicherung | `trafag_exporter.db.before-stockvalue-persistence-20260824-080003.bak`, gepruefte Blockkopie |
| Tabelle angelegt | `PurchasingStockValueCache` ist in der produktiven Datenbank vorhanden |
| Zombie-Zeilen aufgeraeumt | Ids 31 bis 34 stehen jetzt auf `Abgebrochen`, mit angehaengter Begruendung; die abgeschlossenen Zeilen 29 und 30 wurden nicht angetastet |
| Lagerwert-Read | 42,5 s, 87 Disponenten, 68'657 Materialien, Gesamtwert CHF 11'552'400.04 |
| Kachelwert | **CHF 9'205'959** ueber 7'335 Materialien, Disponenten 001/002/003/004/005 |
| Kachel produktiv geprueft | `/einkauf` liefert `CHF 9'205'959 / 7'335 Materialien / Stand 24.08.2026 08:11` statt `wartet auf Einkauf-Lauf` |

**Damit ist der Fix der Endlosschleife aus Abschnitt 11 endlich belegt.** Der Read laeuft ueber
`MARCSet`, also genau die Stelle, an der die Schleife vom 2026-08-19 hing. Er kam nach 42,5
Sekunden zurueck.

Die Verteilung ueber die Disponenten (Bewertungskreis 1100, Stand 2026-08-24 08:11):

| Disponent | Wert CHF | Materialien | in der Kachel |
| --- | --- | --- | --- |
| 003 | 4'334'317.53 | 3'749 | ja |
| 001 | 4'250'829.48 | 2'853 | ja |
| 004 | 348'957.51 | 687 | ja |
| 002 | 253'416.32 | 34 | ja |
| 025 | 257'282.06 | 80 | nein |
| 024 | 244'460.13 | 397 | nein |
| 019 | 200'320.85 | 2'614 | nein |
| 099 | 175'222.46 | 42 | nein |

Auffaellig fuer die offene Frage aus Abschnitt 0: Disponent `005` erscheint mit Wert 0 und ist
in der Summe damit wirkungslos, `004` ("Betriebsmat/Einkau") traegt CHF 348'957.51 bei. Das ist
die Zahl, um die es bei Armins offener Entscheidung geht.

### 12.8 FALLE, dabei selbst hineingelaufen: ein Schreibvorgang von aussen ist fuer die Anwendung unsichtbar

Der erste Versuch, die Kachel zu fuellen, ging schief, und die Ursache ist eine Falle, die
jeden kuenftigen Eingriff von aussen betrifft.

Die produktive Datenbank laeuft im Modus `wal` (nachgemessen: `PRAGMA journal_mode` = `wal`).
Ein Werkzeug von diesem Rechner schrieb den Stand erfolgreich und las ihn selbst korrekt
zurueck. Die Anwendung auf dem Server zeigte trotzdem weiter `wartet auf Einkauf-Lauf`.

Grund: der Datensatz stand in `trafag_exporter.db-wal` auf der Freigabe (140'112 Bytes,
geschrieben 08:11:19), die Hauptdatei war noch vom 07:14. Im WAL-Modus finden Leser neue
Commits ueber den gemeinsamen Speicherindex `trafag_exporter.db-shm`. Der lag vom 08:01:46, also
vom Start der Anwendung nach dem Deploy. **Ueber SMB ist diese Koordination zwischen zwei
Rechnern nicht verlaesslich**, die Anwendung sah den Commit deshalb nicht.

Nachgewiesen durch Ausschluss, nicht vermutet: eine Sonde
(`.tmp_tools/ProbeStockValueTile`) baute `IPurchasingDashboardService` genau wie `Program.cs`
auf und zeigte `_stockValueStore injiziert: True` und
`Store.LoadAsync: 87 Zeilen, Gesamtwert 11'552'400.04`. Der Code war also in Ordnung, die
Sichtbarkeit nicht.

Behoben mit `PRAGMA wal_checkpoint(TRUNCATE)` (`.tmp_tools/CheckpointWal`): danach
`busy=0 log=0 checkpointed=0`, WAL- und SHM-Datei verschwunden, Hauptdatei 08:24:51, und der
naechste Abruf von `/einkauf` zeigte den Wert.

**Lehre fuer kuenftige Eingriffe:** Daten, die die Anwendung lesen soll, sollte die Anwendung
selbst schreiben. Wird doch von aussen geschrieben, gehoert ein Checkpoint dazu, sonst ist der
Datensatz vorhanden und trotzdem wirkungslos — und die Fehlersuche landet zuerst im Code, wo
nichts zu finden ist. Das ist dieselbe Klasse von Irrtum wie beim Overlay am 2026-08-21:
ausgeliefert ist nicht angekommen.

### 12.9 Was jetzt noch offen ist

- **Die ganze Kette serverseitig** ist noch nicht am Stueck gelaufen: Klick, Runner,
  Lagerwert-Read, Speichern, Kachel. Belegt sind die Teile einzeln. Der naechtliche Slot um
  12:00 fuehrt sie zusammen, sofern der Worker dann lebt.
- Der MB5L-Abgleich aus Abschnitt 6a.3 steht weiterhin aus. Die Kachel zeigt einen fachlich
  **unbestaetigten** Wert.
- Armins Entscheidung zu Disponent `004` (CHF 348'957.51, siehe Tabelle oben) ist offen.
- `MaterialUsageDataRefreshService.ReadAllRowsAsync` hat die strukturelle Schwaeche aus 11.5
  weiterhin ohne Notbremse.

## 13. Nachtrag 2026-09-28: Lagerwert-Verlauf je Woche (Weg A)

### 13.1 Wunsch und Entscheid

Waehrend Ingos Ferien kam per Mail der Wunsch, "den woechentlich abgefragten Lagerwert der
Einkaufsteile in eine Liste zu schreiben und eine Grafik anzuzeigen, wie sich die Werte ueber
die Wochen veraendert haben", damit ein Trend sichtbar wird.

Ingo hat zwei Wege vorgelegt bekommen und am 2026-09-28 **Weg A** gewaehlt ("mach mal weg a"):

| Weg | Inhalt | Stand |
| --- | --- | --- |
| **A** | Ab jetzt jeden gelesenen Stand zusaetzlich in eine Verlaufstabelle schreiben und je Kalenderwoche anzeigen. Keine SAP-Aenderung. | **gebaut, siehe 13.2** |
| B | Verlauf rueckwirkend aus `MBEWH` ueber das aggregierende EntitySet aus 10.4, nur **monatlich** moeglich | nicht begonnen, nicht gewuenscht |

Marco hat dazu am selben Tag geschrieben, dass "das Erfassen ab verfuegbar schon sehr
hilfreich" sei und der Trend in Zukunft interessant werde. Das deckt Weg A. Seine Vermutung,
der Bestand per Monatsende lasse sich rueckwirkend kaum sauber bestimmen, trifft so nicht zu:
Abschnitt 3.3 und 4.1 zeigen, dass es ueber `MBEWH` geht, nur eben mit SAP-Aufwand (Weg B).

**Zwei Richtigstellungen zur Mail:** Der Wert wird nicht woechentlich, sondern bei **jedem
Einkauf-Lauf** gelesen, planmaessig taeglich um 12:00 (siehe 12.2). Die Woche bildet erst die
Anzeige. Und der Wert ist weiterhin **nicht gegen MB5L abgeglichen**; ein Trend auf einer
unbestaetigten Zahl bleibt unbestaetigt.

### 13.2 Was gebaut wurde

| Datei | Aenderung |
| --- | --- |
| `Services/DatabaseInitializationService.SchemaSql.cs` | neue Tabelle `PurchasingStockValueHistory` (Bewertungskreis, Tag, Disponent, Wert, Menge, Materialzahl, Lesezeitpunkt) |
| `Services/DatabaseSchemaMaintenanceService.cs` | legt die Tabelle beim Start an und uebernimmt den vorhandenen Kachelstand als ersten Verlaufspunkt |
| `Services/PurchasingStockValueStore.cs` | `SaveAsync` schreibt in **derselben Transaktion** zusaetzlich den Tagesstand; neu `LoadHistoryAsync`, `SeedHistoryFromCache`, `StockValueHistory.ToWeekly` |
| `Services/PurchasingDashboardService.cs`, `Services/IPurchasingDashboardService.cs` | Wochenverlauf im Anzeigezustand (`StockValueWeeklyHistory`) |
| `Components/Pages/PurchasingDashboard.razor` | Panel "Lagerwert Einkaufsteile: Verlauf je Woche" oben auf der Einkauf-Uebersicht: Liniengrafik plus Tabelle (KW, Stand vom, Wert, Veraenderung zur Vorwoche, Materialien) |
| `Services/PurchasingUiTextGeneratedTranslations.cs` | neue Texte in es, it, hi, sq, tr, tlh |
| `TrafagSalesExporter.Tests/PurchasingStockValueStoreTests.cs` | 9 neue Tests |

Tests: `724/724` gruen (lokal, Arbeitsbaum mit fremden unkommittierten Finance_All-Aenderungen).
**Die Oberflaeche ist nicht im Browser angesehen worden**, weil nicht deployed.

### 13.3 Entwurfsentscheidungen

1. **Je Tag gespeichert, je Woche angezeigt.** Ein zweiter Lauf am selben Tag ersetzt den
   Tagesstand, sonst wird aus dem Verlauf nie geloescht. Als Wochenpunkt gilt der **letzte**
   vorhandene Tag der ISO-Kalenderwoche (Stand Ende Woche). Der Tag wird in Schweizer Ortszeit
   bestimmt.
2. **Alle Disponenten gespeichert, gefiltert wird beim Lesen.** Faellt Armins Entscheid zu
   `004` anders aus, stimmt damit der ganze Verlauf rueckwirkend, ohne Datenkorrektur.
3. **Wochen ohne Lauf bleiben Luecken**, es wird nicht interpoliert. Die Tabelle nennt bei
   einer Luecke die Vergleichswoche.
4. **ISO-Jahr statt Kalenderjahr.** Der 2027-01-01 gehoert zur KW 53/2026; mit dem
   Kalenderjahr wuerde diese Woche in zwei Punkte zerfallen. Ist getestet.
5. **Die Hochachse beginnt beim tiefsten Wert**, nicht bei null, und das steht unter der
   Grafik. Bei rund CHF 9 Mio waeren Wochenbewegungen sonst eine flache Linie.
6. **Die Anwendung schreibt den ersten Punkt selbst** (beim Start aus dem Kachelstand), nicht
   ein Werkzeug von aussen. Grund ist die WAL-Falle aus 12.8. Dieser erste Punkt traegt das
   Datum des letzten erfolgreichen Lagerwert-Reads und kann deshalb **aelter als der Deploy**
   sein. Die Oberflaeche nennt darum das Datum des ersten Punkts aus den Daten statt eines
   festen Startdatums. Scheitert die Uebernahme, faengt die Schemapflege den Fehler ab und
   der Verlauf beginnt mit dem naechsten Lauf.

### 13.4 Was offen ist

- **Nicht deployed.** Der Arbeitsbaum enthaelt fremde, nicht committete Aenderungen an
  `Program.cs`, `Services/TimerBackgroundService.cs` und `TrafagSalesExporter.csproj`
  (Finance_All-Reservierung). Ein Release-Build aus diesem Baum wuerde sie mit ausliefern.
  Deploy nur nach Freigabe durch Ingo und aus einem sauberen Stand. Der Commit-Stand ist in
  einem eigenen Worktree geprueft: `709/709` gruen. Der Build braucht dort eine (leere)
  `trafag_exporter.db`, weil das Projekt sie ins Ausgabeverzeichnis kopiert.
- **Ein Deploy von HEAD liefert mehr als den Verlauf.** Seit dem letzten Deploy (`d5bc321`,
  2026-09-10 09:40) sind ausserdem `4d0b9cd` (Marktsegment-Vorschlaege) sowie `16bc901` und
  `caaf377` (CH/AT-Journal monatsweise lesen) nicht ausgeliefert.
- **Der Verlauf beginnt erst mit dem Deploy.** Die Vorher-Sicherungen der Datenbank seit dem
  2026-08-24 enthalten je einen Kachelstand und koennten einige Punkte nachliefern. Das
  read-only Auslesen der produktiven Sicherungen wurde in dieser Sitzung vom
  Berechtigungssystem abgelehnt und ist deshalb **nicht gemacht**. Entscheid bei Ingo.
- **Der Verlauf fuellt sich nur, wenn die Einkauf-Laeufe tatsaechlich laufen.** Der
  Kettennachweis aus 12.9 steht weiterhin aus.
- MB5L-Abgleich und Entscheid zu `004` wie bisher offen.
