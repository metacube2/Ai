# Konzernkosten: Schnitt nach der ersten internen Stufe. Entscheid Andreas vom 2026-09-09

Stand: 2026-09-09. Primaerquelle ist ein Transkript des Gespraechs zwischen Andreas
Stoller, Ingo Kohler und Philipp Steiger. Zitate im Folgenden sind Transkriptwortlaut,
also gesprochene Sprache und keine geschriebene Freigabe.

Fachlicher Gesamtstand der Kostenbasis: `docs/FINANCE_STANDARDKOSTEN.md`, dort Abschnitt
12. Status: `ISS-007.3` neu, betrifft ausserdem `ISS-009` und `ISS-014`.

## 1. Der Entscheid

> Aber die Logik ist ja, ist das ein externer Lieferant, dann gehe ich auf die
> Standardkosten. Ist das ein interner Lieferant, dann mache ich Cut nach der ersten
> Stufe, oder?

> Das heisst, interner Lieferant waere ja nur AG, Indien, Italien. Mehr haben wir aktuell
> gar nicht.

> Und dann machst du den Cut. Das heisst, du ziehst einfach die Standardkosten aus dem
> System, aus den drei Tabellen.

Daraus folgen genau zwei Regeln:

| Fall | Kostenbasis |
| --- | --- |
| **externer Lieferant** | lokaler Standardpreis der verkaufenden Gesellschaft, kein Versuch einer Konzernkostenquelle |
| **interner Lieferant** | Kosten der **ersten** liefernden Konzerngesellschaft, danach wird die Kette nicht weiter aufgeloest |

Intern heisst ausschliesslich Trafag AG, Trafag Controls India und Trafag Italia. Das
deckt sich mit dem Bestand: es gibt genau drei Konzernkostentabellen, siehe
`docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 3.

Praktisch heisst der Schnitt: Liefert Italien nach Frankreich, gilt Italiens Kostenwert,
auch wenn Italien die Ware selbst von Trafag AG bezogen hat.

## 2. Das tut die Berechnung heute schon

`GroupMarginCostRules.GroupStandardCost` in `Services/GroupMarginCalculator.cs` loest ueber
`ResolveDeliveringEntity` genau **eine** Stufe auf und nimmt die Kostenquelle dieser
Gesellschaft; eine Rekursion gibt es nicht. Externe Zeilen fallen durch bis zur letzten
Regel, dem lokalen Standardpreis aus der Verkaufszeile.

Der Entscheid verlangt also keinen Umbau. Er **schliesst** einen offenen Punkt, und das ist
sein Wert: bisher war unklar, ob die Kaskade tiefer greifen soll.

## 3. Was damit geschlossen ist

Der seit dem 2026-08-27 offene Fachentscheid, ob der Schweizer `MBEW-STPRS` als
Konzern-Herstellkostenbasis gilt, **sobald** die Schweiz das Material im Werk 1100 fuehrt,
unabhaengig von der liefernden Gesellschaft: **nein.** Damit gilt:

- Der Finance-Schalter `InternalSupplierCostSourceMode` bleibt auf dem Standard. Der
  Sonderwert `SwissStprsForChPlantMaterial` bleibt die eng gefasste, bewusst waehlbare
  Alternative und wird nicht zur Regel.
- Die weitergehende Lesart aus `docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 10 wird **nicht**
  als eigene Regel gebaut.
- Der Codekommentar an dieser Stelle fuehrte die Frage als „offener Fachentscheid von
  Andreas". Er ist am 2026-09-09 nachgefuehrt.

## 4. Akzeptierte Ungenauigkeit, von Andreas selbst bemessen

Es gibt einen Fall, in dem der Schnitt die Konzernmarge nachweislich zu niedrig zeigt, und
Andreas hat ihn vorab benannt und akzeptiert: **Thermostate, die Italien von Indien zukauft
und an die Tochtergesellschaften weiterverkauft.**

> Das Einzige, wo es nicht ganz zutrifft, sind Thermostate, die Italien von Indien
> zukauft. Und an die Tochtergesellschaften verkauft. Da ist die Marge etwas hoeher. Das
> geht auf eine Preisliste minus 30 Prozent.

