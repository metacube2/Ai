# Plan: T161V ATPCONFMRP fuer F / NB / 1100 in T76/100 voruebergehend leeren (Test Fabio)

Stand: 2026-11-18 12:12 (Phase 2 ausgefuehrt 12:03 bis 12:07, Abschnitt 5a: Feld seit 12:06:00 leer, Auftrag T76K912926
lokal und offen; **Rueckbau auf X steht aus**, Abschnitt 5); davor Phase 1, nur gelesen (11:56 bis 12:02), Agent sap-t76. Auftrag Ingo 18.11.2026 im Chat, Wunsch Fabio Palma.
Freigabe Ingos gilt genau fuer diese eine Feldaenderung; Phase 2 laeuft im selben Lauf, weil Phase 1 den Auftrag in jedem
Punkt bestaetigt hat (Abschnitt 2).

Ablageort: `saptasks/ic_preisfindung/`, weil es zum selben Vorhaben gehoert (IC Trafag AG 1100 an Trafag AT 1200,
Bestellart NB mit Lieferwerk 1100, Pruefregel IC; der juengste Auftrag zu diesem Schluessel heisst „Trafag AT IC: Ablauf
Verfuegbakreispruefung Bestellung“, T76K912888). Das Thema ist die Verfuegbarkeitspruefung, nicht die Preisfindung; deshalb
eigene Dateien mit `T161V` im Namen statt eines Abschnitts im V1-Plan.

## 1. Was verlangt ist

Tabelle T161V, Schluessel Belegtyp F, Belegart NB, Lieferwerk 1100, Feld ATPCONFMRP („ATP Best. MRP“) von X auf leer.
Pruefregel IC, Pruefgruppe 02 und Gesamtwiederbeschaffungszeit 70 Tage bleiben. Nach dem Test wieder X (auf Zuruf Ingos,
nicht Teil dieses Laufs).

## 2. Ist-Stand T76/100 (alles per RFC gelesen, 18.11.2026 11:56 bis 12:02)

| Punkt | Befund | Beleg |
| --- | --- | --- |
| Tabelle | T161V, transparent, mandantenabhaengig, Auslieferungsklasse G | DD02L `TABCLASS TRANSP`, `CLIDEP X`, `CONTFLAG G` |
| Schluessel | MANDT, BSTYP, BSART, RESWK; das Lieferwerk heisst `RESWK` | DD03L T161V, `KEYFLAG X` |
| Feld | `ATPCONFMRP`, CHAR 1, Position 20, Datenelement ATPCONFMRP „ATP-Bestaetigungen relevant fuer MRP“, Bildtext „ATP Best. MRP“ | DD03L, DD04T (D) |
| Zielzeile | genau **eine** Zeile F / NB / 1100: LFART NL, PRREG **IC**, MEVST X, LFART1 NL, LFART2 NLCC, REVFE B, **ATPCONFMRP X**, alle uebrigen Felder leer | `T161V_F_NB_1100_VORHER_2026-11-18.txt` Abschnitt 1 |
| Ganze Tabelle | 11 Zeilen, alle BSTYP F; mit Lieferwerk 1100: UB und NB; mit Belegart NB: 0001 und 1100; ATPCONFMRP X nur bei NB/1100 | VORHER-Datei Abschnitt 2 |
| Pflegesicht | `VV_161V_VF` „Umlagerungsdaten“ (Sicht auf T161V mit fester Bedingung `BSTYP = 'F'`), IMG-Aktivitaet `SIMG_OLME_VV_161V_VF`, Aufruf ueber SM30; enthaelt das Feld ATPCONFMRP | DD25T, DD27S, DD28S (`BSTYP EQ 'F'`), CUS_ACTOBJ (`OBJECTNAME VV_161V_VF`, `TCODE SM30`); fruehere E071K-Eintraege `MASTERNAME VV_161V_VF` |
| Auftragspflicht | Mandant 100: Kategorie T, `CCCORACTIV 1` (Aenderungen werden aufgezeichnet), also Abfrage Customizing-Auftrag | T000 |
| Sperren | Schluessel `100FNB  1100` nur in T76K912180/181 (MEEY_HAST, 30.09.2026, **R**). Kein offener Auftrag mit diesem Schluessel | E071K `OBJNAME = 'T161V'`, E070 |
| Nachbarn | T76K912888/889 (MEEY_HAST, 11.11.2026, R, Ziel P76, **Mandant 090**) mit `090FNB  1100`; offene Aufgabe T76K911043 (LAA, 2025) mit `100BNB  1100` (BSTYP B, anderer Schluessel) | E071K, E070, E070C, E07T |
| Pruefregel/Pruefgruppe | T441V 02 / IC vorhanden (wird nicht angefasst) | VORHER-Datei Abschnitt 3 |

