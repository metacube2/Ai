# ZLO03 FIX 24: Stückkosten ungerundet, Textpositionen nicht im Bedarf, Bottom-Up-Details in Stücklisten-Einheit (2026-10-29)

Stand: 2026-10-30. T76 aktiv, Auftrag `T76K912785`, nicht freigegeben. Alle fünf Testläufe (A bis E) bestanden.

## 1. Anlass

Ingo hat nach FIX 23 gefragt, ob die Top-Down-Logik weitere Fehler hat. Die Befunde stehen in `zlo03/CLAUDE.md` Abschnitt G und
sind dort am 2026-10-29 mit Daten belegt worden. Umgesetzt wurde nach Ingos Meldung „import ist durch“ (FIX 12 bis 23 in P76).

## 2. Umsetzung

Quelltext `Z_ZLO03_TURBO2_FIX24_2026-10-29.abap` (Vorstand FIX 23, 4'119 Zeilen, 15 Stellen `FIX 24`).

| Punkt | Befund | Änderung |
| --- | --- | --- |
| G1 Rundung | `ty_ktab-stueckkosten` hatte 2 Nachkommastellen; alle Wertspalten rechneten mit dem gerundeten Preis (K50057: 0.02 je 10 → 0.00, Wert 0) | Stückkosten mit 10 Nachkommastellen, Ausgabe auf 6 Stellen ohne Nullen am Ende (neue FORM `zahl_text`); in den transponierten Details (`ausgabe_spalten_details`) ebenso |
| G2 `p_days` | Parameter „Verbräuche zurück in Tagen“ setzte nur `gv_datum_von`, das nirgends gelesen wird | `NO-DISPLAY`: ausgeblendet, nicht gelöscht, damit gespeicherte Varianten weiter laufen |
| G3 Textpositionen | Top-Down: mit `p_txtpo` zählte die Menge einer T-Position zum Bedarf; Bottom-Up: jede Position wurde summiert | In beiden Richtungen keine T-Position mehr in MTAB. `p_txtpo` macht die Zeile nur noch sichtbar; eine Zeile, die nur aus einer Textposition besteht, hat keine Menge |
| G5 Kommentar | FIX-22-Kommentar „Werk p_werks“ | „Werk gc_werks“ |
| Rest FIX 23 | Bottom-Up mit „Detailinform. für Spalte“: Bestand, Zugänge, Abgänge, Endbestand, offene MK, Meldebestand, feste Losgrösse, Sicherheitsbestand und Stückkosten standen in der Basiseinheit (H05039 in KAR), der Kopf der Spalte seit FIX 23 in der Stücklisten-Einheit (ST) | Neue FORM `details_bom_unit`: umgerechnete Kopien für die Mengenzeilen (× UMREN / UMREZ), Stückkosten × UMREZ / UMREN. Die Wertzeilen in CHF bleiben bei Basismenge × Basispreis |

Nebenbei: `fill_ktab` teilt durch `ps_ktab-peinh` statt `ls_stamm-peinh`. Das ändert nichts, weil der Stammdaten-Cache leere
PEINH schon beim Laden auf 1 setzt.

**G4 Mehrstufigkeit** ist bewusst nicht geändert (siehe `CLAUDE.md` G4: erst gegen CS12 prüfen).

## 3. Einspielen (eigene Sitzung T76/100)

| Schritt | Ergebnis |
| --- | --- |
| Sperre | `Z_ZLO03_TURBO2` in keinem offenen Auftrag (`E071`/`E070`, alle Aufträge R) |
| Auftrag | neuer Workbench-Auftrag **`T76K912785`** „ZLO03 FIX 24 Rundung, Textpositionen, Einheit Details“ |
| Aktivieren | nur `Z_ZLO03_TURBO2`, „Objekt(e) wurde(n) aktiviert“ (fremdes inaktives `Z_REICHW` übergangen) |
| Rückgelesen | per `abap-read`, `diff` Exitcode 0 gegen die Patchdatei |
| Selektionsbild | Feld „Verbräuche zurück in Tagen“ nicht mehr sichtbar |

## 4. Test mit Daten (T76, Läufe von Ingo gespeichert, Vergleich Zelle für Zelle mit den Läufen vom 2026-10-29)

| Lauf | Auswahl | Vergleich | Ergebnis |
| --- | --- | --- | --- |
| A | Top-Down 58944 | `hrg.xlsx` | 27 Zellen anders, alle in Stückkosten und Wertspalten (7 Komponenten). K50057: 0.00 → **0.002**, Wert Endbestand 0 → **19.67**; H05026: 0.00 → 0.003666, Wert Endbestand 0 → 333.01; E01159: 0.06 → 0.0638. Mengen unverändert. Datei `jj.xlsx`, zellgleich mit `a.xlsx` vom 29.10. |
| B | Top-Down 41019 | `s.xlsx` (FIX 23) | 87 Zellen anders, alle in Stückkosten und Wertspalten (z. B. C34321: 0.01 → 0.0113, Wert Endbestand 185.06 → 209.12); H05039 unverändert 0.014133 |
| C | Top-Down 46123 mit Textpositionen | `kg.xlsx` (mit `p_txtpo`) | B63485 in 46123: 2 → **1** ST; sonst nur Stückkosten und Wertspalten (C15877: 0.01 → 0.00931). Gegen `hhh.xlsx` (ohne `p_txtpo`) nur Stückkosten und Werte: das Kästchen ändert den Bedarf nicht mehr |
| D | Bottom-Up B63485 | `mmm.xlsx` | genau 1 Zelle anders: 46123 von 2 → **1** ST (Textposition zählt nicht mehr). Alle übrigen Zellen gleich. Datei `g.xlsx` |
| E | Bottom-Up H05039 mit Details | `hhh.xlsx` im Repo-Stamm (ohne Details) | 988 Zeilen zellgleich; Details neu in ST: Lagerbestand 4'659.6, feste Losgrösse 12'000, Sicherheitsbestand 1'800, Stückkosten 0.014133, Wert Endbestand 65.86 CHF (gleich wie im Top-Down) |

## 5. Offen

1. Freigabe und Import `T76K912785` nach P76 (Ingo).
2. Ann-Katrin und Sandro informieren: Werte bei Komponenten mit Preiseinheit > 1 ändern sich (genauer), Bedarf zählt keine
   Textpositionen mehr, Feld „Verbräuche zurück in Tagen“ ist weg.
3. G4 Mehrstufigkeit: 41019 gegen CS12 (mehrstufig) prüfen.
