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

# Nachtrag 2026-11-24 12:45: Bruecke lokale Artikelnummer <-> Trafag-Nummer (KNA1, KNMT, VBAK, VBAP)

## Auftrag und Lesung

Ingo „ja prüfe das“ auf das Angebot, in P76 „KNMT, VBAP-KDMAT der IC-Kunden“ nur lesend anzusehen. Nur KNA1, KNMT, VBAK und
VBAP gelesen. Eigene Sitzung `/app/con[1]/ses[1]`, SE16 mit Feldauswahl (Funktionscode `FELD`, `btn[14]`, ankreuzen,
Uebernehmen) und `%PC`, 12:44 bis 12:50, danach geschlossen. Keine Benutzerfelder in der Ausgabe, nichts geaendert.

| Tabelle | Ausgabefelder | Selektion in SE16 | Treffer | Filter danach | Datei, Zeilen |
| --- | --- | --- | --- | --- | --- |
| KNA1 | KUNNR LAND1 NAME1 KTOKD VBUND | keine | 17'805 | VBUND gefuellt (0 Treffer) oder NAME1 enthaelt TRAFAG (56) | `KNA1_IC.csv`, 56 |
| KNMT | VKORG VTWEG KUNNR MATNR KDMAT POSTX | keine (ganze Tabelle) | 22'378 | keiner, Spalte `IC_KUNDE` = X fuer die 56 | `KNMT.csv`, 22'378 |
| VBAK | VBELN ERDAT AUDAT VBTYP AUART KUNNR BSTNK | AUDAT ab 01.07.2022 | 73'168 | KUNNR aus `KNA1_IC`, ERDAT ab 2023-01-01 | `VBAK_IC.csv`, 10'687 |
| VBAP | VBELN POSNR MATNR KDMAT ARKTX | VBELN aus `VBAK_IC` (Mehrfachselektion per Datei) | 24'466 | keiner, Kopfdaten dazugemischt | `VBAP_IC.csv`, 24'466 |

- **VBUND ist in KNA1 nirgends gefuellt.** Die IC-Kunden sind deshalb nur ueber den Namen bestimmt. `HINWEIS_AGENT` in
  `KNA1_IC.csv` ist die Einschaetzung des Agenten, nicht aus SAP: Testkunde/Muster (3), Fremdkunde mit Trafag im Namen
  (Sabre Engines, MASTER GRID, LCI), Trafag AG / CH intern (alle LAND1 CH).
- VBAK bietet in SE16 nur VBELN, AUDAT, VBTYP, AUART als Selektionsfelder an, KUNNR und ERDAT nicht. Gelesen wurde darum
  AUDAT ab 01.07.2022 fuer alle Kunden, gefiltert in Python. Im Abzug gibt es keinen Beleg mit AUDAT vor 2023 und ERDAT ab
  2023; Belege mit AUDAT vor dem 01.07.2022 und ERDAT ab 2023 sind nicht gelesen (vermutlich keine).
- `VBAK_IC` enthaelt alle Belegtypen: VBTYP C (Auftrag) 8'955, B (Angebot) 1'582, K (Gutschriftsanforderung) 138, L 8, A 3,
  I 1. `VBAP_IC` traegt VBTYP, AUART, KUNNR, LAND1 und ERDAT des Kopfs mit; 7 Koepfe haben keine Position.
- Format wie oben: UTF-8, Semikolon, CRLF, MATNR, KUNNR und VBELN in externer Form, POSNR ohne fuehrende Nullen, Datum
  JJJJ-MM-TT. Ersatzzeichen 0. Neu erzeugen mit `umwandeln_bruecke.py <Ordner mit Rohexporten>`.

## Befund

- KNMT gehoert fast ganz Fremdkunden: 15'061 von 22'378 Eintraegen haben KDMAT. Bei den IC-Kunden sind es 4'854 Eintraege
  (alle VKORG 1100, VTWEG 01), aber nur **200 mit KDMAT**; 2'596 tragen nur einen Positionstext (POSTX), 2'098 sind ganz
  leer. 21 Kombinationen Kunde + KDMAT zeigen auf mehrere MATNR.
- VBAP der IC-Auftraege: 896 von 24'466 Positionen haben KDMAT (845 davon in Auftraegen VBTYP C). Davon stehen 631 auf
  Sammel-/Dummymaterial (`*99999`, `STS999`) und 485 enthalten Preistexte wie `EUR 160.00` (Reparaturen RS99999). Eine echte
  Nummernbruecke (kein Dummy, kein Preistext, KDMAT ungleich MATNR) bleibt bei **264 Positionen**, 153 davon stehen genauso
  in KNMT.
- KDMAT wird also selten als lokale Artikelnummer gefuehrt; haeufig steht dort eine Bestell-, Reklamations- oder QM-Nummer
  (z. B. `QM113920`, `CLAIM 11_2024`, `B-SAMPLE`). Als allgemeine Bruecke taugt es nur teilweise [V].
