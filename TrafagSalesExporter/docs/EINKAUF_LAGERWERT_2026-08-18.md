# Lagerwert der Einkaufsteile im Einkauf-Cockpit

Stand: 2026-08-18. Zurueck: `docs/router/einkauf.md`.

## 1. Die Anforderung

Armin hat am 2026-08-18 per Mail an Ingo geschrieben:

> wie heute vor der Pause kurz angesprochen, waere der Lagerwert von den Einkaufsteilen eine
> interessante Groesse

In der beigelegten Grafik zeigt ein Pfeil auf die KPI-Zeile des Einkauf-Cockpits (neben
`Spend total`, `Bestellungen im Zeitraum`, `Verpflichtungen`, `Lieferantenperformance`) mit zwei
praezisen Vorgaben:

- **„Lagerwert per «bis Monat» (Werte dito Transaktion MB5L)"**
- **„Abgrenzung: Einkaufsteile, d.h. Disponenten 001, 002, 003, 004, 005"**

Das ist damit keine vage Idee, sondern eine Anforderung mit Stichtagsbezug, Referenztransaktion
und Mengenabgrenzung.

## 2. Ist-Stand: nicht umgesetzt

Geprueft am 2026-08-18 gegen den Code- und Dokumentationsstand:

| Frage | Antwort |
| --- | --- |
| Gibt es im Einkauf-Cockpit einen Lagerwert? | **Nein.** Keine Kachel, keine Query, kein Feld. |
| Wird `MBEW` bereits gelesen? | Ja, aber **nur als Stueckpreis** (`STPRS`) fuer die Gruppenmarge, siehe `docs/FINANCE_STANDARDKOSTEN.md`. Bestandsmenge (`LBKUM`) und Bestandswert (`SALK3`) werden nirgends verwendet. |
| Gibt es Bestandswerte woanders? | Im Logistik-Stuecklisten-Dashboard gibt es Bestands**mengen**. Eine aggregierte Bewertung in CHF wurde dort **bewusst weggelassen**, weil eine mehrfach verwendete Komponente ueber Stuecklistenbeziehungen doppelt gezaehlt wuerde (`docs/LOGISTIK_STUECKLISTEN_DASHBOARD_2026-08-01.md`). Das ist eine andere Fragestellung als Armins und kein Gegenargument zu seinem Wunsch. |
| Gibt es schon eine Doku dazu? | **Nein.** Diese Datei ist die erste. |

Vorhanden ist lediglich eine Datenprofilierung: Der Analyse-Report vom 2026-07-09 (T76/100) hat
gemessen, dass `MBEW.STPRS` bei `3'725` von `3'727` Saetzen gefuellt ist und `SALK3 > 0` bei
`2'762` Saetzen. Bestandswerte sind also grundsaetzlich vorhanden. Im Ideenbereich steht
ausserdem `Working Capital` als unausgearbeitete Idee.

## 2a. Messung am 2026-08-18 (T76/100, Bewertungskreis 1100)

Gelaufen mit `docs/abap/Z_PURCHASING_LAGERWERT_ANALYSE.abap`.

**Armins Abgrenzung ist stimmig.** Alle fuenf Disponenten existieren, und ihre Bezeichnungen
bestaetigen die Zuordnung zum Einkauf:

| Disponent | Bezeichnung | Materialien | Lagerwert CHF |
| --- | --- | --- | --- |
| `001` | rot / Einkauf | 2'810 | 4'041'124.80 |
| `002` | gun / Einkauf | 30 | 302'246.37 |
| `003` | mso / Einkauf | 3'724 | 4'301'097.56 |
| `004` | Betriebsmat/Einkau | 686 | 322'931.33 |
| `005` | wid / Einkauf | 11 | 15'538.72 |
| **Summe** | | **7'261** | **8'982'938.78** |

Das ist die **Zielzahl fuer den MB5L-Abgleich** (Stichtag heute, Bewertungskreis 1100).