Haltebedingungen des Auftrags, alle **nicht** erfuellt: genau eine Zeile (ja); ATPCONFMRP heute X (ja); Feld und Tabelle wie
beschrieben (ja, Lieferwerk = RESWK); kein fremder offener Auftrag mit dem Schluessel; die Sicht aendert ein einzelnes
Ankreuzfeld der Zeile.

Nicht gelesen: Mandant 090 und P76. Die Gesamtwiederbeschaffungszeit und die Pruefgruppe sind Felder des Materialstamms je
Werk (DD03L MARC: `WZEIT`, DEC 3, Datenelement WZEIT „Gesamtwiederbeschaffungszeit (Arbeitstage)“; `MTVFP`, CHAR 2,
Datenelement MTVFP „Pruefgruppe fuer Verfuegbarkeitspruefung“; gelesen 12:10). Fuer welches Material Fabio testet, steht
nicht im Auftrag; die Werte 70 und 02 sind deshalb an keinem Material nachgelesen und werden nicht angefasst.

## 3. Schritte Phase 2 (genau diese, nichts dazu)

Rahmen: kein Passwort; GUI nur in eigener Sitzung (`SapGuiT76Hilfe.vbs neu`, nur die gemeldete Id, Ingos `ses[0]` nie);
Ausgabe von cscript in Dateien; bei E- oder W-Meldung, Sperrmeldung, Anzeigemodus oder unerwartetem Fenster: F12, verwerfen,
anhalten und berichten. Wird ein Befehl abgelehnt: anhalten, nicht variieren.

| Nr | Schritt | Erwartet |
| --- | --- | --- |
| 1 | `info`, dann `neu`, dann `info` | vorher nur `ses[0]`, nachher genau eine Sitzung mehr mit neuer Id |
| 2 | In der eigenen Sitzung `/nSM30`, Sicht `VV_161V_VF`, „Pflegen“ | Bild „Sicht Umlagerungsdaten aendern“ im Aendern-Modus, Liste mit den 11 Zeilen |
| 3 | Zeile bestimmen: derselbe Zeilenindex traegt Belegart `NB`, Lieferwerk `1100` und den gesetzten Haken „ATP Best. MRP“ | sonst anhalten (es gibt auch NB / 0001 ohne Haken) |
| 4 | Ganze Zeile am Bild auslesen und gegen VORHER vergleichen (LFART NL, PRREG IC, MEVST X, LFART1 NL, LFART2 NLCC, REVFE B, ATPCONFMRP X) | gleich |
| 5 | Nur das Ankreuzfeld ATPCONFMRP dieser Zeile auf leer setzen; danach die Zeile und alle sichtbaren Zeilen noch einmal lesen | nur dieser Haken anders |
| 6 | Sichern (Strg+S). Dialog „Abfrage Customizing-Auftrag“: Vorschlag **nicht** uebernehmen, „Auftrag anlegen“ (F8) | Dialog „Auftrag anlegen“ |
| 7 | Kurzbeschreibung `T161V ATPCONFMRP NB/1100 Test Fabio 18.11. NICHT FREIGEBEN` (58 Zeichen, ohne Umlaut; der Vorschlag aus dem Auftrag hat 66 Zeichen, erlaubt sind 60, LEARNINGS 2026-10-29). **Zielsystem leeren** (lokaler Auftrag, LEARNINGS 2026-10-20: Test-Customizing nie in einen Auftrag mit Ziel P76; Muster T76K912750). Sichern, Nummer notieren, im Abfragedialog ausdruecklich diese Nummer bestaetigen | Hinweis „wird nicht transportiert“ o. ae.; danach „Daten wurden gesichert“ |
| 8 | Uhrzeit mit `Get-Date` ablesen | – |
| 9 | Auftrag **nicht** freigeben. Eigene Sitzung schliessen (`zu`), `info`: nur noch Ingos Sitzung | – |