> Aber es geht da vielleicht um 200'000, 300'000 Gruppenumsatz. Das heisst, es ist dann
> wiederum um dafuer eine komplexe Rechnung anzustellen.

Der Verrechnungspreis folgt hier also nicht den Herstellkosten, sondern der Preisliste
minus 30 Prozent. Beim Schnitt nach der ersten Stufe bleibt Italiens Handelsmarge in der
Kostenbasis stehen, die Konzernmarge dieser Zeilen ist damit zu niedrig.

**Das ist eine akzeptierte Abweichung und kein offener Punkt.** Sie ist mit rund 200'000
bis 300'000 Gruppenumsatz bemessen, und Andreas haelt eine mehrstufige Rechnung dafuer
ausdruecklich fuer unverhaeltnismaessig. Wer die Zahl spaeter in Frage stellt, findet hier
die Groessenordnung und die Begruendung, statt den Fall neu zu entdecken.

## 5. Zweite Stufe zurueckgestellt, nicht verworfen

> Und man kann dann im zweiten Schritt sagen, wir gehen eine Stufe tiefer. Das spielt eine
> ganz grosse Rolle bei Thermostaten, die ueber Italien verkauft werden. Bei den anderen
> Produkten spielt das keine so grosse Rolle.

Der Schnitt ist damit **die erste Iteration**, nicht die endgueltige Antwort. Eine zwei-
oder dreistufige Konzernkostenrechnung bleibt als moeglicher zweiter Schritt auf der Liste;
Andreas will sie im ersten Schritt bewusst nicht.

**Hinweis zur Verrechnungspreisgrundlage:** Im Gespraech ist auch ein Prozentsatz fuer den
Aufschlag bei Halbfabrikaten und Rohstofflieferungen gefallen. Ingo hat ihn am 2026-09-09
als moeglicherweise falsch gesagt gekennzeichnet, deshalb steht hier **keine Zahl**. Das
ist genau die Groesse, die eine zweite Stufe braeuchte; sie muesste vorher bestaetigt
werden. Unabhaengig davon bleibt die Messung aus Abschnitt 10 der Kostendoku gueltig: bei
den 867 gemeinsam gefuehrten Materialien liegt Italiens Stueckwert im Mittel beim
3.48-fachen des Schweizer Werts.

## 6. Das Flussdiagramm vom 2026-09-02 ist in diesem Punkt ueberholt

`docs/FINANCE_LIEFERANT_STANDARDKOSTEN_WORKFLOW_2026-09-02.svg` und die zugehoerige `.pdf`
zeigen die Variante **ohne** Schnitt, also mehrere Faelle je Stufe.

> Aber hier waere es, wenn der Cut nicht waere, da gaebe es auch verschiedene Faelle, die
> man machen kann. Aber ja, wenn du sagst Cut, genau.

> Also ohne Cut koennte man mit diesem Dokument hier noch arbeiten.

Die Dateien bleiben als datierter Stand vom 2026-09-02 stehen, sind in der Stufenfrage aber
ueberholt. Wer sie neu erzeugt, erzeugt sie aus `.tmp_tools/BuildSupplierWorkflowSvg` und
soll den Schnitt darin abbilden.

## 7. Andreas prueft selbst gegen `Sales_All`

> Ach, die Sales-All ist immer die, ist alles drin, die Sales-All, die man hat. [...] Von
> gestern, ja.

Er nimmt also den Gesamtexport und vergleicht die Standardkosten selbst. Zwei Dinge gehoeren
dazu gesagt:

