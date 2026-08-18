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