Begruendung zu Schritt 7 (Ziel leer): Der Auftrag Ingos nennt kein Ziel. Der Schluessel F/NB/1100 wird fuer P76 in Mandant 090
gepflegt und von dort transportiert (T76K912888/889 vom 11.11.2026). Ein Testauftrag aus Mandant 100 mit Ziel P76 koennte
bei einer versehentlichen Freigabe die ganze Zeile nach P76 tragen. Ein lokaler Auftrag ohne Ziel schliesst das aus.

## 4. Rueckleseprobe (RFC, nach Schritt 7)

1. Zielzeile mit vollem Schluessel: `table-read T161V --where "BSTYP = 'F' AND BSART = 'NB' AND RESWK = '1100'"` mit allen 33
   Feldern. Erwartet: `100,F,NB,1100,NL,IC,,,,X,,NL,NLCC,,B,,,,,,,,,,,,,,,,,,` (nur die Stelle ATPCONFMRP ist leer statt X).
2. Ganze Tabelle mit demselben Befehl wie vorher (`BSTYP LIKE '%'`, `--rowcount 5000 --format csv`): weiter **11 Zeilen**;
   Vergleich gegen die Vorher-Ausgabe zeilenweise, Exitcode des Vergleichs selbst ausgewertet: genau eine Zeile anders.
3. T441V 02 / IC: gleich wie vorher.
4. Auftrag: `E070` neuer Auftrag W, D, `TARSYSTEM` leer, KOI; Aufgabe darunter; `E070C` Mandant 100; `E071` der Aufgabe nur
   `VDAT VV_161V_VF`; `E071K` genau ein Schluessel `T161V` / `100FNB  1100`. Steht dort mehr, im Bericht nennen.
5. Stand ablegen: `T161V_F_NB_1100_NACHHER_2026-11-18.txt`.

## 5. Rueckbau (spaeter, nur auf Zuruf Ingos)

SM30, Sicht `VV_161V_VF`, Zeile NB / 1100 (auf der ersten Seite die dritte Zeile; vor dem Setzen wieder Belegart, Lieferwerk
und Haken aus demselben Zeilenindex lesen), Haken „ATP Best. MRP“ wieder setzen, sichern. Kommt der Auftragsdialog, den
lokalen Auftrag T76K912926 ausdruecklich setzen. *Vermutung:* Der Dialog bleibt aus, weil Sicht und Schluessel schon in der
eigenen offenen Aufgabe T76K912927 stehen (so am 11.11. bei V/06 Z002, `PLAN_V1_CUSTOMIZING_2026-11-11.md` 6.4 Schritt 5);
dann nach dem Sichern in E071K pruefen, dass der Schluessel `100FNB  1100` weiter nur in T76K912927 steht und kein anderer
offener Auftrag ihn bekommen hat. Rueckleseprobe: Zielzeile und ganze Tabelle zeichengleich mit
`T161V_F_NB_1100_VORHER_2026-11-18.txt` (ATPCONFMRP = X, 11 Zeilen). Der lokale Auftrag wird nie freigegeben; nach dem
Rueckbau kann Ingo ihn loeschen oder stehen lassen.

## 5a. Ausfuehrung Phase 2 (2026-11-18, 12:03 bis 12:07)

**Ergebnis: ausgefuehrt und rueckgelesen. ATPCONFMRP fuer F / NB / 1100 ist in T76/100 seit 12:06:00 leer** (mit `Get-Date`
abgelesen direkt nach „Daten wurden gesichert“; der Klick dazu lag zwischen 12:05:47 und 12:06:00; als Stuetze E070-AS4TIME
der Aufgabe: 12:05:58 Serverzeit). Auftrag **T76K912926** (Customizing, lokal ohne Ziel, offen, nicht
freigegeben), Aufgabe T76K912927. P76 und Mandant 090 nicht gelesen und nicht beruehrt. Kein Befehl abgelehnt, kein
Anmeldebild, keine E-Meldung.