- Der Stand vom 2026-09-08 traegt in den deutschen Artikelbezeichnungen noch den
  RTF-Schriftrest („MS Shell Dlg, Microsoft Sans Serif"), siehe
  `docs/STANDORT_DE_ALPHAPLAN.md` Abschnitt 8. Betroffen ist ausschliesslich die Spalte mit
  der Bezeichnung, **keine** Kosten- oder Umsatzwerte. Ein neuer Gesamtexport zieht das nach.
- Fuer den Kostenvergleich reicht der vorhandene Export. Sales Type und Trafag-Sachnummer
  stehen seit dem 2026-08-27 additiv darin, `ISS-013`.

## 8. Journal und Financial Data Lake, eigenes Thema

Die zweite Haelfte des Gespraechs betrifft nicht die Verkaufsstrecke, sondern das
Hauptbuch. Zugehoerige Doku: `docs/FINANCE_JOURNAL.md` und
`docs/FINANCE_JOURNAL_KONSOLIDIERUNG_ANDREAS_2026-09-08.md`, Status `ISS-006`.

**Minimalladung nach Andreas**, als „Minimum-Load, die man sehr gut verarbeiten kann in
einer Datenbasis": Buchungsdatum, Faelligkeitsdatum, Buchungs-ID (einmalig im System),
Konto, Kontobezeichnung, Betrag, Buchungstext und Entity. Dazu das Mapping vom lokalen
Konto auf das Konzernkonto; damit ist die Konsolidierung direkt moeglich.

**Die Soll-Haben-Darstellung ist nicht entschieden.** Andreas nennt im Gespraech beide
Varianten: zuerst

> Nicht mit Soll und Haben Betrag, sondern mit Soll und Haben Kennzeichen.

spaeter, beim Skizzieren am Bildschirm,

> Wahrscheinlich dann zwei Amounts, ne? Amount Debit, aber auch die Credit, ne?

Beides ist notiert, keine der Varianten ist damit gesetzt. Philipp Steiger soll darauf
schauen; ausserdem haengt die Antwort daran, was B1 beziehungsweise HANA im Standard
liefert, was noch zu pruefen ist.

Zwei Punkte daraus haben bereits einen Stand: das Faelligkeitsdatum ist umgesetzt und
produktiv, und das Konzernkonto-Mapping fehlt weiterhin und ist der eigentliche Blocker der
Konsolidierung.

## 9. Rohails Wunsch nach dem Deckungsbeitrag: von Andreas zurueckgestellt

Rohail hat einen Deckungsbeitrag mit Fix-Variabel-Split fuer die Marktsegmentanalyse
angefragt. Andreas hat das im Gespraech abgelehnt:

> Nein, das kriegen wir so nicht hin, das geht jetzt zu weit, das wird ein Desaster.

Das deckt sich mit dem vorhandenen offenen Punkt: kein Quellsystem liefert den
Fix-Variabel-Split, `StandardCostVariable` und `StandardCostFixed` sind vorbereitet und
bleiben bewusst leer (`docs/FINANCE_STANDARDKOSTEN.md` Abschnitt 7). Zusaetzlich haben
Andreas und Ingo festgehalten, dass solche Anforderungen zuerst zwischen Andreas, Ingo und
Rohail abzustimmen sind, statt sie einzeln ins System zu tragen; sonst entstehen Zahlen,
die niemand fachlich vertritt.

## 10. Was nach diesem Gespraech offen bleibt

| Punkt | Bei wem |
| --- | --- |
| **Zeilen ohne Lieferantenfeld.** Der Schnitt sagt nichts darueber, weil dort weder extern noch intern bekannt ist. Heute entscheidet der Treffer im Schweizer Werkstamm auf `Intern / TR_AG`; Andreas' einfachere Variante (ohne Lieferant lokale Kosten) ist als Schalter vorhanden. Betroffen sind 10'817 von 22'950 Kandidatenzeilen | Andreas |
| **Benennung der Kennzahl.** Mit dem Schnitt misst die Zahl die Marge gegen die Kosten der liefernden Gesellschaft, nicht gegen Konzern-Herstellkosten. Das gehoert in die Fachfreigabe `ISS-009`, sonst verspricht der Titel mehr als die Zahl haelt | Andreas / Ingo |
| **Soll-Haben-Darstellung im Journal** und die Frage, was B1/HANA im Standard liefert | Andreas / Philipp |
| **Verrechnungspreisgrundlage**, falls die zweite Stufe je aufgerufen wird, siehe Abschnitt 5 | Andreas |
