# Lagerwert der Einkaufsteile im Einkauf-Cockpit

Stand: 2026-08-18. Zurueck: `docs/router/einkauf.md`.

## 0. Stand auf einen Blick

| | |
| --- | --- |
| **Auftrag** | Armin will den Lagerwert der Einkaufsteile als KPI-Kachel, „per «bis Monat»", Werte wie MB5L, abgegrenzt auf die Disponenten `001`–`005`. |
| **Umgesetzt?** | **Nein.** Analyse und Messung sind fertig, Code ist nicht angefasst. |
| **Die Zahl** | Einkaufsteile heute: **CHF 8'982'938.78** ueber 7'261 Materialien, das sind 82 % des gesamten Lagerwerts von CHF 10'937'376.40. |
| **Machbar?** | Ja. Alle fuenf Disponenten existieren, `MBEWH` reicht bis 2000 zurueck, die Stichtagsrechnung ist gebaut und in sich geprueft. |
| **Groesster offener Punkt** | Der MB5L-Abgleich. Ohne ihn ist die Zahl nicht freigegeben. |
| **Groesste technische Huerde** | `MBEWH` ist mit 5,4 Mio Zeilen nicht ueber OData ladbar. Es braucht ein serverseitig aggregierendes SAP-Set. |

**Hier geht es weiter:** Abschnitt 7.

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
| `SUMMEN` | **hoch** — Summenzeilen zusaetzlich in der Ausgabe; wer alle Zeilen addiert, zaehlt doppelt. |
| `KEINZEL` | **hoch** — unterdrueckt Einzelposten; dann bleiben nur Summen. |
| `MATLINES` | **hoch** — begrenzt die Materialzeilen; Liste abgeschnitten, Summe zu niedrig. |
| `NEGATIV` | **hoch** — schraenkt auf negative Bestaende ein. |
| `VMSALDO` / `VJSALDO` | **hoch** — zusaetzliche Wertspalte Vormonat bzw. Vorjahr. |
| `BWTAR` | **besonders** — bei getrennter Bewertung fuehrt SAP je Material einen Kopfsatz UND Teilsaetze; beide summiert ergibt den doppelten Wert. |
| `AKSALDO` | **muss an sein**, sonst wird ein anderer Zeitpunkt verglichen als `MBEW` fuehrt. |
| `BUKRS`, `BKLAS`, `MATNR`, `SKONT` | mittel — schraenken die Materialmenge ein. |
| `NULLB` | bewusst an, damit ein Material mit Wert ohne Menge nicht fehlt. |

Der Abgleichsreport setzt deshalb gezielt `BWKEY`, `AKSALDO = X`, `NULLB = X` und leert
`VMSALDO`/`VJSALDO`. Alles andere bleibt auf der Vorbelegung des Programms.

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

## 8. Werkzeuge

| Datei | Zweck |
| --- | --- |
| `docs/abap/Z_PURCHASING_LAGERWERT_ANALYSE.abap` | Disponenten, Lagerwert je Disponent, `VPRSV`-Verteilung, `MBEWH`-Verfuegbarkeit, Stichtagsrechnung mit waehlbarem Jahr/Periode. Read-only. |
| `docs/abap/Z_PURCHASING_MB5L_ABGLEICH.abap` | Ruft MB5L per `SUBMIT` auf und stellt die Summen gegenueber. Zweistufig: ohne `p_run` nur Analyse, mit `p_run` der echte Lauf. Read-only. |

Beide ermitteln Programm- und Feldnamen aus dem System, statt sie anzunehmen.

## 9. Status

Analyse und Dokumentation. **Kein Anwendungscode, keine Datenbank, kein Deploy.** Die Umsetzung
ist bewusst nicht begonnen, weil der MB5L-Abgleich und drei fachliche Entscheidungen ausstehen.