| Nr | Ist |
| --- | --- |
| 1 | `info` vorher: nur `ses[0]` (SESSION_MANAGER, Ingo). `neu`: „vorher=1 nachher=2“, eigene Sitzung Index 1, Id `/app/con[0]/ses[2]`. Nur dort gearbeitet |
| 2 | `/nSM30`, `VV_161V_VF`, „Pflegen“: Bild „Sicht "Umlagerungsdaten" aendern: Uebersicht“ (SAPL0ME6, Dynpro 180), ohne Rueckfrage, „Eintrag 1 von 11“; die Tabelle zeigt drei Zeilen je Seite |
| 3 | Zeile 2 der ersten Seite: Belegart NB, Lieferwerk 1100 (Trafag AG), Haken „ATP Best. MRP“ gesetzt und aenderbar. Zeile 1 ist NB / 0001 ohne Haken |
| 4 | Am Bild (Feldnamen der Sicht; Bezeichnungen laut DD04T, gelesen 12:10): LFART NL, PRREG IC (Text „Intercompany“), MEVST gesetzt („Versandterminierung durchfuehren“), MERFP leer („Routenfahrplan fuer die Terminierung benutzen“), LFART1 NL, LFART2 NLCC, LFCON leer, REVFE B („Regel zur Uebernahme der ATP-Ergebnisse im Einkauf“), REQPRF leer, ATPCONFMRP gesetzt, ACTIVATE_ADV_PAL_IND leer („Kontingentierungspruefung im erweiterten ATP aktivieren“): gleich VORHER. Die Sicht zeigt 12 der 33 Tabellenfelder (DD27S); die uebrigen (z. B. LFRET) sind nur per RFC verglichen |
| 5 | Nur `chkV_161V-ATPCONFMRP[15,2]` auf leer; Rueckmessung aller drei sichtbaren Zeilen: einzig dieser Haken anders |
| 6 | Sichern: Dialog „Abfrage Customizing-Auftrag“ fuer `VV_161V_VF`, Auftragsfeld **leer** vorbelegt; „Auftrag anlegen“ (F8) |
| 7 | Dialog „Auftrag anlegen“: Customizing-Auftrag, Inhaber KOI, Quellmandant 100, Ziel **P76 vorbelegt** und geleert, Text 58 Zeichen zurueckgelesen. Neue Nummer T76K912926; vor dem Bestaetigen in E070 gelesen (W, D, Ziel leer). Nach „Weiter“ die erwartete Warnung „Lokaler Auftrag T76K912926 wird nicht transportiert“, bestaetigt; Statuszeile „Daten wurden gesichert“ (S) |
| 8 | 12:06:00 (`Get-Date`); Serverzeit der Aufgabe 12:05:58 |
| 9 | Auftrag nicht freigegeben. `zu 1`: „Sitzungen jetzt=1“, `info`: nur `ses[0]` SESSION_MANAGER |

Rueckleseprobe (RFC, 12:06:17), Einzelheiten in `T161V_F_NB_1100_NACHHER_2026-11-18.txt`:

- Zielzeile: `100,F,NB,1100,NL,IC,,,,X,,NL,NLCC,,B,,,,,,,,,,,,,,,,,,`, wie in Abschnitt 4 erwartet.
- Ganze Tabelle: 11 Zeilen vorher und nachher; `Compare-Object` zeigt genau eine geaenderte Zeile, darin nur ATPCONFMRP.
- T441V 02 / IC: zeichengleich.
- Auftrag: E070 T76K912926 W/D/Ziel leer/KOI, T76K912927 Q/D; E070C beide 100; E071 nur `VDAT VV_161V_VF`; E071K genau ein
  Schluessel `T161V` / `100FNB  1100`. Nichts Fremdes.

Abweichung vom Plan: keine. Die Warnung erschien erst beim Bestaetigen des Abfragedialogs, nicht beim Sichern des Auftrags.

## 6. Wirkung waehrend des Testfensters

Die Zeile gilt je Belegart und Lieferwerk, nicht je Bestellung oder Benutzer: Ab dem Sichern wirkt der leere Haken in T76/100
fuer alle NB-Bestellungen mit Lieferwerk 1100, auch fuer andere Tester (so auch im Wortlaut Fabios). P76 und Mandant 090 sind
nicht betroffen.
