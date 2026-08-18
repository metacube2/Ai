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

1. **Messen** mit dem Report aus Abschnitt 6: Existiert `MBEWH`, ist sie gefuellt, welche
   Disponenten gibt es, wie hoch ist der Lagerwert je Disponent heute und per Monatsende?
2. **Gegen MB5L abgleichen.** Armin oder Ingo laesst MB5L fuer denselben Stichtag und
   Bewertungskreis laufen. Erst wenn die Zahl des Reports mit MB5L uebereinstimmt, ist die
   Grundlage belastbar. Ohne diesen Abgleich keine Kachel.
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
| Sonderbestaende einbeziehen? | **Offen**, Frage an Armin, nirgends dokumentiert. |
| Disponent `004` Betriebsmaterial einbeziehen? | **Offen**, Frage an Armin. |

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