Weitere Messwerte:

- `65'549` MARC-Saetze im Werk 1100, `65'498` MBEW-Saetze. Es gibt **89 Disponenten** insgesamt;
  die grosse Restmenge liegt in Produktions- und Sensorik-Dispos (`A99 PPD / Status 99` allein
  26'750 Materialien) sowie 2'172 Materialien ohne Disponent.
- **Kein bewertetes Material ohne MARC-Satz** (0 Zeilen, 0 CHF). Der Join ist also lueckenlos.
- Preissteuerung: `65'493` mal `S` (Standardpreis), **`5` mal `V`** (gleitender Durchschnitt).
  Die fuenf `V`-Materialien genuegen, um `SALK3` verbindlich zu machen.
- `MBEWH` ist vorhanden und umfangreich: **`5'371'798` Saetze** allein im Bewertungskreis 1100,
  mit Historie zurueck bis Periode 2000/12.

## 2b. FALLSTRICK: MBEWH darf nicht einfach summiert werden

Die Messung hat einen Fehler in der naheliegenden Methode aufgedeckt. Abschnitt 5 des Reports
summierte fuer jede Periode schlicht die vorhandenen `MBEWH`-Saetze. Das ergibt:

| Periode | Materialien | Summe SALK3 |
| --- | --- | --- |
| 2026/01 | 6'814 | 8'472'199.14 |
| 2026/02 | 6'817 | 8'355'679.91 |
| 2026/03 | 1'432 | 5'914'193.63 |
| 2026/04 | **74** | **383'904.48** |
| 2026/05 | 136 | 681'062.31 |
| 2026/06 | 652 | 3'070'209.07 |
| 2026/07 | **60** | **285'762.03** |

Der Bestand ist selbstverstaendlich nicht von 8,4 Millionen auf 0,29 Millionen gefallen.
**`MBEWH` enthaelt fuer eine Periode nur die Materialien, bei denen es danach eine
bewertungsrelevante Bewegung gab.** Ein Material, das seit Maerz nicht bewegt wurde, hat fuer
2026/04 keinen Historiensatz; sein Wert steht unveraendert in `MBEW`. Je juenger die Periode,
desto grosser die Luecke — und in den Altjahren fallen einzelne Perioden aus demselben Grund ab
(z. B. 2025/07 nur 1'787 Materialien, 2024/03 nur 1'858).

**Korrekte Logik fuer einen Stichtag P** (das ist auch, was MB5L tut): je Material den
`MBEWH`-Satz der **kleinsten Periode >= P** nehmen; existiert keiner, gilt der aktuelle
`MBEW`-Wert. Eine reine Summe ueber `MBEWH` ist systematisch zu niedrig und waere als
Cockpit-Kachel ein Zahlenfehler.

Der Report ist entsprechend korrigiert und rechnet jetzt mit dieser Rueckrechnung, mit einem
waehlbaren Stichtag. Die Zahlen der Tabelle oben sind damit **ueberholt** und nur noch als Beleg
fuer den Fallstrick aufgefuehrt.

## 2c. Zweiter Lauf mit korrigierter Rueckrechnung

Stichtag `2026 / 06`, Bewertungskreis `1100`:

| Groesse | Wert |
| --- | --- |
| Lagerwert per 2026/06 (korrekt zurueckgerechnet) | **CHF 8'973'694.30** |
| Menge | 14'215'807.724 |
| Aktueller Wert aus `MBEW` (heute) | CHF 8'982'938.78 |
| Differenz zu heute | −9'244.48 CHF (0,1 %) |
| davon aus `MBEWH` (Historiensatz vorhanden) | 687 Materialien |
| davon aus `MBEW` (Fallback) | 6'574 Materialien |
| **Naive Summe nur ueber `MBEWH`** | **CHF 3'070'209.07** — um Faktor 2,9 zu niedrig |

Die naive Methode haette also fuer Juni 2026 rund 5,9 Millionen CHF zu wenig ausgewiesen. Der
Unterschied zwischen richtiger und falscher Rechnung ist damit belegt.

**Vorbehalt zur Aussagekraft (Hinweis Ingo):** `T76` ist eine mehrere Monate alte Kopie, in 2026
bewegt sich dort kaum noch etwas. Deshalb stammen 6'574 der 7'261 Werte aus dem `MBEW`-Fallback
und nur 687 aus der Historie. Der Fallback-Pfad ist damit gut geprueft, der **Historienpfad
praktisch nicht**. Fuer eine belastbare Abnahme fehlt ein zweiter Lauf mit einem Stichtag, an dem
die Historie traegt, zum Beispiel `2025 / 03` oder `2024 / 12` — dort liegen laut Abschnitt 4
noch rund 33'000 Saetze je Periode. Auf dem Produktivsystem ist die Verteilung ohnehin anders,
weil dort laufend gebucht wird.

Nebenbefund zur Datenmenge: Fuer die 7'261 Zielmaterialien allein liegen **1'132'402**
`MBEWH`-Saetze vor (Historie bis 2000). Das unterstreicht Abschnitt 3b. Der Report liest die
Historie inzwischen mit Periodeneinschraenkung in der `WHERE`-Klausel statt alles zu holen und
im Code zu verwerfen.

## 2d. Dritter Lauf: Gegentest mit altem Stichtag

Stichtag `2024 / 06`, also bewusst so gewaehlt, dass der Historienpfad traegt statt des
Fallbacks. Damit ist die in 2c beschriebene Luecke geschlossen.

| Groesse | 2026 / 06 | 2024 / 06 |
| --- | --- | --- |
| Lagerwert (korrekt zurueckgerechnet) | CHF 8'973'694.30 | **CHF 10'648'966.26** |
| Menge | 14'215'807.724 | 15'480'608.268 |
| aus `MBEWH` (Historie) | 687 (9 %) | **6'893 (95 %)** |
| aus `MBEW` (Fallback) | 6'574 (91 %) | 368 (5 %) |
| naive Summe nur ueber `MBEWH` | CHF 3'070'209.07 | CHF 10'640'616.98 |
| Abweichung der naiven Methode | **−66 %** | **−0,08 %** |

**Beide Pfade sind damit geprueft.** Beim alten Stichtag stammen 95 % der Werte aus der
Historie, beim jungen 91 % aus dem Fallback. Die Rueckrechnung liefert in beiden Faellen einen
plausiblen Wert, und der Bestandsverlauf ist stimmig: von 10,65 Mio (Mitte 2024) auf 8,98 Mio
(heute), passend zur Periodenreihe aus dem ersten Lauf.

### Die eigentliche Falle: die naive Methode sieht bei alten Perioden richtig aus

Das ist der wichtigste Befund dieses Laufs. Die falsche Methode weicht

- bei `2024 / 06` nur um **0,08 %** ab (8'349 CHF) — praktisch unauffaellig,
- bei `2026 / 06` dagegen um **66 %** (5,9 Mio CHF) — offensichtlich kaputt.

Der Grund ist derselbe wie in 2b: Je aelter die Periode, desto dichter ist die Historie, weil
inzwischen fast jedes Material einmal bewegt wurde. Wer die Umsetzung also an einem
zurueckliegenden Stichtag testet, **sieht den Fehler nicht** und baut ihn produktiv ein — wo er
dann ausgerechnet fuer den aktuellen Monat zuschlaegt, den die Kachel am haeufigsten zeigt.

Konsequenz fuer die Abnahme: **immer mit einem jungen Stichtag testen**, nicht mit einem alten.

### Was noch fehlt

Beide Laeufe zeigen nur, dass die Rueckrechnung in sich stimmig ist. **Konsistenz ist nicht
Korrektheit.** Ob die Zahl fachlich richtig ist, beweist erst der Abgleich mit MB5L.

## 3. Der fachliche Knackpunkt: „per «bis Monat»"

Das ist der Teil, an dem die Umsetzung haengt, und er ist nicht offensichtlich.

`MBEW` (Materialbewertung) fuehrt **nur den aktuellen Bestand**. Es gibt dort keinen Zeitbezug.
Fragt man `MBEW` heute ab, bekommt man den Wert von heute, nicht den Wert per 31.03.

Die Transaktion **MB5L, auf die Armin sich ausdruecklich beruft, liest fuer Stichtage in der
Vergangenheit die Bewertungshistorie `MBEWH`** (monatsgenau, je Bewertungskreis und Periode).
Genau deshalb kann MB5L rueckwirkend Salden zeigen.

Daraus folgt eine Verzweigung, die vor der Umsetzung entschieden werden muss:

- **Variante A, „Stand heute":** nur `MBEW`. Schnell umsetzbar, weil die Quelle bereits
  angebunden ist. Erfuellt Armins Vorgabe aber **nicht** — die Kachel wuerde sich beim Verstellen
  des Zeitraumfilters nicht aendern, was irrefuehrend waere, weil alle Nachbarkacheln auf den
  Filter reagieren.
- **Variante B, „per bis Monat" wie bestellt:** braucht `MBEWH` im OData-Modell. Das ist eine
  SAP-Erweiterung, vergleichbar mit der `MARA001Set`-Erweiterung vom 2026-07-23.

**Nicht geraten:** Ob `MBEWH` ueber `ZPOWERBI_EINKAUF_SRV` erreichbar ist, welche Felder das
`mbewSet` heute fuehrt und ob die Historie ueberhaupt gepflegt wird, ist ungeprueft. Nach
Vorrangregel 5 wird das gemessen, nicht angenommen. Dafuer gibt es den Report in Abschnitt 6.

## 3a. Was zu den offenen Punkten schon dokumentiert ist

Geprueft am 2026-08-18 quer durch `docs/`:

| Punkt | Schon beschrieben? | Wo und was |
| --- | --- | --- |
| **Bewertungskreis** | **Ja, belegt** | `docs/FINANCE_STANDARDKOSTEN.md`: „MBEW ist je Material **und** Bewertungskreis verschluesselt (CH = 1100, AT = 1200, per `T001K` bestaetigt)". Damit ist Frage (d) beantwortet: `1100` ist CH, `1200` ist AT, und eine Summe ueber beide waere wegen unterschiedlicher Hauswaehrung falsch. |
| **`mbewSet` im Gateway** | **Ja, im Einsatz** | `Services/SapGatewayStandardCostReader.cs` liest `mbewSet` produktiv fuer die Gruppenmarge, mit `$filter=Bwkey eq '...'`. Dokumentiert in `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md`. |
| **Kosten dieses Reads** | **Ja, gemessen** | Ebendort im Code: `mbewSet` ignoriert `$top`, `$skip` und `$orderby` und liefert bei jeder Anfrage den vollen Bestand — am Produktivsystem 2026-07-28 gemessen: **`68'543` Zeilen, 124 MB, rund 28 Sekunden**. |
| **`MBEWH` im Gateway** | **Nein** | Nirgends erwaehnt. Ob ein Historien-Set existiert, ist offen. |
| **Sonderbestaende** (`MSKU`, `MSKA`, `MSPR`) | **Nein** | Kommen in der gesamten Dokumentation nicht vor. Vollstaendig offen. |
| **Disponent `004` Betriebsmaterial** | **Nein** | Nicht dokumentiert. Die Messung liefert aber den SAP-Text `Betriebsmat/Einkau`, der Disponent gehoert also organisatorisch zum Einkauf. Ob er fachlich in den Lagerwert soll, bleibt Armins Entscheidung. |
| **Preiseinheit `PEINH`** | **Ja, als Warnung** | `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md`: „`PEINH` ist kritisch: der Preis gilt pro X Stueck — wird das uebersehen, liegt die Marge um Faktor 10/100 daneben". Fuer den Lagerwert entfaellt das Problem, weil `SALK3` ein absoluter Wert ohne Preiseinheit ist. Ein weiteres Argument gegen `LBKUM * STPRS`. |

## 3b. Architektur-Folgerung: MBEWH ist nicht ladbar wie MAKT oder MARA

Das ist die wichtigste technische Konsequenz aus 2a und 3a.

Der bisherige Weg fuer Stammdaten war „ein ungepagter Request, alles in den Cache" — so laufen
`MARA001Set`, `MAKTSet` und `mbewSet`. Fuer `MBEWH` funktioniert das **nicht**:

- `mbewSet` kostet mit `68'543` Zeilen bereits **124 MB und 28 Sekunden** je Aufruf.
- `MBEWH` hat im Bewertungskreis 1100 allein **`5'371'798` Zeilen**, also rund das
  **78-fache**. Hochgerechnet waeren das mehrere Gigabyte je Aufruf.

Ein Full-Load-Ansatz ist damit ausgeschlossen. Der richtige Weg ist ein **eigenes, serverseitig
aggregierendes EntitySet**: SAP rechnet den Lagerwert je Bewertungskreis, Periode und
Disponentengruppe und liefert wenige Zeilen statt Millionen. Die Rueckrechnungslogik aus 2b
gehoert dann ebenfalls nach ABAP, wo sie neben den Daten liegt, statt in C# ueber einen
Millionen-Zeilen-Cache. Muster dafuer: `docs/abap/ZSTR_MAT_XYZ_GET_ENTITYSET.abap`.

## 4. Weitere Punkte, die vor der Umsetzung zu klaeren sind

1. **Bewertungskreis.** MB5L laeuft je Bewertungskreis (`BWKEY`). Fuer Trafag CH ist das `1100`.
   Soll die Kachel nur `1100` zeigen oder mehrere Kreise summieren? Eine Summe ueber Kreise mit
   unterschiedlicher Hauswaehrung waere falsch.
2. **Woher kommt der Disponent?** Armins Abgrenzung `001`–`005` ist der Disponent aus dem
   Werksstamm (`MARC.DISPO`). Das Einkauf-Cockpit fuehrt heute einen **anderen** Disponentenweg:
   `ZLO03`-Komponente -> `VknrDispo` -> Produktgruppenregel
   (`PurchasingSpendDisponentRule`, 45 SAP-OData-Regeln). Beide Wege duerfen nicht verwechselt
   werden, sonst weicht die Kachel von MB5L ab.
3. **Deckt `001`–`005` wirklich „Einkaufsteile"?** Aus dem Analyse-Report vom 2026-07-09 sind
   unter anderem bekannt: `001 rot/Einkauf` (1'568 Materialien), `003 mso/Einkauf` (1'281),
   `004 Betriebsmat` (542). `002` und `005` sind in dieser Auswertung nicht aufgetaucht. Ob sie
   existieren, leer sind oder schlicht nicht in den Top-Gruppen lagen, ist offen. Ebenso, ob
   `004 Betriebsmat` (Betriebsmaterial) fachlich zum gewuenschten Lagerwert gehoeren soll.
4. **Bewertungsart.** `MBEW.SALK3` ist der Bestandswert zum jeweils gueltigen Preissteuerungs-
   kennzeichen (`VPRSV`: `S` Standardpreis, `V` gleitender Durchschnitt). MB5L zeigt denselben
   Wert; eine eigene Rechnung `LBKUM * STPRS` waere bei `V`-Materialien falsch. Es ist also
   `SALK3` zu verwenden, nicht selbst zu multiplizieren.
5. **Semantik der Kachel.** Alle heutigen Kacheln sind Periodengroessen (Spend im Zeitraum) oder
   Stand-heute-Groessen (offene Verpflichtungen). Ein Lagerwert ist ein **Stichtagswert**. Die
   Beschriftung muss das sagen, sonst wird er als Periodenfluss gelesen und mit dem Spend addiert.
6. **Sonderbestaende.** MB5L kennt Konsignations-, Kundenauftrags- und Projektbestand
   (`MSKU`, `MSKA`, `MSPR`). `MBEW` allein deckt nur den frei verwendbaren Eigenbestand. Ob
   Armin die Sonderbestaende erwartet, ist zu fragen.

## 5. Vorschlag fuer das Vorgehen

1. **Messen** — erledigt am 2026-08-18, siehe 2a bis 2d. Beide Rechenpfade geprueft.
2. **Gegen MB5L abgleichen — der naechste offene Schritt.** MB5L mit Bewertungskreis `1100`
   laufen lassen, einmal per heute (Erwartung `8'982'938.78`) und einmal per Ende Juni 2026
   (Erwartung `8'973'694.30`), jeweils eingeschraenkt auf die Disponenten `001`–`005`.
   **Wichtig: den jungen Stichtag nicht weglassen** — bei einem alten Stichtag saehe selbst die
   nachweislich falsche Methode richtig aus (2d). Erst bei Uebereinstimmung ist die Grundlage
   belastbar. Ohne diesen Abgleich keine Kachel.
3. **Offene Fragen aus Abschnitt 4 mit Armin klaeren**, insbesondere `004 Betriebsmat`,
   Sonderbestaende und Bewertungskreis.
4. **Erst danach umsetzen.** Bei Variante B zuerst die SAP-Erweiterung fuer `MBEWH`, dann Cache,
   dann Kachel. Nach Marcos Leitplanke „ein Punkt nach dem anderen" und erst nach Abnahme des
   laufenden Spend-Themas.

## 5a. Beantwortete und noch offene Fragen im Ueberblick

| Frage | Stand |
| --- | --- |
| Existieren die Disponenten `001`–`005`? | **Ja**, alle fuenf, zusammen 7'261 Materialien, alle mit „Einkauf" im Namen. |
| Ist `MBEWH` vorhanden? | **Ja**, 5'371'798 Saetze zurueck bis 2000. |
| Ist `SALK3` oder `LBKUM * STPRS` zu nehmen? | **`SALK3`**, belegt durch 5 `V`-Materialien und die dokumentierte `PEINH`-Falle. |
| Welcher Bewertungskreis? | **Beantwortet**: `1100` = CH, `1200` = AT (`T001K`). Nicht summieren. |
| Wie rechnet man einen Stichtag? | **Beantwortet und korrigiert**: kleinste `MBEWH`-Periode >= Stichtag, sonst `MBEW`. Nicht naiv summieren. |
| Kann man `MBEWH` per OData laden? | **Nein**, siehe 3b. Braucht ein aggregierendes EntitySet. |
| Stimmt die Zahl mit MB5L? | **Offen** — der Abgleich ist der naechste Schritt. |
| Ist die Rueckrechnung in sich stimmig? | **Ja**, beide Pfade geprueft: Fallback bei `2026/06` (91 %), Historie bei `2024/06` (95 %). Siehe 2c und 2d. |
| Ist sie fachlich korrekt? | **Offen** — das beweist nur MB5L. |
| Sonderbestaende einbeziehen? | **Offen**, Frage an Armin, nirgends dokumentiert. |
| Disponent `004` Betriebsmaterial einbeziehen? | **Offen**, Frage an Armin. |

## 5b. MB5L-Abgleich per Report statt von Hand

`docs/abap/Z_PURCHASING_MB5L_ABGLEICH.abap` (read-only) fuehrt den Vergleich selbst aus, damit
die Zahl nicht abgetippt und dabei anders eingegrenzt wird als gerechnet.

Ablauf in zwei Stufen, `p_run` zunaechst leer lassen:

1. **Ohne `p_run`:** ermittelt den Programmnamen zu `MB5L` **aus `TSTC`** statt ihn zu raten,
   listet dessen Selektionsparameter auf und rechnet die eigene Vergleichszahl aus `MBEW`.
   Kostet nichts und zeigt, ob die Feldzuordnung passt.
2. **Mit `p_run = X`:** ruft das Programm per `SUBMIT` mit dynamisch gefuellter
   Selektionstabelle auf — gefuellt wird **nur, was in Stufe 1 tatsaechlich vorhanden war**.
   Das Ergebnis wird ueber `cl_salv_bs_runtime_info` abgegriffen (MB5L ist ALV-basiert, ein
   reines `EXPORTING LIST TO MEMORY` liefert dort oft nichts), summiert und gegenuebergestellt.

**Verglichen wird die Gesamtsumme des Bewertungskreises, nicht die Disponentengruppe.** MB5L
kennt keinen Disponentenfilter. Stimmt die Gesamtsumme, ist die Bewertungslogik (`SALK3`)
bestaetigt; die Abgrenzung auf `001`–`005` ist danach nur noch ein Filter auf derselben Basis
und braucht keinen eigenen Beweis.

Weicht die Summe ab, nennt der Report die wahrscheinlichen Ursachen in der zu pruefenden
Reihenfolge: Sonderbestaende, getrennte Bewertung (`BWTAR`), abweichende Selektion.

### Ergebnis Stufe 1, gemessen 2026-08-18 auf T76/100

- Transaktion `MB5L` -> Programm **`RM07MBST`** (aus `TSTC`, nicht angenommen).
- 16 Selektionsfelder, darunter `BWKEY`, `BUKRS`, `BKLAS`, `BWTAR`, `MATNR`, `SKONT` als
  Select-Options und `AKSALDO`, `VMSALDO`, `VJSALDO`, `SUMMEN`, `KEINZEL`, `NULLB`, `NEGATIV`,
  `PRUEF`, `MATLINES`, `ALV_DEF` als Parameter.
- Eigene Vergleichszahl aus `MBEW`, Bewertungskreis `1100`:

| Basis | Materialien | SALK3-Summe CHF |
| --- | --- | --- |
| **Alle Materialien** (Vergleichsgroesse fuer MB5L) | 65'498 | **10'937'376.40** |
| davon Disponenten `001`–`005` | 7'261 | 8'982'938.78 |

Die Einkaufsteile machen damit rund **82 %** des gesamten Lagerwerts aus — plausibel und ein
Hinweis darauf, dass Armins Abgrenzung den wesentlichen Teil trifft.

### Belegt: MB5L kann keinen freien Stichtag

Die Saldoschalter heissen `AKSALDO`, `VMSALDO` und `VJSALDO` — aktueller Saldo, Vormonat,
Vorjahr. **Damit ist aus dem Selektionsbild selbst bewiesen, dass MB5L genau diese drei
Zeitpunkte kennt und keinen frei waehlbaren Monat.** Das war vorher eine Vermutung.

Fuer Armin heisst das: „Werte dito MB5L" laesst sich als **Bewertungslogik** eins zu eins
uebernehmen (`SALK3`, aktueller Saldo), aber „per «bis Monat»" geht ueber das hinaus, was MB5L
selbst leisten kann. Die passende Referenz fuer beliebige Stichtage ist `MB5B` (Bestand zum
Buchungsdatum). Das ist kein Widerspruch in Armins Wunsch, sondern eine Praezisierung, die beim
Abgleich zu beachten ist: die Kachel kann mehr als MB5L, und MB5L taugt nur zur Pruefung des
aktuellen Stands.

Stufe 2 setzt deshalb gezielt `AKSALDO = X`, leert `VMSALDO`/`VJSALDO` und schliesst mit
`NULLB = X` Nullbestaende ein, damit beide Seiten dieselbe Grundmenge haben.

### Welche Feldeinstellungen das Ergebnis verfaelschen

Der Report gibt die Bedeutung jedes Feldes aus dem **Textpool des Programms** aus, damit sie
nicht interpretiert werden muss, und bewertet das Risiko:

| Feld | Wirkung auf den Summenvergleich |
| --- | --- |
| `SUMMEN` | **hoch** — Summenzeilen zusaetzlich in der Ausgabe. Wer alle Zeilen addiert, zaehlt doppelt. |
| `KEINZEL` | **hoch** — unterdrueckt Einzelposten; dann bleiben nur Summen und die Materialsumme ist nicht bildbar. |
| `MATLINES` | **hoch** — begrenzt die Zahl der Materialzeilen. Die Liste wird abgeschnitten, die Summe ist zu niedrig. |
| `NEGATIV` | **hoch** — schraenkt auf negative Bestaende ein. |
| `VMSALDO` / `VJSALDO` | **hoch** — zusaetzliche Wertspalte fuer Vormonat bzw. Vorjahr. |
| `BWTAR` | **besonders** — bei getrennter Bewertung fuehrt SAP je Material einen Kopfsatz UND Teilsaetze. Werden beide summiert, ist der Wert doppelt. |
| `AKSALDO` | **muss an sein**, sonst wird ein anderer Zeitpunkt verglichen, als `MBEW` fuehrt. |
| `BUKRS`, `BKLAS`, `MATNR`, `SKONT` | mittel — schraenken die Materialmenge ein. |
| `NULLB` | bewusst an, damit ein Material mit Wert ohne Menge nicht fehlt. |

### Datencheck statt Vermutung

Der Report misst zusaetzlich die beiden Konstellationen, die erfahrungsgemaess echte Differenzen
erzeugen, damit man bei einer Abweichung nicht im Nebel sucht:

1. **Getrennte Bewertung:** Zaehlt `MBEW`-Saetze mit und ohne `BWTAR`. Gibt es Teilsaetze, ist
   die eigene Summe moeglicherweise zu hoch und muss auf `BWTAR = leer` beschraenkt werden.
   Gibt es keine, ist diese Ursache ausgeschlossen.
2. **Sonderbestaende:** Zaehlt `MSKA` (Kundenauftrag), `MSPR` (Projekt) und `MSLB` (Bestand beim
   Lieferanten). `MBEW` fuehrt diese nicht; zeigt MB5L sie mit an, liegt MB5L hoeher.

Nach dem Lauf vergleicht der Report ausserdem **die Zeilenzahl von MB5L mit der Zahl der
`MBEW`-Saetze**, bevor er summiert. Mehr Zeilen deuten auf Summenzeilen oder Teilsaetze, weniger
auf eine zusaetzliche Einschraenkung. So faellt eine unbrauchbare Grundmenge auf, bevor man die
Summe fuer bare Muenze nimmt.

## 6. Messwerkzeug

`docs/abap/Z_PURCHASING_LAGERWERT_ANALYSE.abap` (read-only, SE38) beantwortet in einem Lauf:

- Welche Disponenten (`MARC.DISPO`) es gibt, mit Materialzahl je Disponent, und ob `001`–`005`
  vorhanden sind.
- Aktueller Lagerwert (`MBEW.SALK3`) und Bestandsmenge (`LBKUM`) je Disponent und
  Bewertungskreis, mit der Summe fuer `001`–`005` als direkte Vergleichszahl fuer MB5L.
- Verteilung der Preissteuerung (`VPRSV`), damit klar ist, ob `SALK3` zwingend ist.
- Ob `MBEWH` (Bewertungshistorie) Saetze fuer die letzten Monate enthaelt — die Voraussetzung
  fuer „per «bis Monat»".
- Den historischen Lagerwert je Monatsende der letzten Perioden, sofern `MBEWH` gefuellt ist.

## 7. Status

Analyse und Dokumentation. **Kein Anwendungscode, keine Datenbank, kein Deploy.** Die Umsetzung
ist bewusst noch nicht begonnen, weil Stichtagsquelle und Abgrenzung erst belegt sein muessen.
