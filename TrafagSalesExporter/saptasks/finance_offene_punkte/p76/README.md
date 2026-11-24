# P76-Abzug Materialstamm fuer die offenen Finance-Punkte (2026-11-24)

## Auftrag

Ingo am 24.11.2026: „SAP P und T offen, du kannst loslegen“, auf das Angebot „P76 lesen: MARA und MBEW fuer die
H7-Materialien, MARA-ZZTYP fuer UK und DE“. Die Hauptsitzung hat den Umfang auf alle Materialien sowie MAKT und MARC
(Werk 1100) fuer den Werkstamm-Abgleich gefasst. P76 nur lesend.

## Lesung

- P76, Mandant 100, eigene Sitzung `/app/con[1]/ses[1]` (geoeffnet mit `SapGuiP76Hilfe.vbs neu`, Ingos `ses[0]` unberuehrt),
  SE16, Download `%PC` „Text mit Tabulatoren“; danach mit `P76SitzungAufraeumen.vbs` und `P76SitzungSchliessen.vbs`
  geschlossen. Kein Passwort, kein RFC, nichts geaendert.
- Lesezeitpunkt: 24.11.2026 11:50 bis 12:04 (MBEW 11:50, MARC und MAKT 11:52 bis 11:54, MARA 12:03); danach eine Kontrolle der Feldauswahl (unten).
- Vorab DD03L (`TABNAME MARA`, `FIELDNAME ZZ*`): `ZZTYP` (Position 0354) und `ZZTYP_F4` (0355) gibt es in P76, beide
  Datenelement `ZZTYP`, CHAR 60.
- MARA hat 324 Felder; bei Listbreite 1023 zeigt SE16 nur die ersten 85, die Z-Felder fehlen. Deshalb fuer MARA die
  Feldauswahl (Einstellungen > Listaufbereitung > Feldauswahl, Funktionscode `FELD`): im Dialog zuerst alle Markierungen
  loeschen (Knopf `btn[14]`; Tooltip nicht gelesen, Wirkung aus dem Ergebnis erschlossen: danach „7 von 7“ Feldern), dann
  MATNR, LVORM, MTART, MATKL, MEINS, ZZTYP, ZZTYP_F4 ankreuzen, Uebernehmen, F8 und `%PC` aus demselben Selektionsbild. Im
  Ausgangszustand sind alle Felder markiert. Die Auswahl bleibt nicht beim Benutzer stehen: in einer danach neu geoeffneten
  Sitzung zeigte MARA wieder „85 von 324“ (nach 12:04, Testabzug MAX=1, geloescht). Den Funktionscode `FELD` lieferte RSMPTEXTS
  (SAPLSETB, „Feldauswahl...“), gelesen in derselben P76-Sitzung, weil `SapGuiP76Hilfe.vbs select` Menues nicht waehlen kann.
- Benutzerfelder: Der MARA-Hauptabzug enthaelt kein ERNAM/AENAM. Zwei Testabzuege mit je 5 Zeilen vor der wirksamen
  Feldauswahl und der Testabzug MAX=1 enthielten sie; sie lagen nur im Scratchpad und sind geloescht.
- SE16 kann beim ersten Aufruf einer Tabelle Selektionsreports generieren (Standard, keine Daten; LEARNINGS 17.11.).
- MBEW, MARC, MAKT ohne Feldauswahl (die gebrauchten Felder liegen vorne); die Rohexporte bleiben im Scratchpad der Sitzung,
  hier stehen nur die verlangten Spalten.

| Datei | Tabelle | Filter in SE16 | Treffer SE16 | Zeilen CSV | Schluessel eindeutig |
| --- | --- | --- | --- | --- | --- |
| `MARA.csv` | MARA | keiner (alle Materialien) | 68'840 | 68'840 | MATNR ja |
| `MBEW_1100.csv` | MBEW | BWKEY 1100 | 66'587 | 66'587 | MATNR ja |
| `MARC_1100.csv` | MARC | WERKS 1100 | 66'637 | 66'637 | MATNR ja |
| `MAKT.csv` | MAKT | SPRAS DE und EN (zwei Laeufe) | 68'839 + 49'549 | 118'388 | MATNR+SPRAS ja |

