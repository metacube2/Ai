# ZLO03 / Materialanteil: Einkaufsteile ohne englischen Kurztext fehlten (Befund 2026-11-18)

Stand: 2026-11-19 08:45 (**Prüfung produktiv bestanden**, Abschnitt 6a: 10.08.00 bei FP_DICHTESENS 273'028 CHF); davor 2026-11-18 18:05
(Korrektur 1 im Dashboard deployed 17:57, `1417a3a`; Einkauf-Lauf 18:01 gestartet). Auftrag Ingo 18.11. („schau mal ob Zusammenhang besteht, alte neue zlo03“, danach
„mach 1“). T76/100 nur gelesen (SapProbe `table-read`), P76 nicht gelesen, in SAP nichts geändert.
Zurück: `zlo03/CLAUDE.md`, `docs/rag/PURCHASING.md`.

## 1. Meldung

Armin Güntensperger, Mail 18.11.2026 13:03, Betreff „Dashboard“: Im Dashboard „Materialanteil je Produktgruppe“ fehlt bei
FP_DICHTESENS die Warengruppe 10.08.00 (Leiterplatten bestückt). Sein Excel aus der ZLO03 (Spalte „DW-Sensoren“) zeigt dort
die grösste Position (rund 258'000), das Dashboard zeigt 10.08.00 gar nicht (FP_DICHTESENS im Dashboard: Umsatz 2'182'604,
Materialanteil 186'913, grösste Warengruppe 10.01.00 mit 127'714).

## 2. Antwort auf die Frage „alte / neue ZLO03“

**Kein Zusammenhang** mit der neuen ZLO03-Kopie für PCBA-Stücklisten (Verwendung 9, `PLAN_ZLO03_PCBA_2026-11-18.md`). Die
bestückte Leiterplatte B44212 (Warengruppe 10.08.00, Werk 1100 Disponent 003, Fremdbeschaffung, Standardpreis 21.54) ist in
den Dichtesensoren 34887 und 65580 ein Einkaufsteil auf Stufe 01 und gehört in der **alten** ZLO03 mit ihrer eigenen
Warengruppe in den Export. Die PCBA-Kopie würde die Leiterplatte nur zusätzlich in ihre Bauteile auflösen.

*Für den PCBA-Plan vorzumerken:* Löst die Kopie die Leiterplatte auf, darf der Materialanteil nicht Leiterplatte **und**
Bauteile zählen.

## 3. Ursache

1. `Z_ZLO03_TURBO2` liest den Materialkurztext in der Anmeldesprache (`load_stammdaten_cache`: `LEFT JOIN makt … AND
   t~spras = sy-langu`).
2. Am Ende von `process_main` löscht der alte FIX 4 alle Komponenten ohne Text: `IF p_txtpo = ' '. DELETE gt_ktab WHERE
   maktx IS INITIAL. ENDIF.`
3. `app_export` (FIX 25, Übergabe ans Gateway) nimmt nur Komponenten, die in `gt_ktab` stehen (`READ TABLE gt_ktab …
   CHECK sy-subrc = 0`).
4. Das Dashboard hat die Anfrage `ZloTopDownSet` ohne `sap-language` gestellt. Der Aufruf lief englisch: Das Dashboard
   zeigte englische Kurztexte („Locking Ring M27X1.5, knurled“) und Einheiten PCE, MTR.

Folge: Jedes Einkaufsteil ohne **englischen** Kurztext fehlte im Materialanteil. Armin startet die ZLO03 deutsch, dort hat
praktisch jedes Teil einen Text.

Derselbe Fehlertyp steht seit dem 13.11. in `docs/rag/LEARNINGS.md` (Gateway lief trotz deutscher Anmeldung teils englisch;
Regel: Texte nie über `sy-langu`). Der Materialanteil ist am 02.11. gebaut worden und wurde danach nicht darauf geprüft.

## 4. Belege

**T76/100, Stammdaten (gelesen 18.11. 17:50 bis 17:56):**

| Messung | Wert |
| --- | --- |
| Materialien mit Kurztext Deutsch / Englisch (MAKT) | 67'858 / 48'589 |
| Einkaufsteile Werk 1100, Disponent 001 bis 006 (MARC) | 7'258 |
| davon ohne englischen Kurztext | **4'050 (56 %)**: Disponent 001 1'584, 002 7, 003 1'933, 004 524, 005 2 |
| davon ohne deutschen Kurztext | **1** (H90044, Disponent 003, Fremdbeschaffung) |
| Stichprobe Dichtesensor 34887: 12 im Dashboard fehlende Teile | alle nur mit deutschem Text (B44212, C37141, C36400, E17077, C36521, C41465, K70974, H05013, C23336, H90252, H73507, E01221) |
| Stichprobe 34887: 11 im Dashboard vorhandene Teile | alle mit englischem Text; das Dashboard zeigte genau diese englischen Texte |

**Produktivdaten (Wochensicherung der Dashboard-Datenbank vom 16.11., nur lesend, Ladestand 16.11. 13:31 UTC):**

| Messung | Wert |
| --- | --- |
| Stand | 8'565 verkürzte Nummern, 112'095 Stücklistenzeilen |
| FP_DICHTESENS | 33 Nummern, Menge 12'576, Umsatz 2'182'604; 34887 mit 9 Stücklistenzeilen (T76: rund 28 Einkaufsteile) |
| 10.08.00 im ganzen Materialanteil | nur 10 Teile in 835 Nummern |
| Stücklistenzeilen je Disponent | 001 77'180, 002 6, 003 32'295, 004 2'614, 005 und 006 keine |
| Erwartete Paare Einkaufsteil × verkaufte Nummer (aus dem Verwendungsnachweis im Einkauf-Cache) | 292'373, davon im Materialanteil 107'053 (**37 %**) |
| Anteil gefunden je Warengruppe | 10.08.00 16 %, 20.01.01 10 %, 10.06.00 23 %, 70.01.00 6 %, 60.03.00 0 % |
| Einkaufsteile, die nie vorkommen | 1'530 von 2'653 |

Die 37 % sind ein Richtwert: Die Vergleichsbasis (Bottom-Up-Verwendung `ZSTR_LZCODE_USAGESet`) ist eine andere Quelle und
zählt auch Textpositionen und löschvorgemerkte Nummern.

**Tragweite:** Der Materialanteil war seit dem 02.11. in **allen** Produktgruppen zu tief, nicht nur bei den
Dichtesensoren (Total im Dashboard 10,4 Mio. CHF bei 79,0 Mio. Umsatz).

## 5. Korrektur

**Korrektur 1, Dashboard (gebaut, `1417a3a`, deployed 18.11.2026 17:57):** Die Anfrage `ZloTopDownSet` trägt
`sap-language=DE` (`PurchasingDataRefreshService.BuildSingleRequestUrl`, Konstante
`PurchasingMaterialShareLoader.BomLanguage`). `Zc23Set` bleibt unverändert (die Umsatzseite stimmte mit Armins Zahl
überein). Test `Stuecklisten_Anfrage_Laeuft_In_Deutscher_Anmeldesprache`; Release-Worktree 1'635 von 1'635 Tests grün.
Nebenwirkung: Kurztexte und Einheiten der Einkaufsteile erscheinen im Materialanteil deutsch (ST, M statt PCE, MTR).

**Reicht Korrektur 1?** Nach den Stammdaten in T76 ja, mit einer Ausnahme: Genau ein Einkaufsteil (H90044) hat nur einen
englischen Text und fiele mit der deutschen Anfrage weg. Voraussetzung ist, dass das Gateway den Parameter übernimmt; das
zeigt erst der Produktivlauf (Abschnitt 6). Der Aufruf liess sich im Browser gegen T76 nicht nachstellen (Chrome-Fehlerseite,
der Port ist erreichbar).

**Korrektur 2, SAP (nicht gebaut, Vorschlag; nächste freie FIX-Nummer, „FIX 29“ ist im PCBA-Plan schon für die Kopie
vergeben):** In `Z_ZLO03_TURBO2` den Kurztext mit Rückfall lesen (Anmeldesprache, sonst Deutsch, sonst Englisch) und bei
`P_APP` keine Komponente wegen leerem Text löschen. Das macht den Export unabhängig von der Anmeldesprache und hilft auch
Anwendern, die die ZLO03 nicht deutsch starten. Dieselbe Stelle ist im Schwesterprogramm `ZM_LZCODE20_OPT` als FIX 14 schon
einmal beschrieben worden (`DELETE … WHERE maktx IS INITIAL` durch `WHERE postyp = 'T'` ersetzen, `zlo03/CLAUDE.md`), in
`Z_ZLO03_TURBO2` aber nie angekommen. Braucht T76, Transport und Import nach
P76. Empfehlung: als Absicherung einplanen, nicht dringend, sobald Abschnitt 6 bestanden ist. Für H90044 genügt alternativ
ein deutscher Kurztext im Materialstamm.

## 6. Prüfung produktiv (steht aus)

Einkauf-Lauf (Delta) am 18.11. um 18:01 im Dashboard gestartet; der Materialanteil wird am Ende des Laufs geladen (am 09.11.
dauerte der Lauf gut zwei Stunden). Zu prüfen:

1. Meldung des Laufs: Zahl der Stücklistenzeilen (vorher 112'112) und kein „Fehler beim Lesen (Stand unverändert)“.
2. FP_DICHTESENS zeigt 10.08.00; unter 34887 steht B44212.
3. Disponenten 005 und 006 kommen vor, Kurztexte sind deutsch.
4. Grössenordnung gegen Armins Excel (10.08.00 rund 258'000 in seiner Spalte „DW-Sensoren“; Zeitraum und Auswahl seines
   Excel sind nicht bekannt, Gleichheit ist nicht zu erwarten).

Bleibt die Zeilenzahl gleich, übernimmt das Gateway den Parameter nicht; dann ist Korrektur 2 nötig.

## 6a. Ergebnis produktiv (19.11. 08:45, im Dashboard gelesen)

Einkauf-Lauf 18.11. 18:01 bis 19:29 erfolgreich: **262'856 Stücklistenzeilen** (vorher 112'112), 31'003 ZC23-Summen, 8'876 Nummern,
kein Lesefehler. Damit übernimmt das Gateway `sap-language=DE`. Materialanteil FP_DICHTESENS: Umsatz 2'145'646, Materialanteil
**693'626 CHF (32.3 %)** statt 186'913 (8.5 %); 10.08.00 ist mit 39.4 % die grösste Warengruppe. Vergleich mit Armins Excel (Zeitraum
und Auswahl seines Excel unbekannt):

| Warengruppe | Dashboard 19.11. | Excel Armin („DW-Sensoren“) |
| --- | ---: | ---: |
| 10.08.00 Leiterplatten bestueckt | 273'028 | 257'717 |
| 10.01.00 Elektronische Bauteile | 124'328 | 117'320 |
| 20.01.01 Anschlussteil | 108'126 | 104'219 |
| 20.02.01 Gehaeuseteil | 75'718 | 75'057 |
| 01.01.00 Rohmaterial | 26'651 | 27'393 |
| 10.06.00 Steckverbinder | 17'695 | 16'745 |
| 10.05.00 Kabel konfektioniert | 5'615 | 5'314 |

Total aller Produktgruppen: Materialanteil 29,7 Mio. CHF bei 82,3 Mio. Umsatz (36.1 %), vorher 10,4 Mio. Korrektur 2 in der ZLO03
bleibt ein Vorschlag (Absicherung), nicht dringend.

## 7. Nicht geprüft

- P76 wurde nicht gelesen. Die Kurztexte stammen aus T76; sie decken sich mit den Texten im produktiven Dashboard.
- Mit welcher Anmeldesprache der technische Benutzer des Dashboards in P76 arbeitet, ist aus den Texten und Einheiten
  gefolgert, nicht in SU01 gelesen.
- Grundlage von Armins Excel (Zeitraum, Auswahl der Nummern).
