# FIX 30 bis FIX 34 in `Z_LO03_MD04_BATCH`: offene Mengenkontraktmenge, offene Bestellmenge, Verbrauch (2026-11-25)

Stand: 2026-11-26 (Abschnitt 8: erster P76-Lauf 26.11. 06:00 mit FIX 30 bis 34 fertig, Werte in `ZMD04_CALC` gegen P76-Rohdaten nachgerechnet und gleich; B64344 = 7'125 wegen neuem Kontrakt 46010673 vom 25.11.); davor 2026-11-25 14:40 (Abschnitt 7: `T76K912950` in P76 importiert 13:49:32 RC 0, Job und Variante gelesen); davor 13:15 (Abschnitt 6: FIX 32 bis 34 im selben Auftrag; der Dateiname nennt nur FIX 30/31, weil die Datei vorher angelegt wurde). **In T76/100 aktiv, Auftrag `T76K912950` (Aufgabe `T76K912951`), nicht freigegeben. P76 nicht beruehrt.**
Anlass und Ursache: `BEFUND_MKMNG_ABRUFE_2026-11-25.md` (Meldung Armin, B64344 in ZLO03 8'456 statt 2'125 wie ME3M).
Auftrag Ingo 25.11.: „ja einbauen und testen im t. punkt 3 mach ein schalter“. Entscheid Armin 25.11. (Mail an Ingo):
abgelaufene Kontrakte mit Restmenge zaehlen mit, „da wir eine offene Verpflichtung gegenueber dem Lieferanten haben“.

## 1. Was geaendert ist

Quelltext vorher `Z_LO03_MD04_BATCH_T76_2026-11-25.abap` (1'265 Zeilen), nachher `Z_LO03_MD04_BATCH_FIX31_2026-11-25.abap`
(1'294 Zeilen). Nur die mit `*** FIX 30` und `*** FIX 31` markierten Stellen:

- **FIX 30:** Typ `ty_ekab` um `ebeln`, `ebelp` ergaenzt, `EKAB` wird mit `konnr, ktpnr, menge, ebeln, ebelp` gelesen. Damit ist
  jeder Abruf eine eigene Zeile; `FOR ALL ENTRIES` fasst gleiche Abrufmengen nicht mehr zusammen. `calc_mkmng` unveraendert.
- **FIX 31:** neuer Parameter `P_KTABG` (Ankreuzfeld, Selektionstext „Abgelaufene MK mit Restmenge“, **Vorschlag an**). Angekreuzt
  entfaellt die Pruefung des Laufzeitendes (`k~kdate >= @gv_kdate_ab`, `gv_kdate_ab` dann `00000000`); Beginn der Laufzeit,
  Loeschkennzeichen an Kopf und Position, Endlieferung und Statistikkennzeichen werden weiter geprueft. Nicht angekreuzt gilt das
  Verhalten vor FIX 31 (nur heute gueltige Kontrakte). Der Schalter wirkt nur zusammen mit `P_KTGUE`.

Nicht geaendert: `Z_ZLO03_TURBO2`, Jobvarianten. *(Ueberholt 13:15: hier stand, die SELECTs auf `MDBS`, `MSEG`, `AFPO` seien nicht geaendert; sie sind es seit FIX 32 bis 34, Abschnitt 6.)*

## 2. Test in T76 (Agent sap-t76, von Claude gegen T76 nachgeprueft)

Selektion `S_MATNR` B64000 bis B64999, Werk 1100, Modus „Vollstaendig“; der Job rechnet dazu die Komponenten dieser Materialien,
zusammen 500 Zeilen. T76-Daten reichen bis etwa Mai 2026, deshalb andere Zahlen als in P76.

| B64344 | MKMNG |
| --- | --- |
| alter Stand (Testlauf) | 9'032 |
| FIX 30, `P_KTABG` aus | 5'000 |
| FIX 30 + 31, `P_KTABG` an (Vorschlag) | **5'005** |

- **Gegenrechnung Claude** aus `EKPO`/`EKKO`/`EKAB` (RFC, unabhaengig vom Job) fuer alle 20 Materialien B64* mit zaehlender
  Kontraktposition: 16 vom Job gerechnet, alle 16 gleich der Erwartung, und zwar in allen drei Staenden (alt mit dem Fehler
  nachgebildet, neu, neu mit Schalter). Die 4 uebrigen haben eine Loeschvormerkung; der Job rechnet sie nicht.
- FIX 30 aendert 23 von 500 `MKMNG`, alle nach unten (Summe 1'042'315 -> 702'656). Der Schalter hebt danach 11 von 500 wieder an
  (Summe 714'913). Alle uebrigen Spalten der Liste sind in allen Laeufen gleich.
- Echter Lauf 12:13 mit den Vorschlagswerten: „Modus F: 500 Saetze geschrieben“; `ZMD04_CALC` B64344 = 5'005, `OMKWR` 77'127.05.
- Quelltext aus T76 zurueckgelesen: zeichengleich mit der Zieldatei. Syntaxpruefung ohne Meldung, ST22 ohne Laufzeitfehler.
- Belege: `fix30_31_belege/` (Listen vorher, nachher, nachher mit Schalter; geaenderte Materialien). Die Listen sind aus zwei
  Bildschirmhaelften zusammengesetzt, weil die Testliste des Reports breiter ist als das Fenster; die Werte „nachher“ sind
  zusaetzlich gegen `ZMD04_CALC` bestaetigt (500 von 500).

## 3. Erwartung fuer P76

Mit Armins ME3M-Liste vom 25.11.: B64344 = 5 (46009174, abgelaufen 31.12.2025) + 0 (46009844) + 2'120 (46010268) = **2'125**.
Wirksam in ZLO03 erst nach dem naechsten Lauf des Jobs nach dem Import. *(Ueberholt 26.11.: P76 zeigt 7'125, weil am 25.11. der Kontrakt 46010673 mit 5'000 dazukam; 2'125 gilt fuer die drei alten Kontrakte, Abschnitt 8.)*

## 4. Offen

1. ~~**Ingo:** `T76K912950` freigeben und nach P76 importieren.~~ *Erledigt: freigegeben 13:48, in P76 importiert 13:49:32, RC 0 (Abschnitt 7).*
2. **Vor dem Import pruefen (P76, braucht Ingos Freigabe zum Lesen):** dass in P76 der Stand vom 04.08.2026 laeuft
   (`T76K911441`). Der Transport bringt das ganze Programm; ein abweichender P76-Stand wuerde ueberschrieben. Nicht geprueft.
3. **Nach dem Import:** Selektionsbild der Jobvariante in P76 ansehen. Eine bestehende Variante kennt `P_KTABG` nicht; dass sie dann
   den Vorschlagswert „an“ nimmt, ist eine Annahme und nicht geprueft. Danach ZLO03 Top-Down 61227: B64344 muss 2'125 zeigen.
4. **Antwort an Armin** (Ursache, Wert nach dem naechsten Lauf, Hinweis auf weitere betroffene Materialien).
5. ~~Weitere Felder des Jobs: Befund Abschnitt 5 und 7, Entscheid Ingo.~~ *Entschieden und gebaut, Abschnitt 6.*

## 5. Nebenwirkungen des Tests in T76

- `ZMD04_CALC` T76: 500 Zeilen neu gerechnet (Sicherung der Tabelle vor dem Lauf nur im Scratchpad der Sitzung, nicht im Repo).
- `ZMD04_CONTROL` T76: drei Zeilen mit Datum 25.11.2026 dazu (`LAST_FULL_RUN` = heute, obwohl es ein Teillauf war).

## 6. Nachtrag 2026-11-25 13:15: FIX 32, 33 und 34 im selben Auftrag

Auftrag Ingo 25.11.: „ja alle mitfixen“ (die drei weiteren Stellen mit demselben Muster, Befund Abschnitt 5 und 7.1).
**Aktiver Stand in T76 ist jetzt `Z_LO03_MD04_BATCH_FIX34_2026-11-25.abap` (1'314 Zeilen), zeichengleich zurueckgelesen;
`T76K912950` enthaelt damit FIX 30 bis 34 und den Selektionstext, weiter nicht freigegeben.** Die Datei
`Z_LO03_MD04_BATCH_FIX31_2026-11-25.abap` ist der Zwischenstand nach FIX 31.

- **FIX 32:** `MDBS` mit `etenr` gelesen (Typ `ty_mdbs`): jede Einteilung einer Bestellposition zaehlt, offene Bestellmenge
  `OMENG` und ihr Wert `OWERT` stimmen.
- **FIX 33:** `MSEG` mit `zeile` gelesen (Typ `ty_mseg_kde`): jede Belegzeile zaehlt, Verbrauch aus Kundeneinzelbestand in `VERBR`.
- **FIX 34:** `AFPO` mit `posnr` gelesen (Typ `ty_afpo`): vorsorglich, in T76 kein Fall.

Die Rechenformen (`calc_omeng`, `calc_verbrauch`) sind unveraendert.

### Test in T76 (echte Laeufe je Material vor und nach dem Einspielen, Werte aus `ZMD04_CALC` per RFC)

| Material | OMENG vorher | OMENG nachher | VERBR vorher | VERBR nachher | Erwartung Claude |
| --- | --- | --- | --- | --- | --- |
| C34974 | 41'600 | 70'400 | 54'044 | 54'044 | OMENG 41'600 -> 70'400 |
| B61003 | 33'000 | 53'000 | 44'496 | 44'496 | OMENG 33'000 -> 53'000 |
| D34612 | 9'120 | 15'360 | 161'455 | 161'455 | OMENG 9'120 -> 15'360 |
| C38774 | 0 | 0 | 34'792 | 59'792 | VERBR +25'000 |
| K70758 | 0 | 0 | 34'210 | 42'210 | VERBR +8'000 |
| C10312 | 50'000 | 50'000 | 128'138 | 133'138 | VERBR +5'000, OMENG gleich |
| 59397 | 0 | 0 | 5'571 | 5'871 | VERBR +300 |

- Die Erwartung stammt aus eigener Rechnung ueber `MDBS`, `AFPO` und `MSEG` (RFC), vor dem Test festgelegt; alle sieben treffen.
- `OWERT` steigt bei den drei Bestellmengen-Faellen im gleichen Verhaeltnis (C34974 23'296 -> 39'424). Alle uebrigen Felder
  dieser sieben Zeilen sind gleich.
- **Regression:** die 500 Zeilen des Bereichs B64000 bis B64999 (mit Komponenten), Stand FIX 31 gegen Stand FIX 34, alle Felder
  ausser Datum und Zeit: 499 gleich. Eine Zeile weicht ab, C21962 `GEPL_ZUG` 6'300 -> 6'600. Das Feld kommt aus der
  Bedarfs-/Bestandsliste (MD04-Baustein), die Fixes fassen es nicht an; die Ursache der Aenderung zwischen 12:13 und 13:03 ist
  **nicht geklaert** (eine kurze Stichprobe in `EBAN` und `PLAF` zeigte keinen Beleg von heute).
- Syntaxpruefung ohne Meldung, ST22 ohne Laufzeitfehler. Belege: `fix30_31_belege/md04_fix34_vorher.csv`,
  `md04_fix34_nachher.csv`, `regression_fix34.txt`.

### Offen (ersetzt Abschnitt 4 Punkt 5)

Abschnitt 4 Punkte 1 bis 4 gelten unveraendert, der Auftrag bringt jetzt alle fuenf Fixes. Nach dem Import aendern sich in ZLO03
ausser „offene MK“ auch offene Bestellmenge, deren Wert und der Verbrauch bei den betroffenen Materialien, jeweils nach oben.
Das gehoert in die Antwort an Armin. Offen bleiben die Befunde aus dem Befund Abschnitt 7.2 („Wert offene MK“ mit Stueckkosten
statt Kontraktpreis, alte Zeilen bei Loeschvormerkung).

## 7. Import in P76 nachgeprueft (2026-11-25 14:40, Agent sap-t76, P76 nur gelesen)

Auftrag Ingo: „alle transport im p pruefe“. Belege aus SE01-Transportprotokollen, Versionsverwaltung, SM37 und Variantenanzeige
in P76 (eigene Sitzung, nichts gespeichert); die STMS-Importhistorie liess sich mit den Werkzeugen nicht blaettern.

- **`T76K912950` importiert am 25.11.2026 13:49:32, beendet 13:49:33, RC 0.** Aktive Version von `Z_LO03_MD04_BATCH` in P76 traegt
  `T76K912950`. P76 fuehrt nur die aktive Version; der Vorgaenger laut T76-Versionsliste ist `T76K911441` (04.08.2026), dessen Import
  in P76 am 04.08.2026 10:35:10 mit RC 0 protokolliert ist; dazwischen kein anderer T76-Auftrag. Damit ist Punkt 4.2 nachtraeglich
  belegt (ein Auftrag aus einem anderen System dazwischen ist ohne P76-Versionshistorie nicht ausgeschlossen, unwahrscheinlich).
- Den Quelltext selbst konnten die P76-Werkzeuge nicht lesen; Beleg fuer den neuen Stand sind Auftrag und Zeit der aktiven Version und
  der neue Parameter in der Variante (unten).
- **Job `ZLO03_PRECALC`** (Ersteller KOI, taeglich 06:00, Klasse C), ein Schritt `Z_LO03_MD04_BATCH` mit Variante **`ALLES360TAGE`**
  (Material 1 bis Z410, Werk 1100, vollstaendig, 360 Tage, `P_KTGUE` X, `P_TEST` leer). Letzter Lauf 25.11. 06:00:21 (1'680 s), also
  noch mit dem alten Stand. **Erster Lauf mit FIX 30 bis 34: 26.11.2026 06:00.** Nur die erste Jobzeile war sichtbar; weitere Jobs mit
  dem Programm sind nicht ausgeschlossen.
- **`P_KTABG` in der Variante:** Zeile „Abgelaufene MK mit Restmenge“ mit X und dem SAP-Hinweis „Kein Wert fuer Parameter gespeichert
  (ggf. neuer Parameter)“, also Vorschlagswert X. Das ist Armins Entscheid (abgelaufene Kontrakte mit Restmenge zaehlen); an der
  Variante ist nichts zu aendern. Punkt 4.3 damit erledigt.
- **Offen:** ab 26.11. nach 06:30 ZLO03 Top-Down 61227 bzw. `ZMD04_CALC` B64344 pruefen (Erwartung 2'125), Antwort an Armin. *Geprueft 26.11., Abschnitt 8; offen bleibt die Antwort an Armin.*

## 8. Erster Lauf in P76 nachgeprueft (2026-11-26, Agent sap-t76, P76 nur gelesen, von Claude gegen die Belege gegengelesen)

Auftrag Ingo 26.11.: „sap prod ist offen, du kannst gegenchecken ob felder werte korrekt sind“. Gelesen wurde nur mit SM37 und
SE16 in einer eigenen Sitzung; es lief kein Report, kein Job und keine Variante wurde gespeichert. Belege liegen in `fix30_31_belege/p76_2026-11-26/`.

**Job:** `ZLO03_PRECALC` / 06002101, Status fertig, Start 06:00:16, Dauer 1'693 s (am 25.11. 1'680 s). Protokoll: „Verarbeitet:
44858/44858“, „Modus F: 44858 Saetze geschrieben.“ um 06:28:29, auf erster und letzter Seite keine E- oder A-Meldung. Heute lief kein
anderer Job mit `Z_LO03_MD04_BATCH`. In `ZMD04_CALC` (45'416 Zeilen, alle Werk 1100) tragen 44'858 Zeilen das Datum 26.11.; das ist dieselbe
Zahl. Die uebrigen rund 557 Zeilen haben ein aelteres Datum, eine davon den 25.11. (67692). *Vermutung, nicht geprueft:* Es sind Materialien,
die der Job nicht mehr auswaehlt.

**Werte gegen eine Nachrechnung aus P76-Rohdaten** (Rechenregeln aus `Z_LO03_MD04_BATCH_FIX34_2026-11-25.abap`, Preise aus `MBEW`, Standardpreis/PEINH):

| Material | Feld | `ZMD04_CALC` | Nachrechnung | Urteil |
| --- | --- | --- | --- | --- |
| B64344 | MKMNG / OMKWR | 7'125 / 138'225.00 | 5 + 0 + 2'120 + 5'000 = 7'125; × 19.40 | gleich |
| B64344 | OMENG / OWERT | 1'152 / 22'348.80 | 576 + 576 | gleich |
| C34974 | OMENG / OWERT | 11'600 / 6'496.00 | Einteilung 0045148351/10/5 | gleich |
| D34612 | OMENG / OWERT | 24'000 / 16'560.00 | 3 × 8'000 | gleich |
| D34612 | MKMNG / OMKWR | 60'000 / 41'400.00 | 42'000 + 18'000 (alte Logik 136'000) | gleich |
| K70758 | OMENG / OWERT | 10'000 / 74.30 | 10'000 × 7.43 / 1'000 | gleich |
| B61003 | MKMNG / OMKWR | 40'000 / 21'200.00 | Kontrakt 46010211 | gleich |
| C10312 | MKMNG / OMKWR | 95'000 / 9'500.00 | 150'000 − 55'000 (46010376) | gleich |
| C38774 | VERBR | 44'504 | 4'504 (MVER 12/2025 bis 11/2026) + 40'000 (MSEG KDE) | gleich |

Nicht nachgerechnet sind 59397 OMENG 220 (vermutlich Fertigungsauftrag, `AFPO` nicht gelesen) und VERBR der uebrigen Materialien.

**B64344 = 7'125 statt der erwarteten 2'125:** Die Kontrakte 46009174 (offen 5, abgelaufen 31.12.2025, zaehlt nur mit `P_KTABG`), 46009844
(offen 0) und 46010268 (offen 2'120) ergeben Armins 2'125. Dazu kommt **46010673/10 mit 5'000 Stueck und ohne Abruf**. Laut `EKKO` ist er am
25.11.2026 angelegt, gueltig vom 25.11.2026 bis 30.06.2028, Kuerzel MSO. Die Erwartung 2'125 war fuer den Stand ohne diesen Kontrakt richtig.
*Vermutung:* Er wurde erst nach Armins ME3M-Liste angelegt.

**Wirkung je Fix in P76:**

- FIX 30 ist belegt: Nach der alten Logik ergaebe B64344 13'456 (die gemeldeten 8'456 plus 5'000), D34612 136'000 statt 60'000.
- FIX 31 ist belegt: Ohne `P_KTABG` ergaebe B64344 7'120.
- FIX 33 ist belegt: C38774 KDE-Verbrauch 40'000 statt 15'000, also +25'000 wie in T76. Ursache sind sechs Zeilen eines Belegs vom 18.12.2025.
- FIX 32 hat an den acht Materialien heute keine Wirkung; die T76-Faelle sind in P76 nicht mehr offen.
- FIX 34 ist nicht gesehen.

**Grenzen:** SE16 bietet in P76 bei `EKPO`, `MDBS` und `AFPO` kein MATNR zur Selektion an. Deshalb wurde ueber Nummernkreise gelesen:
`EKPO` 46*, `MDBS` 45140000 bis 45169999 (ab 03.03.2025). Aeltere offene Bestellungen waeren nicht erfasst, der Abgleich mit dem Jobwert zeigt
aber keine Luecke. ST22 wurde nicht gelesen.

**Offen:** Antwort an Armin (Ingo). B64344 zeigt jetzt 7'125, das sind 2'125 aus den alten Kontrakten und 5'000 aus 46010673. Offene Bestellmenge
und Verbrauch steigen bei betroffenen Materialien. Das Dashboard (Materialanteil, `ZloTopDownSet`) liest den neuen Stand erst im Einkauf-Lauf
der Nacht auf den 27.11. Weiter offen sind Befund 7.2, 59397 OMENG und die rund 557 alten Zeilen.