MBEW: Filter „BWTAR leer“ laesst sich in SE16 nicht setzen (leer heisst keine Einschraenkung); nachgefiltert, verworfen 0
Zeilen (keine getrennte Bewertung in 1100). Alle MATNR aus MBEW, MARC und MAKT stehen in MARA. Ersatzzeichen beim Lesen: 0.
Spaltenprobe (kein Versatz): MARC WERKS nur 1100, BESKZ E 53'558 / F 10'906 / leer 2'011 / X 162 (F gleich der Zaehlung in
`saptasks/zukauf_stuecklisten/NOTES.md` vom selben Morgen), SOBSL leer / 50 / 30, LVORM leer / X; MBEW VPRSV nur S;
MARA 8 Materialarten, LVORM leer 43'715 / X 25'125.

## Format

- UTF-8 ohne BOM, Trennzeichen Semikolon, Kopfzeile, Zeilenende CRLF. Felder mit Semikolon oder Anfuehrungszeichen stehen in
  Anfuehrungszeichen (CSV-Regel, betrifft rund 8'000 MAKT-Texte); mit einem CSV-Leser lesen, nicht mit `split(';')`.
- MATNR in externer Form: SE16 zeigt numerische Nummern intern mit fuehrenden Nullen (`000000000000055441`); hier wie in MM03
  ohne Nullen (`55441`). Alphanumerische Nummern unveraendert.
- MAKT-SPRAS steht als interner Schluessel `D` bzw. `E` (Selektion war DE/EN).
- Zahlen (STPRS, VERPR, PEINH, LBKUM) normalisiert: Dezimalpunkt, kein Tausendertrenner. **Formatfalle im Rohexport:** SE16
  schreibt Dezimalpunkt und Tausender-Komma (`PEINH 1,000` = 1000, `LBKUM 4,471.000` = 4471), Minus hinten.
- STPRS/VERPR gelten je PEINH Einheiten der Basismengeneinheit MARA-MEINS. Waehrung nicht gelesen (Bewertungskreis 1100,
  Vermutung CHF).
- Leere Felder sind leer; LVORM `X` = Loeschvormerkung (in MARA auf Mandantenebene, in MARC auf Werksebene).

## Plausibilitaet

- ZZTYP gefuellt 41'791 von 68'840, ZZTYP_F4 41'816 (25 Materialien nur mit ZZTYP_F4). Je Materialart: FERT 38'825 von
  38'926, HALB 2'248 von 29'073, NGRP 688 von 712, HAWA 29 von 72. Ohne Loeschvormerkung 43'715 Materialien, davon 25'151 mit
  ZZTYP.
- ZZTYP_F4 ist der Typencode ohne Leerzeichen (55441: `8258 85 2317 32 0000 0000 19 83 F2 SW` gegen
  `825885231732000000001983F2SW`); gleich sind beide nur bei 199 Materialien.
- Beispiel 55441: FERT, ST, ZZTYP wie in T76; MBEW S, STPRS 33.89 je 1; MARC BESKZ E.
- Beispiel C17389: STPRS 1.58 je 1, BESKZ F, deckt sich mit `saptasks/zukauf_stuecklisten/NOTES.md` (gleicher Tag).
- H70313 (EN10204-3.1 Abnahmepruefzeugnis): MEINS ST, MBEW VPRSV S, STPRS 0.01, VERPR 0.01, PEINH 1, LBKUM 4471; MARC BESKZ E.
  Der Standardpreis ist also wirklich 0.01 je 1 ST, keine Preiseinheit dahinter.
- H7-Materialien in MBEW 1100: 320 (MARA 321), alle VPRSV S, MEINS ST; PEINH 1: 285, 100: 20, 10: 10, 1000: 5.

## Neu erzeugen

`umwandeln.py <Ordner mit Rohexporten>` (Python mit `-I`) liest `mara_roh.txt`, `mbew_1100_roh.txt`, `marc_1100_roh.txt`,
`makt_de_roh.txt`, `makt_en_roh.txt` und schreibt die vier CSV neu.
