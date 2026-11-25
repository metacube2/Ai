# FIX 30 und FIX 31 in `Z_LO03_MD04_BATCH`: offene Mengenkontraktmenge (2026-11-25)

Stand: 2026-11-25 12:45. **In T76/100 aktiv, Auftrag `T76K912950` (Aufgabe `T76K912951`), nicht freigegeben. P76 nicht beruehrt.**
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

Nicht geaendert: die SELECTs auf `MDBS`, `MSEG`, `AFPO` (gleiches Muster, siehe Befund Abschnitt 5), `Z_ZLO03_TURBO2`, Jobvarianten.

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
Wirksam in ZLO03 erst nach dem naechsten Lauf des Jobs nach dem Import.

## 4. Offen

1. **Ingo:** `T76K912950` freigeben und nach P76 importieren.
2. **Vor dem Import pruefen (P76, braucht Ingos Freigabe zum Lesen):** dass in P76 der Stand vom 04.08.2026 laeuft
   (`T76K911441`). Der Transport bringt das ganze Programm; ein abweichender P76-Stand wuerde ueberschrieben. Nicht geprueft.
3. **Nach dem Import:** Selektionsbild der Jobvariante in P76 ansehen. Eine bestehende Variante kennt `P_KTABG` nicht; dass sie dann
   den Vorschlagswert „an“ nimmt, ist eine Annahme und nicht geprueft. Danach ZLO03 Top-Down 61227: B64344 muss 2'125 zeigen.
4. **Antwort an Armin** (Ursache, Wert nach dem naechsten Lauf, Hinweis auf weitere betroffene Materialien).
5. Weitere Felder des Jobs: Befund Abschnitt 5 und 7, Entscheid Ingo.

## 5. Nebenwirkungen des Tests in T76

- `ZMD04_CALC` T76: 500 Zeilen neu gerechnet (Sicherung der Tabelle vor dem Lauf nur im Scratchpad der Sitzung, nicht im Repo).
- `ZMD04_CONTROL` T76: drei Zeilen mit Datum 25.11.2026 dazu (`LAST_FULL_RUN` = heute, obwohl es ein Teillauf war).
