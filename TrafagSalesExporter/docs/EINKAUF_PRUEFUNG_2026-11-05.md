# Einkauf: Funktionspruefung im Produktivdashboard (2026-11-05)

Stand: 2026-11-05. Auftrag Ingo: „kannst du mal die funktionen pruefen wie gestern hr ... das auch fuer einkauf“.
Geprueft am 2026-11-05 um 10:28 im Chrome auf `https://trch-webapp-bidashboard.trafagch.local/BiDashboard/einkauf...`,
nur gelesen und Ansichtsfilter bedient (keine Laeufe, keine Einstellungen). Datenstand: Delta 2026-11-04 14:58 erfolgreich,
Standardzeitraum 2026-01 bis 2026-11. Keine Konsolenfehler. **Nichts davon ist behoben**, ausser wo vermerkt.

## Ergebnis je Ansicht

| Ansicht | Status | Werte / Befund |
| --- | --- | --- |
| `/einkauf` | ok | Bestellwert 32'997'456, 6'681 Bestellungen, offen 9'014'591, Kontrakte 18'231'422, 333 Lieferanten, Lagerwert 9'189'770; Disponentenspalten KW 45 = Kachel. Disponent 006 ueberall 0 (Handbuch sagt noch 001 bis 005) |
| `/einkauf/offene-bestellungen` | ok | ueberfaellig 778'700 (94 Pos.), Altposten 169 Pos. 3'084'784; offene Menge 6'403'927 addiert verschiedene Einheiten |
| `/einkauf/kontrakte` | ok | Umschalter stimmen (45 abgelaufene Positionen 131'158, „vermutlich ersetzt“ 2 Pos. 522'711); Kachel 44 Kontrakte vs. 45 Positionen |
| `/einkauf/lieferanten` | Auffaelligkeit | Preisindikator CHF 1.13 ohne Vorjahresvergleich, Text „braucht EKPO“ ueberholt |
| `/einkauf/aufriss`, 11 Einstiege | ok | Schalter an 30'510'805, aus 32'997'456 (= Kachel); 2020 bis heute 211'069'693; Einstiege untereinander stimmig |
| Aufriss, Matrix Lieferant / Jahr | **Fehler** | 2020 bis heute 211'051'808 statt 211'069'693 (-17'885), siehe F2 |
| Aufriss, Materialanteil je Produktgruppe | **Fehler** | HW_ZUKAUFSORT 957'060'506 bei Umsatz 3'643'696 (26'266 %), Total 1'226 %; FP_THERMOST. 615 %, siehe F1 |
| `/einkauf/lieferperformance` | Auffaelligkeit | Kachel 494 „Faelle“ = Materialien, Balken P1+P2 = 555; „offener Bestellwert“ 7'315'040 ist nur der ZLO03-Umfang; ZLO03/ZMD04-Stand 02.09.2026 |
| `/einkauf/ideen/*` | ok bis Auffaelligkeit | Liefertermin-Risiko summiert auf 9'014'591; Spend-Konzentration Top 10 = 54,7 %; Preisentwicklung siehe F3; Datenqualitaet mischt Bestand (240'780) und Zeitraum (7'427) |
| `/einkauf/3d` | Fehler (Beschriftung) | „-10 %“ = -1'507'099 auf Basis der 6 gezeigten Balken (15'070'990), nicht auf 32'997'456 |
| `/einkauf/verbindungen` | Auffaelligkeit | „Einkaufsquelle aktivieren“ steht auf aus, Text bindet den Nachtlauf daran; letzter Lauf 04.11. 14:58 (siehe F5) |
| `/einkauf/kennzahlen`, `/einkauf/pbix`, `/einkauf/spend`, `/einkauf/interaktiv` | ok | Statustext „Spend CHF: Live-Probe“ ueberholt |

Nicht geprueft: Stuecklisten-Netz „Gehirn“ (Einfriergefahr), Materialanteil nur fuer Warengruppen ab CHF 10'000 aufgeklappt,
Filter der Lieferperformance.

## Fehlerliste (priorisiert)

1. **F1 Materialanteil, Wafer (Ursache belegt):** Stuecklisten-Basismenge `STKO-BMENG` (3'738 bis 4'750 ST je Wafer) wird in
   `Z_ZLO03_TURBO2` ignoriert, ZLO03 rechnet 1 Rolle Wafer je Chip. Befund und Vorschlag FIX 26:
   `zlo03/BEFUND_EINHEIT_WAFER_2026-11-05.md` (T76 lesend). Richtig waeren rund CHF 214'000 bis 220'000 statt 956 Mio.
   **Weiterer Ausreisser FP_THERMOST. (615 %):** H20201 Kapillarrohr 4'329 KGM, H17500 Bimetall 2'040 MTR fuer 24'155 Umsatz;
   vermutlich dieselbe Ursachenfamilie (Mengen-/Basismenge), nicht belegt. FP_FROSTI 84 % mit auffaellig gleichen Mengen
   (20'802) bei mehreren Teilen, Verdacht Doppelzaehlung ueber Stufen oder Alternativstuecklisten. Dazu G4 (Mehrstufigkeit).
2. **F2 Matrix Lieferant / Jahr verliert Spend bei Namen, die sich nur in Gross-/Kleinschreibung unterscheiden:** „THUBA AG“
   und „Thuba AG“ werden zu einer Zeile, je Jahr gewinnt ein Wert statt Summe (-17'885). Gleiche Dubletten bei Trafag AG /
   TRAFAG AG und SFS unimarket / SFS Unimarket (dort ohne Verlust). Der Aufriss zeigt sie doppelt (724 statt 721 Zeilen).
3. **F3 Preisentwicklung:** „Stueckpreis nach Jahr“ zeigt fuer alle Jahre „CHF 1“ (Rundung, Wert um 1.13); seit dem
   Standardzeitraum „laufendes Jahr“ kein Vorjahresvergleich mehr (auch Preisindikator Lieferanten).
4. **F4 3D-Simulation:** Delta-Beschriftung nennt die Basis nicht.
5. **F5 Nachtlauf:** Einkaufsquelle „aus“, kein Lauf seit 04.11. 14:58 (Pruefung 05.11. 10:28). Entweder Text ueberholt
   (laut `docs/rag/PURCHASING.md` seit `66a34da` nicht mehr an IsActive gebunden) oder der Nachtlauf lief nicht; Laufprotokoll pruefen.
6. **F6 Lieferperformance:** Kachel zaehlt Materialien, nicht Faelle; Beschriftung „offener Bestellwert“ irrefuehrend; Datenstand alt.
7. **F7 Kontrakte „vermutlich ersetzt“:** C24503 -> C24504 (46009867/00010, CHF 12'711) vermutlich Fehltreffer ueber gleichen Text.
8. **F8 Muster „Wartet auf SAP“-Badge** trotz geladener Daten auf acht Seiten (vermutlich alte Live-Probe-Logik).
9. **F9 Materialanteil-Spalte „Anteil“** hat je Stufe eine andere Basis (Produktgruppe: Anteil am Umsatz, Materialgruppe:
   Anteil an der Produktgruppe, Teil: Anteil an der Materialgruppe); kleine Anteile erscheinen als „0 %“. Frage Ingo 05.11.
10. **Kleinigkeiten:** Zeitstempel 13:58 vs. 14:58 (UTC/Ortszeit), Kommaformat und englische Ampel in Ideen-Hotlists, ISO-Einheiten
    (PCE, KGM, RO) neben „ST“, ueberholte Statustexte (Kennzahlen, Ideen „Bestellrhythmus berechenbar“, Handbuch „001 bis 005“),
    Delta-Protokoll „geaendert = gesamt = 178'117“.
